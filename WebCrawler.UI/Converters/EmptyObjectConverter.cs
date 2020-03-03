namespace WebCrawler.UI.Converters
{
    public class EmptyObjectConverter : BinaryConverter
    {
        public override bool Convert(object value, object parameter)
        {
            return value == null;
        }
    }
}
