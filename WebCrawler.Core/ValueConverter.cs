using System;
using System.Collections.Generic;

namespace WebCrawler.Core
{
    public class ValueConverter
    {
        public static T Convert<T>(object value)
        {
            return (T)System.Convert.ChangeType(value, typeof(T));
        }

        public static string[] Split(string values)
        {
            return Split<string>(values);
        }

        public static string[] Split(string values, string separator)
        {
            return Split<string>(values, separator);
        }

        public static T[] Split<T>(string values)
        {
            return Split<T>(values, ",");
        }

        public static T[] Split<T>(string values, string separator)
        {
            if (string.IsNullOrEmpty(values))
            {
                return new T[0];
            }

            string[] segments = values.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries);
            T[] results = new T[segments.Length];
            for (int i = 0; i < segments.Length; i++)
            {
                results[i] = Convert<T>(segments[i]);
            }

            return results;
        }

        public static string Join<T>(IEnumerable<T> values)
        {
            return Join<T>(values, ",");
        }

        public static string Join<T>(IEnumerable<T> values, string separator)
        {
            return values == null ? string.Empty : string.Join(separator, values);
        }

        public static T Parse<T>(string strValue, T defaultValue = default(T))
        {
            if (string.IsNullOrEmpty(strValue))
            {
                return defaultValue;
            }

            var type = typeof(T);
            object value;

            if (type == typeof(int))
            {
                value = int.Parse(strValue);
            }
            else if (type == typeof(float))
            {
                value = float.Parse(strValue);
            }
            else if (type == typeof(double))
            {
                value = double.Parse(strValue);
            }
            else if (type == typeof(decimal))
            {
                value = decimal.Parse(strValue);
            }
            else if (type == typeof(bool))
            {
                value = bool.Parse(strValue);
            }
            else if (type == typeof(DateTime))
            {
                value = DateTime.Parse(strValue);
            }
            else // reference value
            {
                value = strValue;
            }

            return (T)value;
        }
    }
}
