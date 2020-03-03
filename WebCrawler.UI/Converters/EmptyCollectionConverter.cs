using System.Collections;

namespace WebCrawler.UI.Converters
{
    public class EmptyCollectionConverter : BinaryConverter
    {
        public override bool Convert(object value, object parameter)
        {
            bool isEmpty = true;
            if (value is IEnumerable collection)
            {
                var enumerator = collection.GetEnumerator();
                isEmpty = !enumerator.MoveNext();
            }

            return isEmpty;
        }
    }
}
