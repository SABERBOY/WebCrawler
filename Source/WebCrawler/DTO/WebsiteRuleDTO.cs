using WebCrawler.Common;
using WebCrawler.Models;

namespace WebCrawler.DTO
{
    public class WebsiteRuleDTO : NotifyPropertyChanged
    {
        #region Notify Properties

        public Guid RuleId
        {
            get { return GetPropertyValue<Guid>(); }
            set { SetPropertyValue(value); }
        }

        public WebsiteRuleType Type
        {
            get { return GetPropertyValue<WebsiteRuleType>(); }
            set { SetPropertyValue(value); }
        }

        public int WebsiteId
        {
            get { return GetPropertyValue<int>(); }
            set { SetPropertyValue(value); }
        }

        #region Page Load

        public PageLoadOption PageLoadOption
        {
            get { return GetPropertyValue<PageLoadOption>(); }
            set { SetPropertyValue(value); }
        }

        public string? PageUrlReviseExp
        {
            get { return GetPropertyValue<string?>(); }
            set { SetPropertyValue(value); }
        }

        public string? PageUrlReplacement
        {
            get { return GetPropertyValue<string?>(); }
            set { SetPropertyValue(value); }
        }

        #endregion

        #region Content Match

        public ContentMatchType ContentMatchType
        {
            get { return GetPropertyValue<ContentMatchType>(); }
            set { SetPropertyValue(value); }
        }

        public string? ContentRootExp
        {
            get { return GetPropertyValue<string?>(); }
            set { SetPropertyValue(value); }
        }

        public string? ContentUrlExp
        {
            get { return GetPropertyValue<string?>(); }
            set { SetPropertyValue(value); }
        }

        public string? ContentUrlReviseExp
        {
            get { return GetPropertyValue<string?>(); }
            set { SetPropertyValue(value); }
        }

        public string? ContentUrlReplacement
        {
            get { return GetPropertyValue<string?>(); }
            set { SetPropertyValue(value); }
        }

        public string? ContentTitleExp
        {
            get { return GetPropertyValue<string?>(); }
            set { SetPropertyValue(value); }
        }

        public string? ContentDateExp
        {
            get { return GetPropertyValue<string?>(); }
            set { SetPropertyValue(value); }
        }

        public string? ContentExp
        {
            get { return GetPropertyValue<string?>(); }
            set { SetPropertyValue(value); }
        }

        #endregion

        #endregion

        public WebsiteRuleDTO()
        {

        }

        public WebsiteRuleDTO(WebsiteRule model)
        {
            RuleId = model.RuleId;
            Type = model.Type;
            WebsiteId = model.WebsiteId;
            PageLoadOption = model.PageLoadOption;
            PageUrlReviseExp = model.PageUrlReviseExp;
            PageUrlReplacement = model.PageUrlReplacement;
            ContentMatchType = model.ContentMatchType;
            ContentRootExp = model.ContentRootExp;
            ContentUrlExp = model.ContentUrlExp;
            ContentUrlReviseExp = model.ContentUrlReviseExp;
            ContentUrlReplacement = model.ContentUrlReplacement;
            ContentTitleExp = model.ContentTitleExp;
            ContentDateExp = model.ContentDateExp;
            ContentExp = model.ContentExp;
        }

        /// <summary>
        /// Shadow copy
        /// </summary>
        /// <returns></returns>
        public WebsiteRuleDTO CloneTo(WebsiteRuleDTO target = null)
        {
            if (target == null)
            {
                target = new WebsiteRuleDTO();
            }

            target.RuleId = RuleId;
            target.Type = Type;
            target.WebsiteId = WebsiteId;
            target.PageLoadOption = PageLoadOption;
            target.PageUrlReviseExp = PageUrlReviseExp;
            target.PageUrlReplacement = PageUrlReplacement;
            target.ContentMatchType = ContentMatchType;
            target.ContentRootExp = ContentRootExp;
            target.ContentUrlExp = ContentUrlExp;
            target.ContentUrlReviseExp = ContentUrlReviseExp;
            target.ContentUrlReplacement = ContentUrlReplacement;
            target.ContentTitleExp = ContentTitleExp;
            target.ContentDateExp = ContentDateExp;
            target.ContentExp = ContentExp;

            return target;
        }

        public WebsiteRule CloneTo(WebsiteRule target = null)
        {
            if (target == null)
            {
                target = new WebsiteRule();
            }

            target.RuleId = RuleId;
            target.Type = Type;
            target.WebsiteId = WebsiteId;
            target.PageLoadOption = PageLoadOption;
            target.PageUrlReviseExp = PageUrlReviseExp;
            target.PageUrlReplacement = PageUrlReplacement;
            target.ContentMatchType = ContentMatchType;
            target.ContentRootExp = ContentRootExp;
            target.ContentTitleExp = ContentTitleExp;
            target.ContentUrlExp = ContentUrlExp;
            target.ContentUrlReviseExp = ContentUrlReviseExp;
            target.ContentUrlReplacement = ContentUrlReplacement;
            target.ContentDateExp = ContentDateExp;
            target.ContentExp = ContentExp;

            return target;
        }
    }
}
