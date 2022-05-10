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
            get
            {
                return _timestamp;
            }
            set
            {
                if (value == _timestamp)
                {
                    return;
                }

                _timestamp = value;
                RaisePropertyChanged();
            }
        }

        private string _message;
        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                if (value == _message)
                {
                    return;
                }

                _message = value;
                RaisePropertyChanged();
            }
        }

        private LogLevel _level;
        public LogLevel Level
        {
            get
            {
                return _level;
            }
            set
            {
                if (value == _level)
                {
                    return;
                }

                _level = value;
                RaisePropertyChanged();
            }
        }

        private string _url;
        public string URL
        {
            get
            {
                return _url;
            }
            set
            {
                if (value == _url)
                {
                    return;
                }

                _url = value;
                RaisePropertyChanged();
            }
        }

        private int? _websiteId;
        public int? WebsiteId
        {
            get
            {
                return _websiteId;
            }
            set
            {
                if (value == _websiteId)
                {
                    return;
                }

                _websiteId = value;
                RaisePropertyChanged();
            }
        }
    }
}
