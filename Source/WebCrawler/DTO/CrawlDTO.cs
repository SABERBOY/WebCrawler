using WebCrawler.Common;

namespace WebCrawler.Models
{
    public class CrawlDTO : NotifyPropertyChanged
    {
        #region Notify Properties

        public int Id
        {
            get { return GetPropertyValue<int>(); }
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

        public string? Notes
        {
            get { return GetPropertyValue<string?>(); }
            set { SetPropertyValue(value); }
        }

        public CrawlStatus Status
        {
            get { return GetPropertyValue<CrawlStatus>(); }
            set { SetPropertyValue(value); }
        }

        public DateTime Started
        {
            get { return GetPropertyValue<DateTime>(); }
            set { SetPropertyValue(value); }
        }

        public DateTime? Completed
        {
            get { return GetPropertyValue<DateTime>(); }
            set { SetPropertyValue(value); }
        }

        #endregion

        public CrawlDTO()
        {

        }

        public CrawlDTO(Crawl model)
        {
            Id = model.Id;
            Started = model.Started;
            Success = model.Success;
            Fail = model.Fail;
            Completed = model.Completed;
            Status = model.Status;
            Notes = model.Notes;
        }

        public Crawl CloneTo(Crawl model = null)
        {
            if (model == null)
            {
                model = new Crawl();
            }

            model.Id = Id;
            model.Started = Started;
            model.Success = Success;
            model.Fail = Fail;
            model.Completed = Completed;
            model.Status = Status;
            model.Notes = Notes;

            return model;
        }
    }
}
