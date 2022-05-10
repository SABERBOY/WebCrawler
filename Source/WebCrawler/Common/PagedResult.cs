namespace WebCrawler.Common
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; }

        public PageInfo PageInfo { get; set; }
    }
}
