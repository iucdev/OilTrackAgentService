using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OilTrackAgentInterface.Visibility {
    public class PercentToScaleConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (!(value is int pct) || parameter == null) return 0.0;
            if (!double.TryParse(parameter.ToString(), out var maxHeight)) return 0.0;
            return maxHeight * pct / 100.0;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
