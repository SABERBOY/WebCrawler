using System.Linq;
using WebCrawler.Common;

namespace WebCrawler.WPF.Converters
{
    public sealed class MatchEnumConverter : BinaryConverter
    {
        public override bool Convert(object value, object parameter)
        {
            var strValue = value?.ToString();
            var strParam = parameter?.ToString();

            if (string.IsNullOrEmpty(strValue) && string.IsNullOrEmpty(strParam))
            {
                return true;
            }
            else
            {
                return ValueConverter.Split(strParam, ";").Contains(strValue);
            }
        }
    }
}
