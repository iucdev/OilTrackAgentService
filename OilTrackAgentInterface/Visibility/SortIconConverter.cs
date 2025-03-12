using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace OilTrackAgentInterface.Visibility {
    public class SortIconConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values.Length < 3)
                return string.Empty;

            string currentSortColumn = values[0] as string;
            string columnName = values[1] as string;
            var sortDirection = values[2] as ListSortDirection?;

            // Логирование значений
            Console.WriteLine($"[SortIconConverter] CurrentSortColumn: {currentSortColumn}, ColumnName: {columnName}, SortDirection: {sortDirection}");

            if (string.IsNullOrEmpty(currentSortColumn) || string.IsNullOrEmpty(columnName))
                return string.Empty;

            if (currentSortColumn != columnName)
                return string.Empty;

            return sortDirection == ListSortDirection.Ascending ? "▲" : "▼";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
