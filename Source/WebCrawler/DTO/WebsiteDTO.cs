using System.Collections.ObjectModel;
using WebCrawler.Common;
using WebCrawler.Models;

namespace WebCrawler.DTO
{
    public class WebsiteDTO : NotifyPropertyChanged
    {
        #region Notify Properties

        public int Id
        {
            get { return GetPropertyValue<int>(); }
            set { SetPropertyValue(value); }
        }

        public int Rank
        {
            get { return GetPropertyValue<int>(); }
            set { SetPropertyValue(value); }
        }

        public string Name
        {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue(value); }
        }

        public string Home
        {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue(value); }
        }

        public string UrlFormat
        {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue(value); }
        }

        public int? StartIndex
        {
            get { return GetPropertyValue<int?>(); }
            set { SetPropertyValue(value); }
        }

        public bool ValidateDate
        {
            get { return GetPropertyValue<bool>(); }
            set { SetPropertyValue(value); }
        }

        public string Notes
        {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue(value); }
        }

        public DateTime Registered
        {
            get { return GetPropertyValue<DateTime>(); }
            set { SetPropertyValue(value); }
        }

        public bool Enabled
        {
            get { return GetPropertyValue<bool>(); }
            set { SetPropertyValue(value); }
        }

        public WebsiteStatus Status
        {
            get { return GetPropertyValue<WebsiteStatus>(); }
            set
            {
                var previous = GetPropertyValue<WebsiteStatus>();

                if (!SetPropertyValue(value)) { return; }

                var enabled = DetermineWebsiteEnabledStatus(value, previous);
                if (enabled != null)
                {
                    Enabled = enabled.Value;
                }
            }
        }

        public string SysNotes
        {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue(value); }
        }

        public ObservableCollection<WebsiteRuleDTO> Rules
        {
            get { return GetPropertyValue<ObservableCollection<WebsiteRuleDTO>>(); }
            set { SetPropertyValue(value); }
        }

        //public ObservableCollection<CrawlLogDTO> CrawlLogs
        //{
        //    get { return GetPropertyValue<ObservableCollection<CrawlLogDTO>>(); }
        //    set { SetPropertyValue(value); }
        //}

        #endregion

        public WebsiteRuleDTO CatalogRule
        {
            get
            {
                return Rules.SingleOrDefault(o => o.Type == WebsiteRuleType.Catalog);
            }
        }

        public WebsiteRuleDTO ArticleRule
        {
            get
            {
                return Rules.SingleOrDefault(o => o.Type == WebsiteRuleType.Article);
            }
        }

        public WebsiteDTO()
        {

        }

        public WebsiteDTO(Website model)
        {
            //CrawlLogs = new ObservableCollection<CrawlLogDTO>(model.CrawlLogs?.Select(o => new CrawlLogDTO(o)) ?? new CrawlLogDTO[0]);
            // NOTES: populate the rules first to avoid dependencies triggered by the updates on the "Home" property in ManageViewModel.Website_PropertyChanged
            Rules = new ObservableCollection<WebsiteRuleDTO>(model.Rules?.OrderBy(o => o.Type).Select(o => new WebsiteRuleDTO(o)) ?? new WebsiteRuleDTO[0]);
 
            Id = model.Id;
            Name = model.Name;
            Home = model.Home;
            Status = model.Status;
            Rank = model.Rank;
            UrlFormat = model.UrlFormat;
            StartIndex = model.StartIndex;
            ValidateDate = model.ValidateDate;
            Notes = model.Notes;
            SysNotes = model.SysNotes;
            Registered = model.Registered;

            // The following should be assigned after Status, otherwise it might be overwritten
            Enabled = model.Enabled;
       }

        /// <summary>
        /// Shadow copy
        /// </summary>
        /// <returns></returns>
        public WebsiteDTO CloneTo(WebsiteDTO target = null)
        {
            if (target == null)
            {
                target = new WebsiteDTO();
            }

            // NOTES: populate the rules first to avoid dependencies triggered by the updates on the "Home" property in ManageViewModel.Website_PropertyChanged
            target.Rules = new ObservableCollection<WebsiteRuleDTO>(Rules?.Select(o => o.CloneTo((WebsiteRuleDTO)null)) ?? new WebsiteRuleDTO[0]);

            target.Id = Id;
            target.Name = Name;
            target.Home = Home;
            target.Status = Status;
            target.Rank = Rank;
            target.UrlFormat = UrlFormat;
            target.StartIndex = StartIndex;
            target.ValidateDate = ValidateDate;
            target.Notes = Notes;
            target.SysNotes = SysNotes;
            target.Registered = Registered;
            //target.CrawlLogs = CrawlLogs;

            // The following should be assigned after Status, otherwise it might be overwritten
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
