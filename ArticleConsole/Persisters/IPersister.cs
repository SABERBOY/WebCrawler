using ArticleConsole.Models;
using System.Collections.Generic;

namespace ArticleConsole.Persisters
{
    public interface IPersister
    {
        Article GetPrevious(ArticleSource source);
        int GetListCount(TransactionStatus status, ArticleSource? source = null);
        List<Article> GetList(TransactionStatus status, int batchSize, int? from = null, ArticleSource? source = null);
        void Add(List<Article> articles);
        void AddTranslation(ArticleZH article);
        void Update(Article article);
    }
}
