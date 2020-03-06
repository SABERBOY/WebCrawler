using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WebCrawler.UI.Converters
{
    public class BinaryConverter : IValueConverter
    {
        public virtual bool Convert(object value, object parameter)
        {
            return GetBinaryValue(value);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object revisedParam = parameter;
            bool isReverse = false;
            if (parameter != null && parameter is string)
            {
                string temp = parameter.ToString();

                isReverse = temp.StartsWith("!");
                revisedParam = temp.TrimStart('!');
             }

            bool flag = Convert(value, revisedParam);

            if (isReverse)
            {
                flag = !flag;
            }

            if (targetType == typeof(Visibility))
            {
                return flag ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return flag;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = GetBinaryValue(value);

            return GetTargetValue(flag, targetType);
        }

        public static bool GetBinaryValue(object value, bool throwErrorIfNotSupported = true)
        {
            if (value == null)
            {
                return false;
            }

            if (value is bool)
            {
                return (bool)value;
            }
            else if (value is bool?)
            {
                bool? nullable = (bool?)value;
                return nullable.HasValue && nullable.Value;
            }
            else if (value is Visibility)
            {
                return (Visibility)value == Visibility.Visible;
            }

            if (throwErrorIfNotSupported)
            {
                throw new NotSupportedException();
            }

            return false;
        }

        public static object GetTargetValue(bool value, Type targetType)
        {
            if (targetType == typeof(bool))
            {
                return value;
            }
            else if (targetType == typeof(bool?))
            {
                return value;
            }
            else if (targetType == typeof(Visibility))
            {
                return value ? Visibility.Visible : Visibility.Collapsed;
            }

            return value;
        }
    }
}
