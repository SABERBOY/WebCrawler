using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.XPath;

namespace WebCrawler.Common
{
    public static class Extensions
    {
        #region XPathNavigator Extensions

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
                return default;
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

        #endregion

        #region PagedResult

        public static PagedResult<T> ToPagedResult<T>(this IEnumerable<T> source, int page, int pageSize = Constants.PAGER_PAGE_SIZE)
        {
            return new PagedResult<T>
            {
                Items = source.Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList(),
                PageInfo = new PageInfo
                {
                    CurrentPage = page,
                    ItemCount = source.Count(),
                    PageSize = pageSize
                }
            };
        }

        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> source, int page, int pageSize = Constants.PAGER_PAGE_SIZE)
        {
            return new PagedResult<T>
            {
                Items = await source.Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(),
                PageInfo = new PageInfo
                {
                    CurrentPage = page,
                    ItemCount = await source.CountAsync(),
                    PageSize = pageSize
                }
            };
        }

        #endregion

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
            {
                action?.Invoke(item);
            }
        }

        public static string GetDisplayName(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    var attr = Attribute.GetCustomAttribute(field, typeof(DisplayAttribute)) as DisplayAttribute;

                    return attr?.Name;
                }
            }
            return null;
        }

        public static string GetDescription(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    var attr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;

                    return attr?.Description;
                }
            }
            return null;
        }

        public static string GetAggregatedMessage(this AggregateException aex)
        {
            return null;
        }

        public static int FirstIndex<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            int index = -1;
            foreach (var item in source)
            {
                index++;
                if (predicate(item))
                {
                    return index;
                }
            }

            return -1;
        }


        public static int LastIndex<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            source = source.Reverse();

            int index = source.Count();
            foreach (var item in source)
            {
                index--;
                if (predicate(item))
                {
                    return index;
                }
            }

            return -1;
        }

        public static T DeepCopy<T>(this T obj)
        {
            string data = JsonConvert.SerializeObject(obj);

            return JsonConvert.DeserializeObject<T>(data);
        }

        public static bool DetermineWebsiteBroken(this Exception ex)
        {
            if (ex is TaskCanceledException)
            {
                // connection timeout is considered as broken
                return Regex.IsMatch(ex.Message, SystemErrorMessages.HTTP_TIMEOUT);
            }
            else if (ex is HttpRequestException)
            {
                // redirections are not considered as broken
                return !Regex.IsMatch(ex.Message, SystemErrorMessages.HTTP_3XX);
            }

            return false;
        }
    }
}
