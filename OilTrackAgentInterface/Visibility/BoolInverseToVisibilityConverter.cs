using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OilTrackAgentInterface.Visibility {
    public class BoolInverseToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}
