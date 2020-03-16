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
                    throw new Exception("Failed to detect link blocks");
                }

                var catalogItems = new Dictionary<Block, CatalogItem[]>();

                foreach (var block in blocks)
                {
                    catalogItems.Add(block, ExtractCatalogItems(htmlDoc, block));
                }

                return catalogItems
                    // blocks with published date has higher priority
                    .OrderByDescending(o => o.Value.First().Published != null)
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
            var linkNodes = htmlDoc.DocumentNode.SelectNodes("//a[@href][text()]");
            if (linkNodes == null)
            {
                return new Block[0];
            }

            var links = linkNodes.Select(o => new Link
            {
                XPath = o.XPath,
                Text = TrimText(o.InnerText),
                Url = o.GetAttributeValue("href", null)
            }).ToArray();

            // test code
            //var test = links.Select(o => new KeyValuePair<string, string>(o.XPath, o.Text)).ToArray();

            Dictionary<string, string[]> similarities = new Dictionary<string, string[]>();

            var expAnyIndex = new Regex("[*]");

            List<Block> blocks = new List<Block>();
            Block block = null;
            Link link;
            string genericXPath;
            for (var j = 0; j < links.Length; j++)
            {
                link = links[j];

                if (block == null) // start 1st item
                {
                    //start new block
                    block = new Block
                    {
                        LinkXPath = link.XPath,
                        MatchCount = 1,
                        LinkTextLength = link.Text.Length
                    };

                    blocks.Add(block);
                }
                else // compare with the items after
                {
                    genericXPath = GenerateXPathSimilarity(block.LinkXPath, link.XPath);
                    if (!string.IsNullOrEmpty(genericXPath)
                        // accepts 2-level depth of nested catalog lists at most
                        && expAnyIndex.Matches(genericXPath).Count <= Constants.RULE_CATALOG_LIST_NESTED_MAX_LEVEL)
                    {
                        block.LinkXPath = genericXPath;
                        block.MatchCount++;
                        block.LinkTextLength += link.Text.Length;
                    }
                    else
                    {
                        // move back to transact again
                        j--;
                        // clear block
                        block = null;
                    }
                }
            }

            var expExcludeSections = new Regex(@"\b(header|footer|aside|nav|abbr)\b", RegexOptions.IgnoreCase);

            var query = blocks
                .Where(o => !expExcludeSections.IsMatch(o.LinkXPath))
                .OrderByDescending(o => o.Score);

            var threshold = query.First().Score * Constants.RULE_CATALOG_BLOCK_MINSCORE_FACTOR;

            return query
                .Where(o => o.Score > threshold) // pick high posibility blocks
                .ToArray();
        }

        private static CatalogItem[] ExtractCatalogItems(HtmlDocument htmlDoc, Block block)
        {
            var items = new List<CatalogItem>();

            var itemIterator = htmlDoc.CreateNavigator().Select(block.ContainerPath);
            string linkUrl;
            string linkTitle;
            while (itemIterator.MoveNext())
            {
                linkUrl = itemIterator.Current.GetValue(block.RelativeLinkXPath + "/@href");
                linkTitle = itemIterator.Current.GetValue(block.RelativeLinkXPath);

                if (string.IsNullOrEmpty(linkUrl))
                {
                    // skip invalid list item
                    continue;
                }

                items.Add(new CatalogItem
                {
                    Url = linkUrl,
                    Title = TrimText(linkTitle),
                    FullText = TrimText(itemIterator.Current.Value),
                    Published = Html2Article.GetPublishDate(itemIterator.Current.Value)
                });
            }

            var results = items
                .GroupBy(o => o.Url)
                // pick the first link which contains text if duplicate links detected
                .Select(o => o.FirstOrDefault(lnk => !string.IsNullOrEmpty(lnk.Title)))
                .Where(o => o != null)
                .ToArray();

            // the published is considered as valid only when valid in all catalog items
            if (results.Any(o => o.Published == null))
            {
                results.ForEach(o => o.Published = null);
            }

            return results;
        }

        private static string TrimText(string text, bool persistLineBreaks = false)
        {
            return string.IsNullOrEmpty(text)
                ? string.Empty
                : Regex.Replace(text, persistLineBreaks ? Constants.EXP_TEXT_CLEAN : Constants.EXP_TEXT_CLEAN_FULL, "");
        }

        //private string[] ExtractXPathSimilarity(string xpath1, string xpath2)
        //{
        //    // assume xpath1 and xpath2 are the 1st and 2nd occurrence of a formatted list in the HTML DOM, so the index and xpath text length should be same if they are in the same list and depth.
        //    if (xpath1.Length != xpath2.Length)
        //    {
        //        return null;
        //    }

        //    List<string> similarity = new List<string>();

        //    StringBuilder builder = new StringBuilder();
        //    char temp;
        //    for (var i = 0; i < xpath1.Length; i++)
        //    {
        //        temp = xpath1[i];

        //        if (temp == xpath2[i])
        //        {
        //            builder.Append(temp);
        //        }
        //        else if (IsNumber(temp))
        //        {
        //            if (builder.Length > 0)
        //            {
        //                similarity.Add(builder.ToString());

        //                builder.Clear();
        //            }
        //        }
        //        else
        //        {
        //            return null;
        //        }
        //    }

        //    if (builder.Length > 0)
        //    {
        //        similarity.Add(builder.ToString());

        //        builder.Clear();
        //    }

        //    return similarity.ToArray();
        //}

        /// <summary>
        /// Analyze if two xpathes match exactly, or the the <see cref="target"/> is a subtring of the <see cref="source"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="exact"></param>
        /// <returns></returns>
        //private int[] CalculateXPathSimilarity(string source, string target, bool exact = true)
        //{
        //    if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
        //    {
        //        return null;
        //    }

        //    List<int> similarity = new List<int>();

        //    char current1;
        //    char current2;
        //    int simiCount = 0;
        //    int index1 = 0;
        //    int index2 = 0;
        //    bool startNew = true;
        //    while (index1 < source.Length && index2 < target.Length)
        //    {
        //        current1 = source[index1];
        //        current2 = target[index2];

        //        if (current1 == current2)
        //        {
        //            if (startNew)
        //            {
        //                similarity.Add(index1);

        //                simiCount = 0;

        //                startNew = false;
        //            }

        //            simiCount++;

        //            index1++;
        //            index2++;
        //        }
        //        else
        //        {
        //            if (IsNumber(current1))
        //            {
        //                index1++;
        //                startNew = true;
        //            }

        //            if (IsNumber(current2))
        //            {
        //                index2++;
        //                startNew = true;
        //            }

        //            if (startNew)
        //            {
        //                similarity.Add(simiCount);
        //            }
        //            else
        //            {
        //                return null;
        //            }
        //        }
        //    }

        //    similarity.Add(simiCount);

        //    if (exact)
        //    {
        //        if (index1 != source.Length || index2 != target.Length)
        //        {
        //            return null;
        //        }
        //    }
        //    else
        //    {
        //        if (index2 != target.Length)
        //        {
        //            return null;
        //        }
        //    }

        //    return similarity.ToArray();
        //}

        private static string GenerateXPathSimilarity(string source, string target)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            {
                return null;
            }

            char current1;
            char current2;
            int index1 = 0;
            int index2 = 0;
            StringBuilder builder = new StringBuilder();
            while (index1 < source.Length && index2 < target.Length)
            {
                current1 = source[index1];
                current2 = target[index2];

                if (current1 == current2)
                {
                    builder.Append(current1);

                    index1++;
                    index2++;
                }
                else
                {
                    if (current1 == '*')
                    {
                        builder.Append('*');

                        index1++;
                    }
                    else if (IsNumber(current1) && source[index1 - 1] == '[') // check previous char as the number might be part of head tags, e.g. h1
                    {
                        builder.Append('*');

                        while (IsNumber(source[++index1])) { }
                    }
                    else
                    {
                        return null;
                    }

                    if (IsNumber(current2) && source[index2 - 1] == '[') // check previous char as the number might be part of head tags, e.g. h1
                    {
                        while (IsNumber(target[++index2])) { }
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            return builder.ToString();
        }

        //private string[] ExtractXPathSimilarity(string source, string target)
        //{
        //    var similarity = CalculateXPathSimilarity(source, target, true);
        //    if (similarity == null)
        //    {
        //        return null;
        //    }

        //    List<string> segments = new List<string>();

        //    for (var i = 0; i < similarity.Length; i += 2)
        //    {
        //        segments.Add(source.Substring(similarity[i], similarity[i + 1]));
        //    }

        //    return segments.ToArray();
        //}

        //private List<int[]> ExtractXPathSimilarity(string xpath, List<int[]> similarity)
        //{
        //    int i;
        //    foreach (var simi in similarity)
        //    {
        //        if (xpath.StartsWith(simi))
        //        {
        //            xpath = xpath.Substring(simi.Length);
        //        }
        //        else if (xpath.Length >= simi.Length)
        //        {
        //            for (i = 0; i < simi.Length; i++)
        //            {
        //                simi[i]
        //            }

        //            while (xpath[0] >= '0' && xpath[0] <= '9')
        //            {
        //                xpath = xpath.Substring(1);
        //            }

        //            if (xpath.StartsWith(simi))
        //            {
        //                xpath = xpath.Substring(simi.Length);
        //            }
        //            else
        //            {
        //                return false;
        //            }
        //        }
        //        else
        //        {
        //            return null;
        //        }
        //    }
        //}

        private static bool IsNumber(char ch)
        {
            return ch >= '0' && ch <= '9';
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
        public int MatchCount { get; set; }
        public int LinkTextLength { get; set; }
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
        public DateTime? Published { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string FullText { get; set; }
    }
}
