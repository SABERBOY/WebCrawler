namespace WebCrawler.Common
{
    public static class Constants
    {
        public const string HTTP_CLIENT_NAME_DEFAULT = "default";
        public const string HTTP_CLIENT_NAME_NOREDIRECT = "no redirect";

        public const string EXP_DATE_TIME = @"((\d{4}|\d{2})(\-|\/)\d{1,2}\3\d{1,2})(\s?\d{2}:\d{2})?|(\d{4}年\d{1,2}月\d{1,2}日)(\s?\d{2}:\d{2})?";
        public const string EXP_TrimText = @"(^[ \r\n]+|[ \r\n]+$|[ ]{2,}|(\r\n){2,})";

        public const int RULE_CATALOG_LIST_NESTED_MAX_LEVEL = 2;
    }
}
