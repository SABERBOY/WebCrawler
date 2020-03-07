using System.Net.Http;
using System.Windows;
using WebCrawler.Common;

namespace WebCrawler.UI.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(Crawler crawler, Manage manage)
        {
            InitializeComponent();

            MainFrame.Navigate(manage);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
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
                    var data = await client.GetHtmlAsync(urls[i]);

                    //var blocks = HtmlAnalyzer.EvaluateCatalogs(data);

                    ;
                }

                // GetDiffXPath
                // https://github.com/ferventdesert/Hawk/blob/03fbdd0ff4bbdb61db3434adae66c40fa6774641/Hawk.ETL/Crawlers/XPathAnalyzer.cs#L153-L159

                // node.CssSelect(xpath)
                // https://github.com/ferventdesert/Hawk/blob/03fbdd0ff4bbdb61db3434adae66c40fa6774641/Hawk.ETL/Crawlers/XPathAnalyzer.cs#L82-L100

                // TODO: 后期需过滤重复链接
            }
        }
    }
}
