using HtmlAgilityPack;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WebCrawler.Analyzers;
using WebCrawler.DTO;

namespace WebCrawler.WPF.Dialogs
{
    public partial class RuleEditor : Window
    {
        #region Dependency Properties

        public static readonly DependencyProperty IsNewProperty = DependencyProperty.Register(
            nameof(IsNew),
            typeof(bool),
            typeof(RuleEditor));

        public static readonly DependencyProperty RuleProperty = DependencyProperty.Register(
            nameof(Rule),
            typeof(WebsiteRuleDTO),
            typeof(RuleEditor));

        public static readonly DependencyProperty NodeSuggestionsProperty = DependencyProperty.Register(
            nameof(NodeSuggestions),
            typeof(ObservableCollection<Link>),
            typeof(RuleEditor));

        public bool IsNew
        {
            get { return (bool)GetValue(IsNewProperty); }
            set { SetValue(IsNewProperty, value); }
        }

        public WebsiteRuleDTO Rule
        {
            get { return (WebsiteRuleDTO)GetValue(RuleProperty); }
            set { SetValue(RuleProperty, value); }
        }

        public ObservableCollection<Link> NodeSuggestions
        {
            get { return (ObservableCollection<Link>)GetValue(NodeSuggestionsProperty); }
            set { SetValue(NodeSuggestionsProperty, value); }
        }

        #endregion

        public WebsiteDTO Website { get; set; }

        public HtmlDocument HtmlDoc { get; set; }

        private Link SelectedNode
        {
            get
            {
                return listBoxSuggestions.SelectedItem as Link;
            }
        }

        public RuleEditor(WebsiteDTO website, HtmlDocument htmlDoc, WebsiteRuleDTO rule = null)
        {
            if (rule == null)
            {
                IsNew = true;
                rule = new WebsiteRuleDTO { RuleId = Guid.NewGuid(), WebsiteId = website.Id };
                website.Rules.Add(rule);
            }

            InitializeComponent();

            Title = $"Rule Editor - {website.Name}";
            Website = website;
            Rule = rule;
            HtmlDoc = htmlDoc;

            DataContext = this;
        }

        #region Events

        private void ListPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var keywords = (sender as TextBox).Text;

            SearchHtmlNodes(keywords.Trim());
        }

        private void ListPathTextBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            listBoxSuggestions.Width = (sender as TextBox).ActualWidth;
        }

        private void listBoxSuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Rule.ContentUrlExp = HtmlAnalyzer.DetectListPath(HtmlDoc, SelectedNode.XPath);
        }

        #endregion

        #region Private Methods

        private void SearchHtmlNodes(string keywords)
        {
            Link[] links;
            if (string.IsNullOrEmpty(keywords) || HtmlDoc == null)
            {
                links = new Link[0];
            }
            else
            {
                links = HtmlAnalyzer.GetValidLinks(HtmlDoc)
                    .Where(o => o.Text.Contains(keywords))
                    .ToArray();
            }

            NodeSuggestions = new ObservableCollection<Link>(links);
        }

        #endregion
    }
}
