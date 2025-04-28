using OilTrackAgentInterface.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace OilTrackAgentInterface.Visibility {
    public class LevelToBrushConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (!(value is LogLevel lvl))
                return Brushes.Gray;

            switch (lvl) {
                case LogLevel.DEBUG: return Brushes.DarkGray;
                case LogLevel.INFO: return Brushes.SteelBlue;
                case LogLevel.WARN: return Brushes.Goldenrod;
                case LogLevel.ERROR: return Brushes.IndianRed;
                case LogLevel.FATAL: return Brushes.DarkRed;
                default: return Brushes.LightGray;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
