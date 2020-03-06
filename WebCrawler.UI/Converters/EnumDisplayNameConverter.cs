using System;
using System.Windows.Data;
using WebCrawler.Common;

namespace WebCrawler.UI.Converters
{
    public class EnumDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Enum v = (Enum)value;

            if (v == null)
            {
                return null;
            }

            string dispName = v.GetDisplayName();

            return string.IsNullOrEmpty(dispName) ? v.ToString() : dispName;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
