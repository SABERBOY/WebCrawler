using WebCrawler.Common;
using WebCrawler.Models;

namespace WebCrawler.DTO
{
    public class CrawlLogDTO : NotifyPropertyChanged
    {
        #region Notify Properties

        public int Id
        {
            get { return GetPropertyValue<int>(); }
            set { SetPropertyValue(value); }
        }

        public int WebsiteId
        {
            get { return GetPropertyValue<int>(); }
            set { SetPropertyValue(value); }
        }

        public int CrawlId
        {
            get { return GetPropertyValue<int>(); }
            set { SetPropertyValue(value); }
        }

        public string LastHandled
        {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue(value); }
        }

        public int Success
        {
            get { return GetPropertyValue<int>(); }
            set { SetPropertyValue(value); }
        }

        public int Fail
        {
            get { return GetPropertyValue<int>(); }
            set { SetPropertyValue(value); }
        }

        public string Notes
        {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue(value); }
        }

        public CrawlStatus Status
        {
            get { return GetPropertyValue<CrawlStatus>(); }
            set { SetPropertyValue(value); }
        }

        public DateTime? Crawled
        {
            get { return GetPropertyValue<DateTime?>(); }
            set { SetPropertyValue(value); }
        }

        public string WebsiteName
        {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue(value); }
        }

        public string WebsiteHome
        {
            get { return GetPropertyValue<string>(); }
            set { SetPropertyValue(value); }
        }

        public WebsiteDTO Website
        {
            get { return GetPropertyValue<WebsiteDTO>(); }
            set { SetPropertyValue(value); }
        }

        #endregion

        public CrawlLogDTO()
        {

        }

        public CrawlLogDTO(CrawlLog model)
            : this(model, false)
        {

        }

        public CrawlLogDTO(CrawlLog model, bool ignoreWebsite)
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

            if (!ignoreWebsite)
            {
                Website = new WebsiteDTO(model.Website);
            }
        }
    }
}
