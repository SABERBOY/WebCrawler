using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebCrawler.UI.ViewModels;

namespace WebCrawler.UI.Common
{
    public static class Extensions
    {
        #region PagedResult

        public static PagedResult<T> ToPagedResult<T>(this IEnumerable<T> source, int page, int pageSize = Constants.PAGE_SIZE)
        {
            return new PagedResult<T>
            {
                Items = source.Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList(),
                Pager = new Pager
                {
                    CurrentPage = page,
                    ItemCount = source.Count(),
                    PageSize = pageSize
                }
            };
        }

        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> source, int page, int pageSize = Constants.PAGE_SIZE)
        {
            return new PagedResult<T>
            {
                Items = await source.Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(),
                Pager = new Pager
                {
                    CurrentPage = page,
                    ItemCount = await source.CountAsync(),
                    PageSize = pageSize
                }
            };
        }

        #endregion
    }
}
