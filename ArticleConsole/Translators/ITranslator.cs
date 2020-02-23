using System.Collections.Generic;
using System.Threading.Tasks;

namespace ArticleConsole.Translators
{
    public interface ITranslator
    {
        Task<string[]> ExecuteAsync(params string[] content);
    }
}
