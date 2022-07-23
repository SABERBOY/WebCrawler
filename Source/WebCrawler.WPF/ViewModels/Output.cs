using Microsoft.Extensions.Logging;
using System;
using WebCrawler.Common;

namespace WebCrawler.WPF.ViewModels
{
    public class Output : NotifyPropertyChanged
    {
        private DateTime _timestamp;
        public DateTime Timestamp
        {
            get { return GetPropertyValue<DateTime>(); }
            set { SetPropertyValue(value); }
        }

        private string _message;
        public string Message
        {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue(value); }
        }

        private LogLevel _level;
        public LogLevel Level
        {
            get { return GetPropertyValue<LogLevel>(); }
            set { SetPropertyValue(value); }
        }

        private string _url;
        public string URL
        {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue(value); }
        }

        private int? _websiteId;
        public int? WebsiteId
        {
            get { return GetPropertyValue<int?>(); }
            set { SetPropertyValue(value); }
        }
    }
}
