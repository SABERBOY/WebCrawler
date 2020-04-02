using HtmlAgilityPack;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using WebCrawler.Core;
using WebCrawler.Housing.Models;
using WebCrawler.Housing.Persisters;

namespace WebCrawler.Housing.Crawlers
{
    public class HousingCrawler : ICrawler
    {
        private readonly static string URL_HOME = "http://dgfc.dg.gov.cn/dgwebsite_v2/Vendition/ProjectInfo.aspx?new=1";

        private readonly IServiceProvider _serviceProvider;
        private readonly CrawlingSettings _crawlingSettings;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        private readonly ActionBlock<ProjectData> _projectWorker;

        private static Dictionary<PropertyInfo, string> _projectProperties;
        private static Dictionary<PropertyInfo, string> ProjectProperties
        {
            get
            {
                if (_projectProperties == null)
                {
                    _projectProperties = typeof(Project)
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .ToDictionary(o => o, o => o.GetCustomAttribute<DisplayAttribute>()?.Name)
                        .Where(o => o.Value != null)
                        .ToDictionary(o => o.Key, o => o.Value);
                }

                return _projectProperties;
            }
        }

        public HousingCrawler(IServiceProvider serviceProvider, CrawlingSettings crawlingSettings, IHttpClientFactory httpClientFactory, ILogger logger)
        {
            _serviceProvider = serviceProvider;
            _crawlingSettings = crawlingSettings;
            _logger = logger;

            _httpClient = httpClientFactory.CreateClient(Constants.HTTP_CLIENT_NAME_DEFAULT);
            _httpClient.DefaultRequestHeaders.Referrer = new Uri(URL_HOME);

            _projectWorker = new ActionBlock<ProjectData>(
                CrawlProjectAsync,
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = _crawlingSettings.MaxDegreeOfParallelism
                });
        }

        public async Task ExecuteAsync()
        {
            await CrawlTownsAsync();

            _projectWorker.Complete();
            _projectWorker.Completion.Wait();

            _logger.LogInformation($"All completed");
        }

        private async Task CrawlTownsAsync()
        {
            try
            {
                _logger.LogInformation("Crawling towns data");

                var homeHtml = await _httpClient.GetStringAsync(URL_HOME);

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(homeHtml);

                var townNodes = htmlDoc.DocumentNode.SelectNodes("//select[@id='townName']/option");

                if (townNodes.Count == 0)
                {
                    _logger.LogError("Couldn't detect towns list");
                    return;
                }

                var viewState = htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__VIEWSTATE']")?.GetAttributeValue("value", null);
                var eventValidation = htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__EVENTVALIDATION']")?.GetAttributeValue("value", null);
                var resultCount = htmlDoc.DocumentNode.SelectSingleNode("//input[@name='resultCount']")?.GetAttributeValue("value", null);
                var pageIndex = htmlDoc.DocumentNode.SelectSingleNode("//input[@name='pageIndex']")?.GetAttributeValue("value", null);

                var towns = townNodes
                    .Select(o => new TownData
                    {
                        Code = o.GetAttributeValue("value", null),
                        Name = o.InnerText,
                        ResultCount = resultCount,
                        PageIndex = pageIndex,
                        ViewState = viewState,
                        EventValidation = eventValidation
                    })
                    .ToArray();

                _logger.LogInformation($"Detected {towns.Length} towns");

                TownData previous = null;
                foreach (var town in towns)
                {
                    if (previous != null)
                    {
                        town.ViewState = previous.ViewState;
                        town.EventValidation = previous.EventValidation;
                        town.ResultCount = previous.ResultCount;
                        town.PageIndex = previous.PageIndex;
                    }

                    previous = await CrawlTownAsync(town);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve towns");
            }
        }

        private async Task<TownData> CrawlTownAsync(TownData townData)
        {
            var town = new Town
            {
                Code = townData.Code,
                Name = townData.Name
            };

            var persister = _serviceProvider.GetRequiredService<IPersister>();
            await persister.SaveAsync(town, town.Code);

            var formData = new Dictionary<string, string>
            {
                { "__VIEWSTATE", townData.ViewState },
                { "__EVENTVALIDATION", townData.EventValidation },
                { "townName", townData.Code },
                { "usage", "" },
                { "projectName", "" },
                { "projectSite", "" },
                { "developer", "" },
                { "area1", "" },
                { "area2", "" },
                { "resultCount", townData.ResultCount },
                { "pageIndex", townData.PageIndex }
            };

            var response = await _httpClient.PostAsync(URL_HOME, new FormUrlEncodedContent(formData));
            response.EnsureSuccessStatusCode();
            
            var respData = await response.Content.ReadAsStringAsync();

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(respData);

            townData.ViewState = htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__VIEWSTATE']")?.GetAttributeValue("value", null);
            townData.EventValidation = htmlDoc.DocumentNode.SelectSingleNode("//input[@name='__EVENTVALIDATION']")?.GetAttributeValue("value", null);
            townData.ResultCount = htmlDoc.DocumentNode.SelectSingleNode("//input[@name='resultCount']")?.GetAttributeValue("value", null);
            townData.PageIndex = htmlDoc.DocumentNode.SelectSingleNode("//input[@name='pageIndex']")?.GetAttributeValue("value", null);

            var projNodes = htmlDoc.DocumentNode.SelectNodes("//table[@id='resultTable']//tr/td[1]/a");
            if (projNodes == null || projNodes.Count == 0)
            {
                return townData;
            }

            var projData = htmlDoc.DocumentNode.SelectNodes("//table[@id='resultTable']//tr/td[1]/a")
                .Select(o => Utilities.ResolveResourceUrl(o.GetAttributeValue("href", null), URL_HOME))
                .Select(o => new ProjectData { Town = town, ProjectUrl = o })
                .ToArray();

            _logger.LogInformation($"{townData.Name} Detected {townData.ResultCount}:{projData.Length} projects");

            foreach (var proj in projData)
            {
                _projectWorker.Post(proj);
            }

            return townData;
        }

        private async Task CrawlProjectAsync(ProjectData projData)
        {
            var basicUrl = projData.ProjectUrl.Replace("BeianDetail.aspx", "BeianView.aspx", StringComparison.CurrentCultureIgnoreCase);
            var respData = await _httpClient.GetStringAsync(basicUrl);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(respData);

            var dataDict = htmlDoc.DocumentNode
                .SelectNodes("//table[@class='resultTable2']//tr")
                .ToDictionary(o => Utilities.NormalizeText(o.SelectSingleNode("td[1]").InnerText).Trim('：'), o => Utilities.NormalizeText(o.SelectSingleNode("td[2]").InnerText));

            var proj = new Project
            {
                Id = projData.ProjectId,
                TownCode = projData.Town.Code,
                URL = basicUrl,
                Timestamp = DateTime.Now
            };

            MergeData(dataDict, proj);

            var persister = _serviceProvider.GetRequiredService<IPersister>();
            await persister.SaveAsync(proj, proj.Id);

            _logger.LogInformation($"{projData.Town.Name} Crawled project {proj.Name}, queue: {_projectWorker.InputCount}");
        }

        private void MergeData(Dictionary<string, string> data, Project project)
        {
            PropertyInfo prop;
            foreach (var pair in data)
            {
                prop = ProjectProperties.FirstOrDefault(o => o.Value == pair.Key).Key;
                if (prop == null)
                {
                    _logger.LogWarning($"Property {pair.Key} isn't defined for project {project.Id} yet");
                }
                else
                {
                    prop.SetValue(project, ExtractValue(pair.Value, prop.PropertyType));
                }
            }
        }

        private static object ExtractValue(string text, Type valueType)
        {
            if (valueType == typeof(double))
            {
                var match = Regex.Match(text, @"\d*(\.\d*)?");

                return match.Success ? double.Parse(match.Value) : 0;
            }
            else if (valueType == typeof(int))
            {
                var match = Regex.Match(text, @"\d+");

                return match.Success ? int.Parse(match.Value) : 0;
            }
            else
            {
                return text;
            }
        }
    }

    public class TownData
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string ResultCount { get; set; }
        public string PageIndex { get; set; }
        public string ViewState { get; set; }
        public string EventValidation { get; set; }
    }

    public class ProjectData
    {
        public Town Town { get; set; }
        public string ProjectUrl { get; set; }
        public int ProjectId
        {
            get
            {
                if (string.IsNullOrEmpty(ProjectUrl))
                {
                    return 0;
                }

                var match = Regex.Match(ProjectUrl, @"(?<=id=)\d+");

                return match.Success ? int.Parse(match.Value) : 0;
            }
        }
    }
}
