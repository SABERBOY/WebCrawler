using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WebCrawler.Common.Analyzers
{
    public static class HtmlAnalyzer
    {
        public static Regex EXP_NODE = new Regex(@"^(?<tag>\w+)\[(?<idx1>\d+)(-(?<idx2>\d+))?\]?$");

        public static Link[] GetValidLinks(HtmlDocument htmlDoc)
        {
            var linkNodes = htmlDoc.DocumentNode.SelectNodes("//a");
            if (linkNodes == null)
            {
                return new Link[0];
            }

            return linkNodes
                .Select(o => new Link
                {
                    XPath = o.XPath,
                    Text = Utilities.NormalizeHtmlText(o.InnerText),
                    Url = o.GetAttributeValue("href", null)
                })
                .Where(o => o.Url != null
                    && !o.Url.StartsWith("#")
                    && !o.Url.StartsWith("javascript", StringComparison.CurrentCultureIgnoreCase)
                )
                .ToArray();
        }

        public static CatalogItem[] ExtractCatalogItems(HtmlDocument htmlDoc, string listPath = null)
        {
            if (string.IsNullOrEmpty(listPath)) // auto detect catalog items
            {
                var blocks = AutoDetectCatalogs(htmlDoc);
                if (blocks.Length == 0)
                {
                    return new CatalogItem[0];
                }

                var catalogItems = new Dictionary<Block, CatalogItem[]>();
                foreach (var block in blocks)
                {
                    var items = GetCatalogItems(htmlDoc, block);
                    if (items.Length > 0)
                    {
                        catalogItems.Add(block, items);
                    }
                }

                if (catalogItems.Count == 0)
                {
                    return new CatalogItem[0];
                }

                return catalogItems
                    // blocks with published date has higher priority
                    .OrderByDescending(o => o.Value.All(l => l.HasDate))
                    .ThenByDescending(o => o.Key.Score)
                    .Select(o => o.Value)
                    .First();
            }
            else // detect catalog items by list xpath
            {
                return GetCatalogItems(htmlDoc, new Block { LinkPathSummary = listPath });
            }
        }

        public static string GetListPath(Link[] links, string xpath)
        {
            var nodeIndexExp = new Regex(@"\[\d+\]");

            var genericPath = nodeIndexExp.Replace(xpath, "");

            var similarLinks = links.Where(o => nodeIndexExp.Replace(o.XPath, "") == genericPath)
                .ToArray();

            List<Block> blocks = new List<Block>();
            BlockLinks(similarLinks, blocks);

#if DEBUG
            // show data for debugging
            var linksPlain = string.Join("\r\n", links.Select(o => o.XPath + "\t" + o.Text));
            var similarLinksPlain = string.Join("\r\n", similarLinks.Select(l => l.XPath + "\t" + l.Text));
            var blocksPlain = string.Join("\r\n\r\n", blocks.Select(o => o.LinkPathSummary + "\t" + o.LinkCount + "\r\n" + o.LinkPath + o.LinkText));
#endif

            return blocks.FirstOrDefault(o => ContainsPath(o.LinkPathSummary, xpath))?.LinkPath;
        }

        private static Block[] AutoDetectCatalogs(HtmlDocument htmlDoc)
        {
            // enumerate all links
            var links = GetValidLinks(htmlDoc);
            if (links.Length == 0)
            {
                return new Block[0];
            }

            // put similar links together and exclude invalid links
            var similarLinks = GetSimilarLinks(links);

            // determine blocks for each links
            List<Block> blocks = new List<Block>();
            similarLinks.ForEach(o => BlockLinks(o.Value, blocks));

#if DEBUG
            // show data for debugging
            var linksPlain = string.Join("\r\n", links.Select(o => o.XPath + "\t" + o.Text));
            var similarLinksPlain = string.Join("\r\n\r\n", similarLinks.Select(o => o.Key + "\r\n" + string.Join("\r\n", o.Value.Select(l => l.XPath + "\t" + l.Text))));
            var blocksPlain = string.Join("\r\n\r\n", blocks.Select(o => o.LinkPathSummary + "\t" + o.LinkCount + "\r\n" + o.LinkPath + o.LinkText));
#endif

            return FilterBlocks(blocks);
        }

        private static CatalogItem[] GetCatalogItems(HtmlDocument htmlDoc, Block block)
        {
            var items = new List<CatalogItem>();

            var blockNodes = htmlDoc.DocumentNode.SelectNodes(block.ContainerPath);
            string linkUrl;
            string linkTitle;
            CatalogItem linkItem;
            HtmlNode linkNode;
            foreach (HtmlNode blockNode in blockNodes)
            {
                linkNode = blockNode.SelectSingleNode(block.RelativeLinkXPath);
                if (linkNode == null)
                {
                    continue;
                }

                linkUrl = linkNode.GetAttributeValue("href", null);
                linkTitle = linkNode.InnerText;

                if (string.IsNullOrEmpty(linkUrl))
                {
                    // skip list item with invalid link
                    continue;
                }

                linkItem = new CatalogItem
                {
                    XPath = linkNode.XPath,
                    Url = linkUrl,
                    Title = Utilities.NormalizeHtmlText(linkTitle),
                    FullText = Utilities.NormalizeHtmlText(blockNode.InnerText),
                    Published = Html2Article.GetPublishDate(blockNode.InnerText),
                    PublishedRaw = Html2Article.GetPublishDateStr(blockNode.InnerText)
                };

                items.Add(linkItem);
            }

            return items
                .GroupBy(o => o.Url)
                // pick the first link which contains text if duplicate links detected
                .Select(o => o.FirstOrDefault(lnk => !string.IsNullOrEmpty(lnk.Title)))
                .Where(o => o != null)
                .ToArray();
        }

        private static Dictionary<string, Link[]> GetSimilarLinks(Link[] links)
        {
            var noiseAreaExp = new Regex(@"\b(header|footer|aside|nav|abbr)\b", RegexOptions.IgnoreCase);
            var nodeIndexExp = new Regex(@"\[\d+\]");

            // group similar links
            return links
                .Where(o => !noiseAreaExp.IsMatch(o.XPath)) // exclude noise area links
                .GroupBy(o => nodeIndexExp.Replace(o.XPath, ""))
                .Where(o => o.Count() >= Constants.RULE_CATALOG_LIST_MIN_LINKCOUNT // exclude small link blocks
                    && o.Max(l => l.Text?.Length ?? 0) >= Constants.RULE_CATALOG_LIST_MIN_LINKTEXT_LEN // exclude short text link blocks, NOTICE: couldn't use average tex length here as we haven't exluced all noise links which might degrade the average score
                )
                .ToDictionary(o => o.Key, o => o.ToArray());
        }

        private static void BlockLinks(Link[] similarLinks, List<Block> blocks)
        {
            Block block = null;
            Link link;
            string listPathSummary;
            for (var i = 0; i < similarLinks.Length; i++)
            {
                link = similarLinks[i];

                if (block == null) // start 1st item
                {
                    //start new block
                    block = new Block
                    {
                        LinkPathSummary = link.XPath,
                        LinkCount = 1,
                        LinkTextLength = link.Text.Length
                    };

#if DEBUG
                    block.LinkText = "\r\n" + link.Text;
#endif

                    blocks.Add(block);
                }
                else // compare with the items after
                {
                    listPathSummary = GetListPathSummary(block.LinkPathSummary, link.XPath);

                    if (!string.IsNullOrEmpty(listPathSummary)
                        // accepts 2-level depth of nested catalog lists at most
                        /*&& expAnyIndex.Matches(genericXPath).Count <= Constants.RULE_CATALOG_LIST_NESTED_MAX_LEVEL*/)
                    {
                        block.LinkPathSummary = listPathSummary;
                        block.LinkCount++;
                        block.LinkTextLength += link.Text.Length;
#if DEBUG
                        block.LinkText += "\r\n" + link.Text;
#endif
                    }
                    else
                    {
                        // move back to transact again
                        i--;
                        // clear block
                        block = null;
                    }
                }
            }
        }

        private static Block[] FilterBlocks(IEnumerable<Block> blocks)
        {
            var query = blocks
              .Where(o => (double)o.LinkTextLength / o.LinkCount > Constants.RULE_CATALOG_LIST_MIN_LINKTEXT_LEN // exclude short link text blocks
                  && o.LinkCount > Constants.RULE_CATALOG_LIST_MIN_LINKCOUNT // exclude small set links blocks
              )
              .OrderByDescending(o => o.Score) as IEnumerable<Block>;

            var topBlock = query.FirstOrDefault();
            if (topBlock == null)
            {
                return new Block[0];
            }
            var threshold = topBlock.Score * Constants.RULE_CATALOG_BLOCK_MINSCORE_FACTOR;

            // exclude blocks with low score
            query = query.Where(o => o.Score > threshold);

            /*var genericIndexExp = new Regex(@"\[\*\]");

            query = query.Where(blk =>
            {
                var level = genericIndexExp.Matches(blk.LinkXPath).Count;
                if (level == 0)
                {
                    return false;
                }
                else if (level == 1)
                {
                    return true;
                }
                else
                {
                    // TODO
                    return false;
                }
            });*/

            return query.ToArray();
        }

        /// <summary>
        /// Summarize and merge path, this could be also used to verify if a xpath is already included in the summary.
        /// </summary>
        /// <param name="summary"></param>
        /// <param name="current"></param>
        /// <returns></returns>
        private static string GetListPathSummary(string summary, string current)
        {
            var summarySegments = summary.Split('/');
            var currentSegments = current.Split('/');

            if (summarySegments.Length != currentSegments.Length)
            {
                return null;
            }

            Match summaryNodeMatch;
            Match currentNodeMatch;
            int summaryIndexStart;
            int summaryIndexEnd;
            int currentIndex;
            for (var i = 0; i < summarySegments.Length; i++)
            {
                // identical path segments which including index
                if (currentSegments[i] == summarySegments[i])
                {
                    continue;
                }

                summaryNodeMatch = EXP_NODE.Match(summarySegments[i]);
                currentNodeMatch = EXP_NODE.Match(currentSegments[i]);

                // check tag name
                if (currentNodeMatch.Groups["tag"].Value != summaryNodeMatch.Groups["tag"].Value)
                {
                    return null;
                }

                int.TryParse(summaryNodeMatch.Groups["idx1"].Value, out summaryIndexStart);
                int.TryParse(summaryNodeMatch.Groups["idx2"].Value, out summaryIndexEnd);
                int.TryParse(currentNodeMatch.Groups["idx1"].Value, out currentIndex);

                // check tag index when index1 and index2 are different
                if (summaryIndexEnd == 0) // new iteration
                {
                    if (summaryIndexStart == 1 && currentIndex == summaryIndexStart + 1) // new iteration must start from 1, and continuous
                    {
                        summarySegments[i] = EXP_NODE.Replace(summarySegments[i], $"${{tag}}[{summaryIndexStart}-{currentIndex}]");
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (currentIndex == summaryIndexEnd + 1) // new index is continuous
                {
                    summarySegments[i] = EXP_NODE.Replace(summarySegments[i], $"${{tag}}[{summaryIndexStart}-{currentIndex}]");
                }
                else if (currentIndex >= summaryIndexStart && currentIndex <= summaryIndexEnd) // path already included, this is used to verify if a xpath is included in a summary
                {
                    continue;
                }
                else if (currentIndex != 1) // new index doesn't start from 1
                {
                    return null;
                }
            }

            return string.Join("/", summarySegments);
        }

        private static bool ContainsPath(string summary, string xpath)
        {
            return summary == GetListPathSummary(summary, xpath);
        }
    }

    public class Link
    {
        public string XPath { get; set; }
        public string Url { get; set; }
        public string Text { get; set; }
    }

    public class Block
    {
        public int LinkCount { get; set; }
        public int LinkTextLength { get; set; }
        /// <summary>
        /// Link text captured in debug mode only.
        /// </summary>
        public string LinkText { get; set; }
        public int FullTextLength { get; set; }
        public double Score
        {
            get
            {
                // return ((double)LinkTextLength / MatchCount) * 1.0 + (MatchCount) * 1.0;
                return LinkTextLength;
            }
        }
        public string LinkPath { get; private set; }

        private string _linkPathSummary;
        public string LinkPathSummary
        {
            get
            {
                return _linkPathSummary;
            }
            set
            {
                _linkPathSummary = value;
                LinkPath = Regex.Replace(LinkPathSummary, @"\[\d+-\d+\]", "[*]");
            }
        }

        public string ContainerPath
        {
            get
            {
                if (string.IsNullOrEmpty(LinkPath))
                {
                    return string.Empty;
                }

                int index = LinkPath.LastIndexOf("[*]");

                return index == -1 ? LinkPath : LinkPath.Substring(0, index);
            }
        }

        public string RelativeLinkXPath
        {
            get
            {
                if (string.IsNullOrEmpty(LinkPath))
                {
                    return string.Empty;
                }

                var xpath = LinkPath.Substring(ContainerPath.Length);
                if (xpath.StartsWith("[*]"))
                {
                    xpath = xpath.Substring(3);
                }

                return "." + xpath;
            }
        }
    }

    public class CatalogItem
    {
        public string XPath { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string FullText { get; set; }
        public DateTime? Published { get; set; }
        /// <summary>
        /// Indicates a date/time string, which might not even be a full date (e.g. exclude year part).
        /// </summary>
        public string PublishedRaw { get; set; }
        public bool HasDate
        {
            get
            {
                return Published != null || !string.IsNullOrEmpty(PublishedRaw);
            }
        }
    }
}
