using AgentService.References;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OilTrackAgentInterface.Visibility {
    public class LevelUnitTypeToDisplayTextConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is Tuple<double, LevelUnitType> unitData) {
                return $"{unitData.Item1} {unitData.Item2.ToDisplayText()}"; // Вызов метода расширения
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
