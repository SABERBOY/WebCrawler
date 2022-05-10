using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
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
                PageInfo = new PageInfo
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
                PageInfo = new PageInfo
                {
                    CurrentPage = page,
                    ItemCount = await source.CountAsync(),
                    PageSize = pageSize
                }
            };
        }

        #endregion

        /// <summary>
        /// Call this method in Navigating event.
        /// </summary>
        /// <param name="webBrowser"></param>
        public static void SuppressScriptErrors(this WebBrowser webBrowser)
        {
            bool hide = true;

            FieldInfo fiComWebBrowser = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fiComWebBrowser == null)
            {
                return;
            }

            object objComWebBrowser = fiComWebBrowser.GetValue(webBrowser);
            if (objComWebBrowser == null)
            {
                return;
            }

            objComWebBrowser.GetType().InvokeMember("Silent", BindingFlags.SetProperty, null, objComWebBrowser, new object[] { hide });
        }
    }
}
