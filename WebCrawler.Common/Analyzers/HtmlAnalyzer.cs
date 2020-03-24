using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WebCrawler.Common.Analyzers
{
    public static class HtmlAnalyzer
    {
        public static CatalogItem[] ExtractCatalogItems(HtmlDocument htmlDoc, string listPath = null)
        {
            if (string.IsNullOrEmpty(listPath)) // auto detect catalog items
            {
                var blocks = EvaluateCatalogs(htmlDoc);
                if (blocks.Length == 0)
                {
                    return new CatalogItem[0];
                }

                var catalogItems = new Dictionary<Block, CatalogItem[]>();

                foreach (var block in blocks)
                {
                    var items = ExtractCatalogItems(htmlDoc, block);
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
                throw new NotImplementedException();
            }
        }

        private static Block[] EvaluateCatalogs(HtmlDocument htmlDoc)
        {
            // enumerate all links
            var linkNodes = htmlDoc.DocumentNode.SelectNodes("//a");
            if (linkNodes == null)
            {
                return new Block[0];
            }

            // extract link data
            var links = linkNodes
                .Select(o => new Link
                {
                    XPath = o.XPath,
                    Text = Utilities.NormalizeHtmlText(o.InnerText),
                    Url = o.GetAttributeValue("href", null)
                })
                .ToArray();

            // put similar links together and exclude invalid links
            var similarLinks = GetSimilarValidLinks(links);

            // determine blocks for each links
            List<Block> blocks = new List<Block>();
            similarLinks.ForEach(o => BlockLinks(o.Value, blocks));

#if DEBUG
            // show data for debugging
            var linksPlain = string.Join("\r\n", links.Select(o => o.XPath + "\t" + o.Text));
            var similarLinksPlain = string.Join("\r\n\r\n", similarLinks.Select(o => o.Key + "\r\n" + string.Join("\r\n", o.Value.Select(l => l.XPath + "\t" + l.Text))));
            var blocksPlain = string.Join("\r\n\r\n", blocks.Select(o => o.LinkXPath + "\t" + o.LinkCount + o.LinkText));
#endif

            return FilterBlocks(blocks);
        }

        private static CatalogItem[] ExtractCatalogItems(HtmlDocument htmlDoc, Block block)
        {
            var items = new List<CatalogItem>();

            var itemIterator = htmlDoc.CreateNavigator().Select(block.ContainerPath);
            string linkUrl;
            string linkTitle;
            CatalogItem linkItem;
            while (itemIterator.MoveNext())
            {
                linkUrl = itemIterator.Current.GetValue(block.RelativeLinkXPath + "/@href");
                linkTitle = itemIterator.Current.GetValue(block.RelativeLinkXPath);

                if (string.IsNullOrEmpty(linkUrl))
                {
                    // skip invalid list item
                    continue;
                }

                linkItem = new CatalogItem
                {
                    Url = linkUrl,
                    Title = Utilities.NormalizeHtmlText(linkTitle),
                    FullText = Utilities.NormalizeHtmlText(itemIterator.Current.Value),
                    Published = Html2Article.GetPublishDate(itemIterator.Current.Value),
                    PublishedStr = Html2Article.GetPublishDateStr(itemIterator.Current.Value)
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

        private static Dictionary<string, Link[]> GetSimilarValidLinks(Link[] links)
        {
            var noiseAreaExp = new Regex(@"\b(header|footer|aside|nav|abbr)\b", RegexOptions.IgnoreCase);
            var nodeIndexExp = new Regex(@"\[\d+\]");

            // exlucde invalid links
            var validLinks = links
                .Where(o => o.Url != null
                    && !o.Url.StartsWith("#")
                    && !o.Url.StartsWith("javascript", StringComparison.CurrentCultureIgnoreCase)
                    && !noiseAreaExp.IsMatch(o.XPath) // exclude noise area links
                )
                .ToArray();

            // group similar links
            return links.GroupBy(o => nodeIndexExp.Replace(o.XPath, ""))
                .Where(o => o.Count() >= Constants.RULE_CATALOG_LIST_MIN_LINKCOUNT // exclude small link blocks
                    && o.Max(l => l.Text?.Length ?? 0) >= Constants.RULE_CATALOG_LIST_MIN_LINKTEXT_LEN // exclude short text link blocks, NOTICE: couldn't use average tex length here as we haven't exluced all noise links which might degrade the average score
                )
                .ToDictionary(o => o.Key, o => o.ToArray());
        }

        private static void BlockLinks(Link[] similarLinks, List<Block> blocks)
        {
            Block block = null;
            Link link;
            string genericXPath;
            for (var i = 0; i < similarLinks.Length; i++)
            {
                link = similarLinks[i];

                if (block == null) // start 1st item
                {
                    //start new block
                    block = new Block
                    {
                        LinkXPath = link.XPath,
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
                    genericXPath = GetCommonPath(block.LinkXPath, similarLinks[i - 1].XPath, link.XPath);

                    if (!string.IsNullOrEmpty(genericXPath)
                        // accepts 2-level depth of nested catalog lists at most
                        /*&& expAnyIndex.Matches(genericXPath).Count <= Constants.RULE_CATALOG_LIST_NESTED_MAX_LEVEL*/)
                    {
                        block.LinkXPath = genericXPath;
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
        /// Works even when <see cref="previous"/> and <see cref="current"/> don't have similar xpath hierarchy
        /// </summary>
        /// <param name="commonPath"></param>
        /// <param name="previous"></param>
        /// <param name="current"></param>
        /// <returns></returns>
        private static string GetCommonPath(string commonPath, string previous, string current)
        {
            var nodeExp = new Regex(@"^(\w+)(\[([*\d]+)\])?$");

            var segments0 = commonPath.Split('/');
            var segments1 = previous.Split('/');
            var segments2 = current.Split('/');

            if (segments1.Length != segments2.Length)
            {
                return null;
            }

            Match nodeMatch0;
            Match nodeMatch1;
            Match nodeMatch2;
            int index1;
            int index2;
            //bool newInterationStarted = false;
            for (var i = 0; i < segments1.Length; i++)
            {
                // identical path segments which including index
                if (segments1[i] == segments2[i])
                {
                    continue;
                }

                nodeMatch0 = nodeExp.Match(segments0[i]);
                nodeMatch1 = nodeExp.Match(segments1[i]);
                nodeMatch2 = nodeExp.Match(segments2[i]);

                // check tag name
                if (nodeMatch1.Groups[1].Value != nodeMatch2.Groups[1].Value)
                {
                    return null;
                }

                int.TryParse(nodeMatch1.Groups[3].Value, out index1);
                int.TryParse(nodeMatch2.Groups[3].Value, out index2);

                // check tag index when index1 and index2 are different
                if (nodeMatch0.Groups[3].Value != "*") // new iteration
                {
                    if (index1 == 1 && index2 == index1 + 1) // new iteration must start from 1, and continuous
                    {
                        segments0[i] = nodeExp.Replace(segments0[i], "$1[*]");

                        //newInterationStarted = true;
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (index2 != index1 + 1 // new index isn't continuous
                    && index2 != 1 // new index doesn't start from 1
                )
                {
                    return null;
                }
            }

            return string.Join("/", segments0);
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
        public string LinkXPath { get; set; }
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

        public string ContainerPath
        {
            get
            {
                if (string.IsNullOrEmpty(LinkXPath))
                {
                    return string.Empty;
                }

                int index = LinkXPath.LastIndexOf("[*]");

                return index == -1 ? LinkXPath : LinkXPath.Substring(0, index);
            }
        }

        public string RelativeLinkXPath
        {
            get
            {
                if (string.IsNullOrEmpty(LinkXPath))
                {
                    return string.Empty;
                }

                var xpath = LinkXPath.Substring(ContainerPath.Length);
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
        public string Title { get; set; }
        public string Url { get; set; }
        public string FullText { get; set; }
        public DateTime? Published { get; set; }
        /// <summary>
        /// Indicates a date/time string, which might not even be a full date (e.g. exclude year part).
        /// </summary>
        public string PublishedStr { get; set; }
        public bool HasDate
        {
            get
            {
                return Published != null || !string.IsNullOrEmpty(PublishedStr);
            }
        }
    }
}
