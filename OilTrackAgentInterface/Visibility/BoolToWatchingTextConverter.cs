using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OilTrackAgentInterface.Visibility {
    public class BoolToWatchingTextConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
          => (value is bool b && b) ? "Наблюдение — ВКЛ" : "Наблюдение — ВЫКЛ";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
