using OilTrackAgentInterface.Models;

namespace OilTrackAgentInterface.ViewModel {
    public class LogFileItem {
        public string Name { get; private set; }
        public string Path { get; private set; }
        public LoggerType Type { get; private set; }

        public LogFileItem(string name, string path, LoggerType loggerType) {
            Name = name;
            Path = path;
            Type = loggerType;
        }

        public override string ToString() {
            return Name;
        }
    }
}
