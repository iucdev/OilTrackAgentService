using OilTrackAgentInterface.ViewModel;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace OilTrackAgentInterface {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
            DataContext = new NavigationViewModel(MainFrame);
            this.Icon = new BitmapImage(new Uri("pack://application:,,,/Assets/tab_icon.ico"));
        }

        private void NavigateToTankIndicatorsPage(object sender, RoutedEventArgs e) {
            MainFrame.Navigate(new Pages.TankIndicatorsPage());
        }

        private void NavigateToTankTransfersPage(object sender, RoutedEventArgs e) {
            MainFrame.Navigate(new Pages.TankTransfersPage());
        }

        private void NavigateToPage(object sender, RoutedEventArgs e) {
            var button = sender as Button;

            // Сбрасываем состояние всех кнопок
            foreach (var child in ((StackPanel)button.Parent).Children) {
                if (child is Button btn) {
                    btn.Tag = null; // Убираем активное состояние
                }
            }

            // Устанавливаем текущую кнопку как активную
            button.Tag = "Active";

            // Обновляем содержимое
            switch (button.Content.ToString()) {
                case "Поставки":
                    MainFrame.Navigate(new Pages.TankTransfersPage());
                    break;
                case "Резервуары":
                    MainFrame.Navigate(new Pages.TankIndicatorsPage());
                    break;
                case "Отправленные данные":
                    MainFrame.Navigate(new Pages.SendPackage());
                    break;
                case "Объекты производства":
                    MainFrame.Navigate(new Pages.AnalyticsPage());
                    break;
            }
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e) {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeWindow(object sender, RoutedEventArgs e) {
            if (this.WindowState == WindowState.Maximized) {
                this.WindowState = WindowState.Normal;
            } else {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void CloseWindow(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed) {
                // Запускаем перетаскивание окна
                this.DragMove();
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true // Открывает ссылку в браузере по умолчанию
            });
            e.Handled = true;
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
