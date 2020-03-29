﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WebCrawler.Common.Analyzers
{
    public static class HtmlAnalyzer
    {
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

        public static CatalogItem[] DetectCatalogItems(HtmlDocument htmlDoc, string listPath = null)
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
                return GetCatalogItems(htmlDoc, new Block { LinkPath = listPath });
            }
        }

        public static string DetectListPath(HtmlDocument htmlDoc, string xpath)
        {
            var nodeIndexExp = new Regex(@"\[\d+\]");

            var genericPath = nodeIndexExp.Replace(xpath, "");

            var links = GetValidLinks(htmlDoc);

            var similarLinks = links
                .Where(o => nodeIndexExp.Replace(o.XPath, "") == genericPath)
                .ToArray();

            // build link trees
            var linkTrees = BuildLinkTrees(similarLinks);

            // locate the target link tree
            var linkTree = linkTrees.FirstOrDefault(o => o.GetDescendants(true).Any(d => d.Path == xpath));

            if (linkTree == null)
            {
                return null;
            }

            linkTrees.ForEach(o => PopulatePublishDate(o, htmlDoc));

            linkTree = RemoveNoiseBranches(linkTree);

            var block = linkTree.ConvertToBlock();
#if DEBUG
            // show data for debugging
            var linksPlain = string.Join("\r\n", links.Select(o => o.XPath + "\t" + o.Text));
            var similarLinksPlain = string.Join("\r\n", similarLinks.Select(l => l.XPath + "\t" + l.Text));
            var linkTreeCleanPlain = PrintLinkTree(linkTree);
            var blockPlain = block.LinkPath + "\t" + block.LinkCount + "\t" + block.LinkTextLength + "\r\n" + block.LinkText;
#endif

            return block.LinkPath;
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

            // build link trees
            var linkTrees = similarLinks.SelectMany(o => BuildLinkTrees(o.Value)).ToArray();

#if DEBUG
            // show data for debugging
            var linksPlain = string.Join("\r\n", links.Select(o => o.XPath + "\t" + o.Text));
            var similarLinksPlain = string.Join("\r\n\r\n", similarLinks.Select(o => o.Key + "\r\n" + string.Join("\r\n", o.Value.Select(l => l.XPath + "\t" + l.Text))));
            var linkTreesPlain = string.Join("\r\n\r\n", linkTrees.Select(o => PrintLinkTree(o)));
#endif

            linkTrees.ForEach(o => PopulatePublishDate(o, htmlDoc));

            var linkTreesWithPublishDatePlain = string.Join("\r\n\r\n", linkTrees.Select(o => PrintLinkTree(o)));

            linkTrees = linkTrees.Select(o => RemoveNoiseBranches(o)).ToArray();

            var linkTreesCleanPlain = string.Join("\r\n\r\n", linkTrees.Select(o => PrintLinkTree(o)));

            var blocks = linkTrees.Select(o => o.ConvertToBlock());
            var blocksPlain = string.Join("\r\n\r\n", blocks.Select(o => o.LinkPath + "\t" + o.LinkCount + "\t" + o.LinkTextLength + "\r\n" + o.LinkText));

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
                    PublishedRaw = Html2Article.GetPublishDateRaw(blockNode.InnerText)
                };

                items.Add(linkItem);
            }

            return items
                .GroupBy(o => o.Url)
                // pick the first link which contains text if duplicate links detected
                // duplicate links might be removed already in RemoveNoiseBranches call, but just in case the scenarios without the RemoveNoiseBranches call.
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

        private static Block[] FilterBlocks(IEnumerable<Block> blocks)
        {
            var query = blocks
              .Where(o => (double)o.LinkTextLength / o.LinkCount >= Constants.RULE_CATALOG_LIST_MIN_LINKTEXT_LEN // exclude short link text blocks
                  && o.LinkCount >= Constants.RULE_CATALOG_LIST_MIN_LINKCOUNT // exclude small set links blocks
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

            return query.ToArray();
        }

        private static LinkTreeNode[] BuildLinkTrees(Link[] links)
        {
            List<LinkTreeNode> trees = new List<LinkTreeNode>();

            LinkTreeNode prevLeaf = null;
            LinkTreeNode temp;
            foreach (var link in links)
            {
                var curLeaf = new LinkTreeNode(link);

                if (prevLeaf == null) // new tree
                {
                    trees.Add(curLeaf);
                }
                else
                {
                    var parentPath = curLeaf.GetParentPath(prevLeaf, out int parentDepth);

                    temp = prevLeaf;
                    while (temp.Parent != null && temp.Parent.Depth > parentDepth) // locate the ascendant container
                    {
                        temp = temp.Parent;
                    }

                    var index1 = temp.GetIndex(parentPath);
                    var index2 = curLeaf.GetIndex(parentPath);

                    if (temp.Parent == null) // travel up, new iteration
                    {
                        if (index1 == 1 && index2 == index1 + 1) // new iteration must start from 1, and continuous
                        {
                            new LinkTreeNode(parentPath).UpdateRelations(null, temp, curLeaf);
                        }
                        else // start new tree
                        {
                            curLeaf = null;
                        }
                    }
                    else if (temp.Parent.Path == parentPath) // same match
                    {
                        if (index2 == index1 + 1) // new index is continuous
                        {
                            curLeaf.UpdateRelations(temp.Parent);
                        }
                        else // start new tree
                        {
                            curLeaf = null;
                        }
                    }
                    else if (temp.Parent.Depth < parentDepth) // travel down
                    {
                        if (index2 == index1 + 1) // new index is continuous
                        {
                            new LinkTreeNode(parentPath).UpdateRelations(temp.Parent, temp, curLeaf);
                        }
                        else // start new tree
                        {
                            curLeaf = null;
                        }
                    }
                }

                prevLeaf = curLeaf;
            }

            return trees
                .Select(o => o.GetRoot())
                .ToArray();
        }

        private static void PopulatePublishDate(LinkTreeNode root, HtmlDocument htmlDoc)
        {
            var text = htmlDoc.DocumentNode.SelectSingleNode(root.RelativeRoot).InnerText;
            root.PublishedRaw = Html2Article.GetPublishDateRaw(text);

            root.Children.ToArray().ForEach(o => PopulatePublishDate(o, htmlDoc));
        }

        /// <summary>
        /// Try to identify the iteration root by date/time, and remove other noise branches, and also remove the noise columns inside each iteration list rows.
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        private static LinkTreeNode RemoveNoiseBranches(LinkTreeNode root)
        {
            var treeNodes = root.GetDescendants();
            var leafNodes = treeNodes.Where(o => o.Link != null).ToArray();

            // seek for a node with the most date/time children, and all its children (wanted list items iteration) have date/time
            var datedRoot = treeNodes
                .Where(o => o.Link == null)
                .Select(o => new KeyValuePair<LinkTreeNode, int>(o, o.GetDatedChildrenDepth()))
                .Where(o => o.Value > 0)
                .OrderByDescending(o => o.Value)
                .FirstOrDefault();

            if (datedRoot.Key != null)
            {
                root = datedRoot.Key;

                treeNodes = root.GetDescendants();

                // continue only when lots of nodes have date/time
                if (treeNodes.Count(o => o.HasDate) >= Constants.RULE_CATALOG_LIST_MIN_LINKCOUNT)
                {
                    leafNodes = treeNodes.Where(o => o.Link != null).ToArray();

                    // group the nodes by columns under each list rows
                    var listItemsByColumns = leafNodes.GroupBy(o => string.Join("/", o.Segments.Skip(datedRoot.Value)));

                    // continue only for more than 1 columns
                    if (listItemsByColumns.Count() < leafNodes.Length)
                    {
                        var columnsWithDate = listItemsByColumns.Where(o => o.All(row => row.HasDate)).Select(o => o.Key).ToArray();
                        foreach (var column in listItemsByColumns)
                        {
                            if ((columnsWithDate.Length > 0 && !columnsWithDate.Contains(column.Key)) // exclude columns without date/time
                                || (double)column.Sum(o => o.Link.Text.Length) / column.Count() < Constants.RULE_CATALOG_LIST_MIN_LINKTEXT_LEN // exclude columns with short text links
                            )
                            {
                                // remove noise column nodes from the tree
                                column.ForEach(o => o.UpdateRelations(null));
                            }
                        }

                        leafNodes = root.GetDescendants(true).ToArray();
                        listItemsByColumns = leafNodes.GroupBy(o => string.Join("/", o.Segments.Skip(datedRoot.Value)));

                        var columnsWithDuplicateUrls = listItemsByColumns.Where(o => o.GroupBy(col => col.Link.Url).Count() > 1)
                            .ToDictionary(o => o, o => o.Sum(col => col.Link.Text.Length) / o.Count())
                            .OrderBy(o => o.Value);

                        // the short text links are all removed previously, so we could take the left min length text as the primary link,
                        // as some list item might include links with large block of text.
                        var columnsToBeRemoved = columnsWithDuplicateUrls.Skip(1).Select(o => o.Key);

                        foreach (var column in columnsToBeRemoved)
                        {
                            // remove noise column nodes from the tree
                            column.ForEach(o => o.UpdateRelations(null));
                        }
                    }
                }
            }

            return root.Simplify();
        }

        private static string PrintLinkTree(LinkTreeNode root)
        {
            return root.Children.Count == 0
                ? root.Path + "\t" + root.PublishedRaw + "\t" + root.Link?.Text
                : root.Path + "\t" + root.PublishedRaw + "\r\n" + string.Join("\r\n", root.Children.Select(o => PrintLinkTree(o)));
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
        public string LinkPath { get; set; }
        public List<CatalogItem> Catalogs { get; set; }

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

    public class LinkTreeNode
    {
        public string Path { get; private set; }
        public int Depth { get; private set; }
        public string[] Segments { get; private set; }
        /// <summary>
        /// Available in leaf nodes only
        /// </summary>
        public Link Link { get; private set; }
        public string PublishedRaw { get; set; }

        public LinkTreeNode Parent { get; private set; }
        public List<LinkTreeNode> Children { get; set; }

        /// <summary>
        /// Full root path distinguishs from other siblings under the same parent, which is also the most closed container under the parent.
        /// </summary>
        public string RelativeRoot
        {
            get
            {
                return Parent == null ? Path : string.Join("/", Segments.Take(Parent.Depth + 1));
            }
        }

        public bool HasDate
        {
            get
            {
                return !string.IsNullOrEmpty(PublishedRaw);
            }
        }

        public LinkTreeNode(string path)
        {
            Path = path;
            Segments = Path.Split('/');
            Depth = Segments.Length;
            Children = new List<LinkTreeNode>();
        }

        public LinkTreeNode(Link link)
            : this(link.XPath)
        {
            Link = link;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent">Specify null to remove the parent relation</param>
        /// <param name="children">This wouldn't remove the children which are not listed</param>
        public void UpdateRelations(LinkTreeNode parent, params LinkTreeNode[] children)
        {
            if (parent == null)
            {
                if (Parent != null)
                {
                    Parent.Children.Remove(this);
                }
            }
            else if (parent != Parent)
            {
                if (Parent != null)
                {
                    Parent.Children.Remove(this);
                }

                Parent = parent;
                parent.Children.Add(this);
            }

            children.ForEach(o => o.UpdateRelations(this));
        }

        public string GetParentPath(LinkTreeNode linkNode, out int depth)
        {
            int lvl;
            for (lvl = 0; lvl < Segments.Length && lvl < linkNode.Segments.Length; lvl++)
            {
                if (Segments[lvl] != linkNode.Segments[lvl])
                {
                    break;
                }
            }

            depth = lvl;
            return string.Join("/", Segments.Take(lvl));
        }

        public int GetIndex(string parent)
        {
            if (string.IsNullOrEmpty(parent))
            {
                return 0;
            }
            else
            {
                var firstRelative = Segments.Skip(parent.Split('/').Length).First();
                return int.Parse(Regex.Match(firstRelative, @"\d+").Value);
            }
        }

        public int GetIndex(LinkTreeNode parent = null)
        {
            parent = parent ?? Parent;
            if (parent == null)
            {
                return 0;
            }
            else
            {
                var firstRelative = Segments.Skip(parent.Depth).First();
                return int.Parse(Regex.Match(firstRelative, @"\d+").Value);
            }
        }

        public LinkTreeNode GetRoot()
        {
            var temp = this;

            while (temp.Parent != null)
            {
                temp = temp.Parent;
            }

            return temp;
        }

        public List<LinkTreeNode> GetDescendants(bool leafOnly = false, List<LinkTreeNode> output = null)
        {
            if (output == null)
            {
                output = new List<LinkTreeNode>();
            }

            if (!leafOnly || Link != null)
            {
                output.Add(this);
            }
            Children.ForEach(o => o.GetDescendants(leafOnly, output));

            return output;
        }

        /// <summary>
        /// Get the depth of the children which all have date/time
        /// </summary>
        /// <returns></returns>
        public int GetDatedChildrenDepth()
        {
            if (!HasDate)
            {
                return -1;
            }
            else if (Children.Count == 0 || Children.Any(o => !o.HasDate))
            {
                return Depth;
            }
            else
            {
                return Math.Max(Depth + 1, Children.Min(o => o.GetDatedChildrenDepth()));
            }
        }

        /// <summary>
        /// Path related to the parent
        /// </summary>
        public string GetRelativePath(LinkTreeNode parent = null)
        {
            if (parent == null)
            {
                parent = Parent;
            }

            return string.Join("/", Segments.Skip(parent.Depth));
        }

        public string GetIterationPath()
        {
            var treeNodes = GetDescendants();
            var firstLeafNode = treeNodes.FirstOrDefault(o => o.Link != null);
            if (firstLeafNode == null)
            {
                // invalid tree without leaf nodes
                return null;
            }

            // TODO: /div/a[*] 这样的如何处理？以下逻辑是否也需要将叶子节点包含进来？
            var iterationDepths = treeNodes
                .Where(o => o.Link == null)
                .Select(o => o.Depth)
                .Distinct()
                .OrderBy(o => o)
                .ToArray();

            StringBuilder builder = new StringBuilder();
            string part;
            for (var depth = 0; depth < firstLeafNode.Segments.Length; depth++)
            {
                part = firstLeafNode.Segments[depth];

                if (iterationDepths.Contains(depth))
                {
                    part = Regex.Replace(part, @"\[\d+\]", "[*]");
                }

                builder.AppendFormat("/{0}", part);
            }

            return builder.ToString().Substring(1);
        }

        /// <summary>
        /// Trim the unnecessary so that all nodes have more than one node, unless that's the link node.
        /// </summary>
        /// <returns></returns>
        public LinkTreeNode Simplify()
        {
            if (Children.Count == 0)
            {
                if (Parent == null || Link != null)
                {
                    return this;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                var revisedChildren = Children
                    .ToArray()
                    .Select(o => o.Simplify())
                    .Where(o => o != null)
                    .ToArray();

                if (revisedChildren.Length == 0)
                {
                    if (Parent == null)
                    {
                        Children.Clear();

                        return this;
                    }
                    else
                    {
                        UpdateRelations(null);

                        return null;
                    }
                }
                else if (revisedChildren.Length == 1)
                {
                    revisedChildren[0].UpdateRelations(Parent);
                    UpdateRelations(null);

                    return revisedChildren[0];
                }
                else
                {
                    return this;
                }
            }
        }

        public Block ConvertToBlock()
        {
            var links = GetDescendants(true);
            return new Block
            {
                LinkPath = GetIterationPath(),
                LinkTextLength = links.Sum(o => o.Link.Text.Length),
                LinkCount = links.Count,
#if DEBUG
                LinkText = string.Join("\r\n", links.Select(o => o.Link.Text))
#endif
            };
        }
    }
}
