using Serilog.Events;
using System;

namespace WebCrawler.UI.ViewModels
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

        private LogEventLevel _level;
        public LogEventLevel Level
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
    }
}
