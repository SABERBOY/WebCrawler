using System;
using System.Linq.Expressions;

namespace WebCrawler.Common
{
    public static class LinqHelper
    {
        public static Expression<Func<TIn, TOut>> CreateKeyAccessor<TIn, TOut>(string property)
        {
            var param = Expression.Parameter(typeof(TIn));
            var body = Expression.PropertyOrField(param, property);

            return Expression.Lambda<Func<TIn, TOut>>(body, param);
        }
    }
}
