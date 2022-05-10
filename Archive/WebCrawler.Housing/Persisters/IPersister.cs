using System;
using System.Threading.Tasks;

namespace WebCrawler.Housing.Persisters
{
    public interface IPersister : IDisposable
    {
        Task SaveAsync<TModel, TKey>(TModel model, TKey key) where TModel : class;
    }
}
