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
    /// Interaction logic for AnalyticsPage.xaml
    /// </summary>
    public partial class AnalyticsPage : Page {
        public AnalyticsPage() {
            InitializeComponent();
            DataContext = new AnalyticsViewModel();
        }

        private void OnCommandExecute(object sender, MouseButtonEventArgs e) {
            if (sender is FrameworkElement element && element.Tag is string commandName) {
                var dataContext = element.DataContext;

                try {
                    var commandProperty = dataContext?.GetType().GetProperty(commandName);
                    var command = commandProperty?.GetValue(dataContext) as ICommand;

                    if (command?.CanExecute(null) == true) {
                        command.Execute(null);
                    }
                } catch (Exception ex) {
                    // Логируйте ошибки или выводите уведомления
                    Console.WriteLine($"Ошибка выполнения команды {commandName}: {ex.Message}");
                }
            }
        }
    }
}
