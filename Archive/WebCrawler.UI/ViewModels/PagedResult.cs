using System.Collections.Generic;

namespace WebCrawler.UI.ViewModels
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; }

        public PageInfo PageInfo { get; set; }
    }
}
