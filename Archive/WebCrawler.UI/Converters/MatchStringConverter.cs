namespace WebCrawler.UI.Converters
{
    public sealed class MatchStringConverter : BinaryConverter
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
                return strValue == strParam;
            }
        }
    }
}
