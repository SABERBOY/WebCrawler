using HtmlAgilityPack;
using System.Collections.ObjectModel;
using WebCrawler.Common.Analyzers;

namespace WebCrawler.UI.ViewModels
{
    public class WebsiteEditor : NotifyPropertyChanged
    {
        #region Notify Properties

        private WebsiteView _website;
        public WebsiteView Website
        {
            get
            {
                return _website;
            }
            set
            {
                if (value == _website)
                {
                    return;
                }

                _website = value;
                RaisePropertyChanged();

                Html = null;
                NodeSuggestions = null;
            }
        }

        private string _html;
        public string Html
        {
            get
            {
                return _html;
            }
            set
            {
                if (value == _html)
                {
                    return;
                }

                _html = value;
                RaisePropertyChanged();

                _htmlDoc = null;
            }
        }

        private ObservableCollection<Link> _htmlNodes;
        public ObservableCollection<Link> NodeSuggestions
        {
            get { return _htmlNodes; }
            set
            {
                if (_htmlNodes == value) { return; }

                _htmlNodes = value;
                RaisePropertyChanged();
            }
        }

        private Link _selectedNode;
        public Link SelectedNode
        {
            get { return _selectedNode; }
            set
            {
                if (_selectedNode == value) { return; }

                _selectedNode = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        private HtmlDocument _htmlDoc;
        public HtmlDocument HtmlDoc
        {
            get
            {
                if (_htmlDoc == null && !string.IsNullOrEmpty(Html))
                {
                    _htmlDoc = new HtmlDocument();
                    _htmlDoc.LoadHtml(Html);
                }

                return _htmlDoc;
            }
        }
    }
}
