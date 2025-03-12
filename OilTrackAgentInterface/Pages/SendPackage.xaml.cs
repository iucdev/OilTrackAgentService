using OilTrackAgentInterface.Pages.Common;
using OilTrackAgentInterface.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OilTrackAgentInterface.Pages {
    /// <summary>
    /// Interaction logic for SendPackage.xaml
    /// </summary>
    public partial class SendPackage : Page {

        private readonly BasePage<SendPackageViewModel> _basePage;

        public SendPackage() {
            InitializeComponent();
            _basePage = new BasePage<SendPackageViewModel>(new SendPackageViewModel());
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
