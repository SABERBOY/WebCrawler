namespace WebCrawler.Common
{
    public class PageInfo : NotifyPropertyChanged
    {
        #region Notify Properties

        public int CurrentPage
        {
            get { return GetPropertyValue<int>(); }
            set { SetPropertyValue(value); }
        }

        private int _pageSize;
        public int PageSize
        {
            get { return GetPropertyValue<int>(); }
            set
            {
                if (!SetPropertyValue(value)) { return; }

                CalculatePageCount();
            }
        }

        private int _itemCount;
        public int ItemCount
        {
            get { return GetPropertyValue<int>(); }
            set
            {
                if (!SetPropertyValue(value)) { return; }

                CalculatePageCount();
            }
        }

        private int _pageCount;
        public int PageCount
        {
            get { return GetPropertyValue<int>(); }
            set { SetPropertyValue(value); }
        }

        private void CalculatePageCount()
        {
            PageCount = PageSize == 0 ? 0 : (ItemCount - 1) / PageSize + 1;
        }

        #endregion
    }
}
