using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace OilTrackAgentInterface.Pages.Common {
    public interface ISortableViewModel {
        void SortData(string columnName);
    }

    public class BasePage<TViewModel> : Page where TViewModel : class, new() {
        public TViewModel ViewModel { get; }

        public BasePage(TViewModel viewModel) {
            ViewModel = viewModel;
            DataContext = viewModel;
        }

        // 🔄 Метод обработки клика по заголовку колонки
        public void OnColumnHeaderClick(object sender, RoutedEventArgs e) {
            if (sender is DataGridColumnHeader columnHeader && columnHeader.Column is DataGridTemplateColumn column) {
                string columnName = column.SortMemberPath;
                if (string.IsNullOrEmpty(columnName)) return;

                if (ViewModel is ISortableViewModel sortableViewModel) {
                    sortableViewModel.SortData(columnName);
                }
            }
        }

        // 🖱 Метод обработки команды
        public void OnCommandExecute(object sender, MouseButtonEventArgs e) {
            if (sender is FrameworkElement element && element.Tag is string commandName) {
                var commandProperty = ViewModel?.GetType().GetProperty(commandName);
                var command = commandProperty?.GetValue(ViewModel) as ICommand;

                if (command?.CanExecute(null) == true) {
                    command.Execute(null);
                }
            }
        }

        // 🔄 Метод обработки сортировки DataGrid
        public void OnSorting(object sender, DataGridSortingEventArgs e) {
            e.Handled = true;

            string columnName = e.Column.SortMemberPath;
            if (string.IsNullOrEmpty(columnName)) return;

            if (ViewModel is ISortableViewModel sortableViewModel) {
                sortableViewModel.SortData(columnName);
            }
        }
    }
}
