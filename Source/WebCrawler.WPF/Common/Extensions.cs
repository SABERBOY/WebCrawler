using System.Reflection;
using System.Windows.Controls;

namespace WebCrawler.WPF.Common
{
    public static class Extensions
    {
        /// <summary>
        /// Call this method in Navigating event.
        /// </summary>
        /// <param name="webBrowser"></param>
        public static void SuppressScriptErrors(this WebBrowser webBrowser)
        {
            bool hide = true;

            FieldInfo fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fiComWebBrowser == null)
            {
                return;
            }

            object objComWebBrowser = fiComWebBrowser.GetValue(webBrowser);
            if (objComWebBrowser == null)
            {
                return;
            }

            objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { hide });
        }
    }
}
