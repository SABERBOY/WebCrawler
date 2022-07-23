using HtmlAgilityPack;
using WebCrawler.Common;
using WebCrawler.DTO;

namespace WebCrawler.WPF.ViewModels
{
    public class WebsiteEditor : NotifyPropertyChanged
    {
        #region Notify Properties

        public bool IsEditing
        {
            get { return GetPropertyValue<bool>(); }
            set { SetPropertyValue(value); }
        }

        public WebsiteDTO Website
        {
            get { return GetPropertyValue<WebsiteDTO>(); }
            set
            {
                if (!SetPropertyValue(value)) { return; }

                Response = null;
            }
        }

        public ResponseData Response
        {
            get { return GetPropertyValue<ResponseData>(); }
            set
            {
                if (!SetPropertyValue(value)) { return; }

                _htmlDoc = null;
            }
        }

        #endregion

        private HtmlDocument _htmlDoc;
        public HtmlDocument HtmlDoc
        {
            get
            {
                if (_htmlDoc == null && !string.IsNullOrEmpty(Response?.Content))
                {
                    _htmlDoc = new HtmlDocument();
                    _htmlDoc.LoadHtml(Response.Content);
                }

                return _htmlDoc;
            }
        }
    }
}
