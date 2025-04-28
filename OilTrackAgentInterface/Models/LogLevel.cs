using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OilTrackAgentInterface.Models {
    public enum LogLevel {
        All,
        DEBUG,
        INFO,
        WARN,
        ERROR,
        FATAL
    }

    public enum LoggerType {
        Sender,
        Receiver
    }

    public static class LogLevelHelper {
        // Для ComboBox: возвращает все значения enum-а
        public static IEnumerable<LogLevel> GetAll() {
            return Enum.GetValues(typeof(LogLevel))
                       .Cast<LogLevel>()
                       .ToList();
        }
    }
}
