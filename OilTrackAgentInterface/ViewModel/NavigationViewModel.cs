using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace OilTrackAgentInterface.ViewModel {
    public class NavigationViewModel : INotifyPropertyChanged {
        private Frame _mainFrame;
        public event PropertyChangedEventHandler PropertyChanged;
        public DateTime CurrentDateTime => DateTime.Now;
        public NavigationViewModel(Frame mainFrame) {
            _mainFrame = mainFrame ?? throw new ArgumentNullException(nameof(mainFrame));
            NavigateCommand = new RelayCommand<string>(Navigate);
            NavigateToInitialPage();
        }

        private void NavigateToInitialPage() {
            // Замените TankView на вашу начальную страницу
            _mainFrame.Navigate(new Pages.TankTransfersPage());
        }

        public ICommand NavigateCommand { get; }

        private void Navigate(string viewName) {
            switch (viewName) {
                case "TankTransfers":
                    _mainFrame.Navigate(new Pages.TankTransfersPage());
                    break;
                case "TankIndicators":
                    _mainFrame.Navigate(new Pages.TankIndicatorsPage());
                    break;
                case "SendPackage":
                    _mainFrame.Navigate(new Pages.SendPackage());
                    break;
                case "Logs":
                    _mainFrame.Navigate(new Pages.LogViewerPage());
                    break;
                default:
                    throw new ArgumentException($"Unknown view name: {viewName}");
            }
        }

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
