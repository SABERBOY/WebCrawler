using Microsoft.Win32;
using System;
using System.Diagnostics;

namespace WebCrawler.Common
{
    public static class BrowserEmulation
    {
        /// <summary>
        /// 11001 (0x2AF9):  Internet Explorer 11.Webpages are displayed in IE11 Standards mode, regardless of the!DOCTYPE directive.
        /// 11000(0x2AF8):   Internet Explorer 11.Webpages containing standards-based!DOCTYPE directives are displayed in IE11 mode.
        /// </summary>
        public static readonly uint BROWSER_EMULATION = 11001;

        public static bool GetBrowserEmulation()
        {
            string exeName = Process.GetCurrentProcess().MainModule.ModuleName;

            try
            {
                using (var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true))
                {
                    object value = rk.GetValue(exeName);

                    return value != null && (int)value == BROWSER_EMULATION;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static void EnableBrowserEmulation()
        {
            if (!GetBrowserEmulation())
            {
                SetBrowserEmulation();
            }
        }

        public static void DisableBrowserEmulation()
        {
            if (GetBrowserEmulation())
            {
                SetBrowserEmulation(true);
            }
        }

        /// <summary>
        /// Reference:
        /// https://weblog.west-wind.com/posts/2011/May/21/Web-Browser-Control-Specifying-the-IE-Version
        /// Non-IE browser options:
        /// https://stackoverflow.com/questions/18119125/options-for-embedding-chromium-instead-of-ie-webbrowser-control-with-wpf-c
        /// </summary>
        /// <param name="uninstall"></param>
        private static void SetBrowserEmulation(bool uninstall = false)
        {
            string exeName = Process.GetCurrentProcess().MainModule.ModuleName;

            try
            {
                using (var rk = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true))
                {
                    if (!uninstall)
                    {
                        object value = rk.GetValue(exeName);
                        if (value == null)
                        {
                            rk.SetValue(exeName, BROWSER_EMULATION, RegistryValueKind.DWord);
                        }
                    }
                    else
                    {
                        rk.DeleteValue(exeName);
                    }
                }
            }
            catch (Exception)
            {

            }
        }
    }
}
