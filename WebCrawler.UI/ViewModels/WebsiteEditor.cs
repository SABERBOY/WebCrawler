using WebCrawler.UI.Models;

namespace WebCrawler.UI.ViewModels
{
    public class WebsiteEditor : NotifyPropertyChanged
    {
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

        private int _rank;
        public int Rank
        {
            get
            {
                return _rank;
            }
            set
            {
                if (value == _rank)
                {
                    return;
                }

                _rank = value;
                RaisePropertyChanged();
            }
        }

        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (value == _name)
                {
                    return;
                }

                _name = value;
                RaisePropertyChanged();
            }
        }

        private string _home;
        public string Home
        {
            get
            {
                return _home;
            }
            set
            {
                if (value == _home)
                {
                    return;
                }

                _home = value;
                RaisePropertyChanged();
            }
        }

        private string _urlFormat;
        public string UrlFormat
        {
            get
            {
                return _urlFormat;
            }
            set
            {
                if (value == _urlFormat)
                {
                    return;
                }

                _urlFormat = value;
                RaisePropertyChanged();
            }
        }

        private int? _startIndex;
        public int? StartIndex
        {
            get
            {
                return _startIndex;
            }
            set
            {
                if (value == _startIndex)
                {
                    return;
                }

                _startIndex = value;
                RaisePropertyChanged();
            }
        }

        private string _listPath;
        public string ListPath
        {
            get
            {
                return _listPath;
            }
            set
            {
                if (value == _listPath)
                {
                    return;
                }

                _listPath = value;
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

        private WebsiteStatus _status;
        public WebsiteStatus Status
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

                if (value != WebsiteStatus.Normal && value != WebsiteStatus.WarningNoDates)
                {
                    Enabled = false;
                }
            }
        }

        private bool _enabled;
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                if (value == _enabled)
                {
                    return;
                }

                _enabled = value;
                RaisePropertyChanged();
            }
        }

        public WebsiteEditor(WebsiteView website)
        {
            Id = website.Id;
            Rank = website.Rank;
            Name = website.Name;
            Home = website.Home;
            UrlFormat = website.UrlFormat;
            StartIndex = website.StartIndex;
            ListPath = website.ListPath;
            Notes = website.Notes;
            Status = website.Status;
            // Enabled should be assigned after Status, otherwise it might be overwritten
            Enabled = website.Enabled;
        }

        public void MergeTo(WebsiteView website)
        {
            website.Id = Id;
            website.Rank = Rank;
            website.Name = Name;
            website.Home = Home;
            website.UrlFormat = UrlFormat;
            website.StartIndex = StartIndex;
            website.ListPath = ListPath;
            website.Notes = Notes;
            website.Enabled = Enabled;
            website.Status = Status;
        }
    }
}
