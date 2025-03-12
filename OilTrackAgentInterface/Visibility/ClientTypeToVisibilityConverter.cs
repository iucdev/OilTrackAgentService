using AgentService.References;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OilTrackAgentInterface.Visibility {
    public class ClientTypeToVisibilityConverter : IValueConverter {
        public ClientType TargetType { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            // Проверяем, совпадает ли текущее значение с целевым типом
            if (value != null) {
                return (ClientType)value == TargetType ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            }
            return System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
