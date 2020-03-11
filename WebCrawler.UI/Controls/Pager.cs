using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WebCrawler.UI.ViewModels;

namespace WebCrawler.UI.Controls
{
    /// <summary>
    /// Interaction logic for Spinner.xaml
    /// </summary>
    public class Pager : Control
    {
        public event RoutedEventHandler Navigated;

        #region Dependency Properties

        public static readonly DependencyProperty PageInfoProperty = DependencyProperty.Register(
            nameof(PageInfo),
            typeof(PageInfo),
            typeof(Pager),
            new PropertyMetadata(OnPropertyChanged));

        public static DependencyProperty NavigatedCommandProperty = DependencyProperty.Register(
            nameof(NavigatedCommand),
            typeof(ICommand),
            typeof(Pager));

        public PageInfo PageInfo
        {
            get { return (PageInfo)GetValue(PageInfoProperty); }
            set { SetValue(PageInfoProperty, value); }
        }

        public ICommand NavigatedCommand
        {
            get { return (ICommand)GetValue(NavigatedCommandProperty); }
            set { SetValue(NavigatedCommandProperty, value); }
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Pager pager)
            {
                switch (e.Property.Name)
                {
                    case nameof(PageInfo):
                        pager.Visibility = pager.PageInfo == null ? Visibility.Collapsed : Visibility.Visible;
                        break;
                    default:
                        break;
                }
            }
        }

        #endregion

        Button first;
        Button previous;
        Button next;
        Button last;

        static Pager()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Pager), new FrameworkPropertyMetadata(typeof(Pager)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            first = GetTemplateChild("First") as Button;
            previous = GetTemplateChild("Previous") as Button;
            next = GetTemplateChild("Next") as Button;
            last = GetTemplateChild("Last") as Button;

            first.Click -= First_Click;
            previous.Click -= Previous_Click;
            next.Click -= Next_Click;
            last.Click -= Last_Click;

            first.Click += First_Click;
            previous.Click += Previous_Click;
            next.Click += Next_Click;
            last.Click += Last_Click;

            Visibility = PageInfo == null ? Visibility.Collapsed : Visibility.Visible;
        }

        private void First_Click(object sender, RoutedEventArgs e)
        {
            if (PageInfo == null)
            {
                return;
            }

            HandleNavigate(1);
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (PageInfo == null)
            {
                return;
            }

            HandleNavigate(PageInfo.CurrentPage - 1);
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (PageInfo == null)
            {
                return;
            }

            HandleNavigate(PageInfo.CurrentPage + 1);
        }

        private void Last_Click(object sender, RoutedEventArgs e)
        {
            if (PageInfo == null)
            {
                return;
            }

            HandleNavigate(PageInfo.PageCount);
        }

        private void HandleNavigate(int page)
        {
            PageInfo.CurrentPage = page;

            first.IsEnabled = page > 1;
            previous.IsEnabled = page > 1;
            next.IsEnabled = page < PageInfo.PageCount;
            last.IsEnabled = page < PageInfo.PageCount;

            Navigated?.Invoke(this, new RoutedEventArgs());
            NavigatedCommand?.Execute(PageInfo.CurrentPage);
        }
    }
}
