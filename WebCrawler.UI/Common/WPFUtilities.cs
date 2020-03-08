using System.Linq;
using System.Windows.Controls;

namespace WebCrawler.UI.Common
{
    public static class WPFUtilities
    {
        public static void Navigate(object page)
        {
            var frame = App.Current.MainWindow.FindDescendants<Frame>().FirstOrDefault();

            frame?.Navigate(page);
        }
    }
}
