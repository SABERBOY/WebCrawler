using System;
using WebCrawler.UI.Models;

namespace WebCrawler.UI.ViewModels
{
    public class CrawlLogView : NotifyPropertyChanged
    {
        #region Notify Properties

        private int _id;
        public int Id
        {
            get
            {
                return _id;
            }
            set
            {
                if (value == _id)
                {
                    return;
                }

                _id = value;
                RaisePropertyChanged();
            }
        }

        private int _websiteId;
        public int WebsiteId
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

        private int _crawlId;
        public int CrawlId
        {
            get
            {
                return _crawlId;
            }
            set
            {
                if (value == _crawlId)
                {
                    return;
                }

                _crawlId = value;
                RaisePropertyChanged();
            }
        }

        private string _lastHandled;
        public string LastHandled
        {
            get
            {
                return _lastHandled;
            }
            set
            {
                if (value == _lastHandled)
                {
                    return;
                }

                _lastHandled = value;
                RaisePropertyChanged();
            }
        }

        private int _success;
        public int Success
        {
            get
            {
                return _success;
            }
            set
            {
                if (value == _success)
                {
                    return;
                }

                _success = value;
                RaisePropertyChanged();
            }
        }

        private int _failed;
        public int Fail
        {
            get
            {
                return _failed;
            }
            set
            {
                if (value == _failed)
                {
                    return;
                }

                _failed = value;
                RaisePropertyChanged();
            }
        }

        private string _notes;
        public string Notes
        {
            get
            {
                return _notes;
            }
            set
            {
                if (value == _notes)
                {
                    return;
                }

                _notes = value;
                RaisePropertyChanged();
            }
        }

        private CrawlStatus _status;
        public CrawlStatus Status
        {
            get
            {
                return _status;
            }
            set
            {
                if (value == _status)
                {
                    return;
                }

                _status = value;
                RaisePropertyChanged();
            }
        }

        private DateTime? _crawled;
        public DateTime? Crawled
        {
            get
            {
                return _crawled;
            }
            set
            {
                if (value == _crawled)
                {
                    return;
                }

                _crawled = value;
                RaisePropertyChanged();
            }
        }

        private string _websiteName;
        public string WebsiteName
        {
            get
            {
                return _websiteName;
            }
            set
            {
                if (value == _websiteName)
                {
                    return;
                }

                _websiteName = value;
                RaisePropertyChanged();
            }
        }

        private string _websiteHome;
        public string WebsiteHome
        {
            get
            {
                return _websiteHome;
            }
            set
            {
                if (value == _websiteHome)
                {
                    return;
                }

                _websiteHome = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        public CrawlLogView()
        {

        }

        public CrawlLogView(CrawlLog model)
        {
            Id = model.Id;
            WebsiteId = model.WebsiteId;
            WebsiteName = model.Website.Name;
            WebsiteHome = model.Website.Home;
            LastHandled = model.LastHandled;
            Success = model.Success;
            Fail = model.Fail;
            Status = model.Status;
            Notes = model.Notes;
            CrawlId = model.CrawlId;
            Crawled = model.Crawled;
        }
    }
}
