using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace WebCrawler.WPF.Controls
{
    public class WebBrowserBehavior
    {
        public static readonly DependencyProperty HtmlProperty = DependencyProperty.RegisterAttached(
            "Html",
            typeof(string),
            typeof(WebBrowserBehavior),
            new FrameworkPropertyMetadata(OnHtmlChanged));

        [AttachedPropertyBrowsableForType(typeof(WebBrowser))]
        public static string GetHtml(WebBrowser d)
        {
            return (string)d.GetValue(HtmlProperty);
        }

        public static void SetHtml(WebBrowser d, string value)
        {
            d.SetValue(HtmlProperty, value);
        }

        static void OnHtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            WebBrowser browser = d as WebBrowser;
            if (browser != null)
            {
                if (browser.Document != null)
                {
                    browser.Navigate("about:blank");
                }

                var revisedHtml = WrapHtml(e.NewValue as string);
                if (!string.IsNullOrEmpty(revisedHtml))
                {
                    browser.NavigateToString(revisedHtml);
                }
            }
        }

        /// <summary>
        /// Reference: Resolve Chinese encoding issue, another approach is to use WinForm WebBrowser.
        /// https://www.xuebuyuan.com/131039.html
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        static string WrapHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return html;
            }

            html = Regex.Replace(html, @"<meta http-equiv=(['""]?)Content-Type\1.*>", "", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<meta charset=[^<>]+>", "", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, @"<head[^<>]*>", "$0<meta charset='utf-8'>");

            return html;
        }
    }
}
