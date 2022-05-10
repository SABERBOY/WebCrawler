namespace WebCrawler.UI.Converters
{
    public class EmptyNumberConverter : BinaryConverter
    {
        public override bool Convert(object value, object parameter)
        {
            bool flag = false;
            if (value == null)
            {
                flag = true;
            }
            else if (value is int)
            {
                flag = (int)value == 0;
            }
            else if (value is decimal)
            {
                flag = (decimal)value == 0;
            }
            else if (value is double)
            {
                flag = (double)value == 0;
            }
            else if (value is float)
            {
                flag = (float)value == 0;
            }

            return flag;
        }
    }
}
