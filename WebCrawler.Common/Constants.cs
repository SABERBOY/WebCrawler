namespace WebCrawler.Common
{
    public static class Constants
    {
        public const string HTTP_CLIENT_NAME_DEFAULT = "default";
        public const string HTTP_CLIENT_NAME_NOREDIRECT = "no redirect";

        public const string EXP_DATE_TIME = @"((\d{4}|\d{2})(\-|\/)\d{1,2}\3\d{1,2})(\s?\d{2}:\d{2})?|(\d{4}年\d{1,2}月\d{1,2}日)(\s?\d{2}:\d{2})?";
        /// <summary>
        /// Clean the page text, but keep single line breaks
        /// </summary>
        public const string EXP_TEXT_CLEAN = @"(^[ \r\n]+|[ \r\n]+$|[ ]{2,}|(\r\n){2,})";
        /// <summary>
        /// Clean the page text, and remove all line breaks
        /// </summary>
        public const string EXP_TEXT_CLEAN_FULL = @"(^[ \r\n]+|[ \r\n]+$|[ ]{2,}|[\r\n]+)";

        public const int RULE_CATALOG_LIST_NESTED_MAX_LEVEL = 2;
    }
}
