namespace WebCrawler.Common
{
    public static class SystemErrorMessages
    {
        public const string UNIQUE_KEY_VIOLATION = @"Duplicate entry .+ for key .+\Wurl_UNIQUE";
        public const string HTTP_TIMEOUT = @"The request was canceled due to the configured HttpClient.Timeout of \d+ seconds elapsing.";
        public const string HTTP_3XX = @"Response status code does not indicate success: 3\d{2}";
    }
}
