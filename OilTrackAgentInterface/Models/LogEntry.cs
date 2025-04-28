using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OilTrackAgentInterface.Models {
    public class LogEntry {
        public int Index { get; set; }
        public LogLevel Level { get; set; }
        public DateTime Time { get; set; }
        public string Message { get; set; }
    }
}
