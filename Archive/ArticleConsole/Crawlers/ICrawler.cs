using ArticleConsole.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArticleConsole.Crawlers
{
    public interface ICrawler
    {
        Task ExecuteAsync();
    }
}
