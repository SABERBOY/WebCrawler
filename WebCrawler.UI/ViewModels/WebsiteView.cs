using System;
using System.Collections.Generic;
using WebCrawler.UI.Models;

namespace WebCrawler.UI.ViewModels
{
    public class WebsiteView : NotifyPropertyChanged
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

        private DateTime _registered;
        public DateTime Registered
        {
            get
            {
                return _registered;
            }
            set
            {
                if (value == _registered)
                {
                    return;
                }

                _registered = value;
                RaisePropertyChanged();
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

                var previous = _status;

                _status = value;
                RaisePropertyChanged();

                var enabled = DetermineWebsiteEnabledStatus(_status, previous);
                if (enabled != null)
                {
                    Enabled = enabled.Value;
                }
            }
        }

        private string _sysNotes;
        public string SysNotes
        {
            get
            {
                return _sysNotes;
            }
            set
            {
                if (value == _sysNotes)
                {
                    return;
                }

                _sysNotes = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        public List<CrawlLog> CrawlLogs { get; set; }

        public WebsiteView()
        {

        }

        public WebsiteView(Website model)
        {
            Id = model.Id;
            Name = model.Name;
            Home = model.Home;
            Status = model.Status;
            Rank = model.Rank;
            ListPath = model.ListPath;
            UrlFormat = model.UrlFormat;
            StartIndex = model.StartIndex;
            Notes = model.Notes;
            SysNotes = model.SysNotes;
            Registered = model.Registered;
            CrawlLogs = model.CrawlLogs;

            // Enabled should be assigned after Status, otherwise it might be overwritten
            Enabled = model.Enabled;
        }

        /// <summary>
        /// Shadow copy
        /// </summary>
        /// <returns></returns>
        public WebsiteView CloneTo(WebsiteView target = null)
        {
            if (target == null)
            {
                target = new WebsiteView();
            }

            target.Id = Id;
            target.Name = Name;
            target.Home = Home;
            target.Status = Status;
            target.Rank = Rank;
            target.ListPath = ListPath;
            target.UrlFormat = UrlFormat;
            target.StartIndex = StartIndex;
            target.Notes = Notes;
            target.SysNotes = SysNotes;
            target.Registered = Registered;
            target.CrawlLogs = CrawlLogs;

            // Enabled should be assigned after Status, otherwise it might be overwritten
            target.Enabled = Enabled;

            return target;
        }

        public static bool? DetermineWebsiteEnabledStatus(WebsiteStatus current, WebsiteStatus previous)
        {
            if (current == WebsiteStatus.Normal)
            {
                return true;
            }
            else if (current == WebsiteStatus.WarningNoDates || current == WebsiteStatus.WarningRedirected)
            {
                if (current != previous)
                {
                    // disable warnings only for the records to be moved to new status
                    return false;
                }
            }
            else if (current != WebsiteStatus.Normal)
            {
                return false;
            }

            return null;
        }
    }
}
