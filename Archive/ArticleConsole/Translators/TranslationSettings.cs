namespace ArticleConsole.Translators
{
    public class TranslationSettings
    {
        public string AppId { get; set; }
        public string AppSecret { get; set; }
        public int MaxUTF8BytesPerRequest { get; set; }
        /// <summary>
        /// In milliseconds
        /// </summary>
        public int PausePerRequest { get; set; }
    }
}
