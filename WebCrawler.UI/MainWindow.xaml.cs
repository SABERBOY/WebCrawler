using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Linq;
using System.IO;
using System.Threading;
using System.Globalization;
using WebCrawler.Common;

namespace WebCrawler.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var x = CodePagesEncodingProvider.Instance.GetEncoding("gb2312");

            var urls = new string[] {
                "http://roll.finance.sina.com.cn/finance/gncj/jrxw/index_1.shtml",
                "http://finance.ifeng.com/roll/index.shtml", // ***** 重点 *****
                "http://0745news.cn/news/xianshi/hecheng/",
                "http://www.ocn.com.cn/hongguan/hongguanguancha/",
                "http://www.mof.gov.cn/zhengwuxinxi/caijingshidian/zyzfmhwz/",
                "http://channel.chinanews.com/u/finance/yw.shtml?pager=0",
                "http://economy.jschina.com.cn/gdxw/",
                "http://www.cq.xinhuanet.com/zhengwu/gzdt.htm",
                "http://roll.finance.sina.com.cn/finance/gncj/jrxw/index_1.shtml"
            };

            using (var client = new HttpClient())
            {
                for (var i = 0; i < urls.Length; i++)
                {
                    var data = await client.GetHTMLAsync(urls[i]);

                    var blocks = EvaluateCatalogs(data);

                    ;
                }

                // GetDiffXPath
                // https://github.com/ferventdesert/Hawk/blob/03fbdd0ff4bbdb61db3434adae66c40fa6774641/Hawk.ETL/Crawlers/XPathAnalyzer.cs#L153-L159

                // node.CssSelect(xpath)
                // https://github.com/ferventdesert/Hawk/blob/03fbdd0ff4bbdb61db3434adae66c40fa6774641/Hawk.ETL/Crawlers/XPathAnalyzer.cs#L82-L100

                // TODO: 后期需过滤重复链接
            }
        }

        private Block[] EvaluateCatalogs(string html)
        {
            var htmlDoc = new HtmlDocument();

            htmlDoc.LoadHtml(html);

            var linkNodes = htmlDoc.DocumentNode.SelectNodes("//a[@href][text()]");
            var links = linkNodes.Select(o => new Link
            {
                XPath = o.XPath,
                Text = o.InnerText,
                Url = o.GetAttributeValue("href", null)
            }).ToArray();

            Dictionary<string, string[]> similarities = new Dictionary<string, string[]>();

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
                    genericXPath= GenerateXPathSimilarity(block.LinkXPath, link.XPath);
                    if (!string.IsNullOrEmpty(genericXPath))
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

            var query = blocks.OrderByDescending(o => o.Score);

            var threshold = query.First().Score / 2;

            return query
                .Where(o => o.Score > threshold) // filter high posibility blocks
                .ToArray();
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

        private string GenerateXPathSimilarity(string source, string target)
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
                    else if (IsNumber(current1))
                    {
                        builder.Append('*');

                        while (IsNumber(source[++index1])) { }
                    }
                    else
                    {
                        return null;
                    }

                    if (IsNumber(current2))
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
                    return null;
                }

                int index = LinkXPath.LastIndexOf("[*]");

                return index == -1 ? LinkXPath : LinkXPath.Substring(0, index);
            }
        }
    }
}
