using OilTrackAgentInterface.Pages.Common;
using OilTrackAgentInterface.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OilTrackAgentInterface.Pages {
    public partial class TankTransfersPage : Page {
        private readonly BasePage<TankTransfersViewModel> _basePage;

        public TankTransfersPage() {
            InitializeComponent();
            _basePage = new BasePage<TankTransfersViewModel>(new TankTransfersViewModel());
            DataContext = _basePage.ViewModel;
        }

        private void OnColumnHeaderClick(object sender, RoutedEventArgs e) {
            _basePage.OnColumnHeaderClick(sender, e);
        }

        private void OnCommandExecute(object sender, MouseButtonEventArgs e) {
            _basePage.OnCommandExecute(sender, e);
        }

        private void BaseDataGrid_Sorting(object sender, DataGridSortingEventArgs e) {
            _basePage.OnSorting(sender, e);
        }
    }
}
