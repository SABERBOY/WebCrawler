using System.Threading.Tasks;

namespace ArticleConsole.Translators
{
    public interface ITranslator
    {
        Task ExecuteAsync();
        Task<string[]> TranslateAsync(params string[] content);
    }
}
