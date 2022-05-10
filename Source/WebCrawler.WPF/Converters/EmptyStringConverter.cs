namespace WebCrawler.WPF.Converters
{
    public class EmptyStringConverter : BinaryConverter
    {
        public override bool Convert(object value, object parameter)
        {
            return value == null || string.IsNullOrEmpty(value.ToString());
        }
    }
}
