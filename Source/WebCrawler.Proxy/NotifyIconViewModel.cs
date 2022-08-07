using GalaSoft.MvvmLight.Command;
using System.Windows;
using System.Windows.Input;
using WebCrawler.Common;

namespace WebCrawler.Proxy
{
    public class NotifyIconViewModel: NotifyPropertyChanged
    {
        private RelayCommand _showWindowCommand;
        /// <summary>
        /// Shows a window, if none is already open.
        /// </summary>
        public ICommand ShowWindowCommand
        {
            get
            {
                if (_showWindowCommand == null)
                {
                    _showWindowCommand = new RelayCommand(
                        () => Application.Current.MainWindow.Show(),
                        () => Application.Current.MainWindow == null || !Application.Current.MainWindow.IsVisible
                    );
                }
                return _showWindowCommand;
            }
        }

        private RelayCommand _hideWindowCommand;
        /// <summary>
        /// Hides the main window. This command is only enabled if a window is open.
        /// </summary>
        public ICommand HideWindowCommand
        {
            get
            {
                if (_hideWindowCommand == null)
                {
                    _hideWindowCommand = new RelayCommand(
                        () => Application.Current.MainWindow.Hide(),
                        () => Application.Current.MainWindow != null && Application.Current.MainWindow.IsVisible
                    );
                }
                return _hideWindowCommand;
            }
        }

        private RelayCommand _exitApplicationCommand;
        /// <summary>
        /// Shuts down the application.
        /// </summary>
        public ICommand ExitApplicationCommand
        {
            get
            {
                if (_exitApplicationCommand == null)
                {
                    _exitApplicationCommand = new RelayCommand(() => Application.Current.Shutdown());
                }
                return _exitApplicationCommand;
            }
        }
    }
}
