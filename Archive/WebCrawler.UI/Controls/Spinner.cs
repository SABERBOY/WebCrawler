using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace WebCrawler.UI.Controls
{
    /// <summary>
    /// Interaction logic for Spinner.xaml
    /// </summary>
    public class Spinner : Control
    {
        private Storyboard _storyboard;

        #region Dependency Properties

        public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
            nameof(Message),
            typeof(string),
            typeof(Spinner),
            new PropertyMetadata(null));

        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        #endregion

        static Spinner()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Spinner), new FrameworkPropertyMetadata(typeof(Spinner)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _storyboard = FindResource("Storyboard") as Storyboard;
            var spinnerRoot = GetTemplateChild("SpinnerRoot") as FrameworkElement;

            IsEnabledChanged += Spinner_IsEnabledChanged;
            Unloaded += Spinner_Unloaded;

            // to do so as targets defined in control template and referenced in storyboard couldn't be detected in the default scope
            _storyboard.Begin(spinnerRoot);
        }

        private void Spinner_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_storyboard == null)
            {
                return;
            }

            if (IsEnabled)
            {
                _storyboard.Resume();
            }
            else
            {
                _storyboard.Pause();
            }

            Visibility = IsEnabled ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Spinner_Unloaded(object sender, RoutedEventArgs e)
        {
            _storyboard.Stop();
        }
    }
}
