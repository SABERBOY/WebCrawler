using System.Text;

namespace WebCrawler.Common
{
    public static class AppTools
    {
        public static void ConfigureEnvironment()
        {
            // add encoding support for GB2312 and GDK
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // https://www.npgsql.org/doc/types/datetime.html#timestamps-and-timezones
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }
    }
}
