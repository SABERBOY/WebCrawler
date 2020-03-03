using System;
using System.Windows.Data;
using WebCrawler.Common;

namespace WebCrawler.UI.Converters
{
    public class EnumDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Enum v = (Enum)value;

            if (v == null)
            {
                return null;
            }

            string desc = v.GetDescription();

            return string.IsNullOrEmpty(desc) ? v.ToString() : desc;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
