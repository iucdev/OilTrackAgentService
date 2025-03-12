using System.Windows;
using System;
using System.Windows.Controls;
using System.Windows.Input;
using OilTrackAgentInterface.Pages.Common;
using OilTrackAgentInterface.ViewModel;

namespace OilTrackAgentInterface.Pages {
    /// <summary>
    /// Interaction logic for TankIndicators.xaml
    /// </summary>
    public partial class TankIndicatorsPage : Page {

        private readonly BasePage<TankIndicatorViewModel> _basePage;

        public TankIndicatorsPage() {
            InitializeComponent();
            _basePage = new BasePage<TankIndicatorViewModel>(new TankIndicatorViewModel());
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
