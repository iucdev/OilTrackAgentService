using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace OilTrackAgentInterface.Visibility {
    public class PercentToBrushConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (!(value is int pct)) return Brushes.Gray;
            if (pct < 25) return new SolidColorBrush(Color.FromRgb(0xE6, 0x4C, 0x3C));
            if (pct < 50) return new SolidColorBrush(Color.FromRgb(0xF4, 0xCD, 0x0B));
            return new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50));
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
