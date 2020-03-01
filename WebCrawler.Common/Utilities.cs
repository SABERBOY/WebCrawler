using System.Text.RegularExpressions;

namespace WebCrawler.Common
{
    public static class Utilities
    {
        public static string ResolveResourceUrl(string resourceUrl, string pageUrl)
        {
            if (string.IsNullOrEmpty(pageUrl))
            {
                return resourceUrl;
            }
            else if (Regex.IsMatch(resourceUrl, "^https?://"))
            {
                return resourceUrl;
            }
            else if (resourceUrl.StartsWith("//"))
            {
                return pageUrl.Substring(0, pageUrl.IndexOf("://") + 1) + resourceUrl;
            }

            string pageFolder = pageUrl.Substring(0, pageUrl.LastIndexOf('/'));

            string folder = string.Empty;
            string path = resourceUrl;
            // relative URL
            if (resourceUrl.StartsWith("/")) // root site path
            {
                folder = GetRootSiteUrl(pageUrl);
            }
            else if (resourceUrl.StartsWith("./")) // current folder path
            {
                folder = pageFolder;
                path = path.Substring(1);
            }
            else if (resourceUrl.StartsWith("../")) // parrent folder path
            {
                folder = pageFolder;
                do
                {
                    // move to parent folder
                    folder = folder.Substring(0, folder.LastIndexOf('/'));
                    // remove one ../ instance
                    path = path.Substring(3);
                } while (path.StartsWith("../"));

                path = "/" + path;
            }
            else // path related to current folder
            {
                folder = pageFolder;
                path = "/" + resourceUrl;
            }

            return folder + path;
        }

        public static string GetRootSiteUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }

            Match m = Regex.Match(url, @"https?://([^/\r\n ]+)");
            if (m.Success)
            {
                return m.Value;
            }

            return url.ToLower();
        }
    }
}
