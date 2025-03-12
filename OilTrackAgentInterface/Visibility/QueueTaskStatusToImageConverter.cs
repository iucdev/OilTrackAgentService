using AgentService.References;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace OilTrackAgentInterface.Visibility {
    public class QueueTaskStatusToImageConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            string imagePath = null;
            if (value is QueueTaskStatus status) {
                switch (status) {
                    case QueueTaskStatus.InProcess:
                        imagePath = "/Assets/waiting-outline.png";
                        break;
                    case QueueTaskStatus.Abandon:
                        imagePath = "/Assets/cross-outline.png";
                        break;
                    case QueueTaskStatus.Processed:
                        imagePath = "/Assets/check-outline.png";
                        break;
                    default:
                        throw new NotImplementedException(status.ToString());
                }
            }
            return imagePath;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
