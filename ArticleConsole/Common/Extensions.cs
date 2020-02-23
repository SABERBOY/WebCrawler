using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.XPath;
using System.Linq;

namespace ArticleConsole.Common
{
    public static class Extensions
    {
        public static string GetValue(this XPathNavigator xNav, string xpath)
        {
            if (string.IsNullOrEmpty(xpath))
            {
                return string.Empty;
            }

            var pathValue = xNav.SelectSingleNode(xpath);

            return pathValue?.Value;
        }

        public static string[] GetValues(this XPathNavigator xNav, string xpath)
        {
            if (string.IsNullOrEmpty(xpath))
            {
                return new string[0];
            }

            var iterator = xNav.Select(xpath);

            List<string> values = new List<string>();

            while (iterator.MoveNext())
            {
                values.Add(iterator.Current.Value);
            }

            return values.ToArray();
        }

        public static T GetValue<T>(this XPathNavigator xNav, string xpath)
        {
            if (string.IsNullOrEmpty(xpath))
            {
                return default(T);
            }

            var pathValue = xNav.SelectSingleNode(xpath);

            return ValueConverter.Parse<T>(pathValue?.Value);
        }

        public static T[] GetValues<T>(this XPathNavigator xNav, string xpath)
        {
            if (string.IsNullOrEmpty(xpath))
            {
                return new T[0];
            }

            var iterator = xNav.Select(xpath);

            List<string> values = new List<string>();

            while (iterator.MoveNext())
            {
                values.Add(iterator.Current.Value);
            }

            return values.Select(o => ValueConverter.Parse<T>(o)).ToArray();
        }

        public static string GetInnerHTML(this XPathNavigator xNav, string xpath)
        {
            if (string.IsNullOrEmpty(xpath))
            {
                return string.Empty;
            }

            var pathValue = xNav.SelectSingleNode(xpath);

            return pathValue?.InnerXml;
        }

        public static string GetAggregatedMessage(this AggregateException aex)
        {
            return null;
        }
    }
}
