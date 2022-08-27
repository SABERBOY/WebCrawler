using System.IO;
using System.Reflection;

namespace WebCrawler.Common
{
    public static class WCUtility
    {
        public static string GetAppTempFolder()
        {
            return Path.Combine(Path.GetTempPath(), Assembly.GetEntryAssembly().FullName.Split(',')[0]);
        }
    }
}
