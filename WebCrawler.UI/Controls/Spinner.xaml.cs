using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace WebCrawler.UI.Controls
{
    /// <summary>
    /// Interaction logic for Spinner.xaml
    /// </summary>
    public partial class Spinner : UserControl
    {
        private Storyboard _storyboard;

        public Spinner()
        {
            InitializeComponent();

            Loaded += Spinner_Loaded;
            Unloaded += Spinner_Unloaded;

            IsEnabledChanged += Spinner_IsEnabledChanged;
        }

        private void Spinner_Loaded(object sender, RoutedEventArgs e)
        {
            _storyboard = FindResource("Storyboard") as Storyboard;

            _storyboard.Begin();
        }

        private void Spinner_Unloaded(object sender, RoutedEventArgs e)
        {
            _storyboard.Stop();
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
    }
}
