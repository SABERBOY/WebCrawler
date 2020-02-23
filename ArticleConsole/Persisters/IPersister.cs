using ArticleConsole.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArticleConsole.Persisters
{
    public interface IPersister
    {
        Task<Article> GetPreviousAsync(ArticleSource source);
        Task<List<Article>> GetUnTranslatedAsync();
        Task PersistAsync(List<Article> articles, ArticleSource source);
        Task PersistAsync(ArticleZH article);
    }
}
