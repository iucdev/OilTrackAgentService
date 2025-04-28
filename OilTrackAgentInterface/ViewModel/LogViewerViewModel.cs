using OilTrackAgentInterface.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace OilTrackAgentInterface.ViewModel {
    public class LogViewerViewModel : INotifyPropertyChanged, IDisposable {
        private const int MaxLines = 100;
        private CancellationTokenSource _cts;
        private long _lastPosition;
        private int _counter;

        public ObservableCollection<LogFileItem> LogFiles { get; }
            = new ObservableCollection<LogFileItem>();

        public ObservableCollection<LogEntry> Entries { get; }
            = new ObservableCollection<LogEntry>();

        public ICollectionView FilteredEntries { get; }

        // Для привязки в ComboBox
        public IEnumerable<LogLevel> LevelsList => LogLevelHelper.GetAll();

        #region Выбор Receive/Send

        private bool _isReceiveSelected;
        public bool IsReceiveSelected {
            get => _isReceiveSelected;
            set {
                if (_isReceiveSelected == value) return;
                _isReceiveSelected = value;
                OnPropertyChanged(nameof(IsReceiveSelected));

                // переключаем файл
                SelectedLogFile = LogFiles
                    .First(f => f.Type == (value ? LoggerType.Sender : LoggerType.Receiver));
            }
        }

        #endregion

        #region SelectedLogFile

        private LogFileItem _selectedLogFile;
        public LogFileItem SelectedLogFile {
            get => _selectedLogFile;
            set {
                if (_selectedLogFile == value) return;
                _selectedLogFile = value;
                OnPropertyChanged(nameof(SelectedLogFile));
                RestartTailing();
            }
        }

        #endregion

        #region Фильтрация

        private LogLevel _selectedLevel = LogLevel.All;
        public LogLevel SelectedLevel {
            get => _selectedLevel;
            set {
                if (_selectedLevel == value) return;
                _selectedLevel = value;
                OnPropertyChanged(nameof(SelectedLevel));
                FilteredEntries.Refresh();
            }
        }

        private string _searchText = "";
        public string SearchText {
            get => _searchText;
            set {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                FilteredEntries.Refresh();
            }
        }

        public ICommand ClearSearchCommand { get; }

        #endregion

        public LogViewerViewModel() {
            // 1) Собираем два файла: sender и receiver
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var today = DateTime.Now.ToString("yyyy-MM-dd");

            LogFiles.Add(new LogFileItem("Отправка",
                Path.Combine(baseDir, "logs", $"Sender-{today}.log"), LoggerType.Sender));

            LogFiles.Add(new LogFileItem("Приём",
                Path.Combine(baseDir, "logs", $"Receiver-{today}.log"), LoggerType.Receiver));

            // 2) Инициализируем коллекцию с фильтрацией
            FilteredEntries = CollectionViewSource.GetDefaultView(Entries);
            FilteredEntries.Filter = obj => {
                var e = obj as LogEntry;
                if (e == null) return false;

                bool okLevel = SelectedLevel == LogLevel.All || e.Level == SelectedLevel;
                bool okText = string.IsNullOrEmpty(SearchText)
                               || e.Message.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
                return okLevel && okText;
            };

            ClearSearchCommand = new RelayCommand(_ => SearchText = "");

            // 3) По-умолчанию показываем «Приём»
            IsReceiveSelected = true;
        }

        private void RestartTailing() {
            // Останавливаем старое
            if (_cts != null && !_cts.IsCancellationRequested)
                _cts.Cancel();

            Entries.Clear();
            _counter = 0;
            _lastPosition = 0;

            // Новый токен
            _cts = new CancellationTokenSource();

            // И стартуем
            StartTailing(_cts.Token);
        }

        private void StartTailing(CancellationToken token) {
            var file = SelectedLogFile?.Path;
            if (string.IsNullOrEmpty(file) || !File.Exists(file)) return;

            Task.Run(() => {
                try {
                    // 1) initial-контент
                    var all = File.ReadAllLines(file, Encoding.UTF8);
                    var tail = all.Length <= MaxLines ? all
                        : all.Skip(all.Length - MaxLines).ToArray();

                    Application.Current.Dispatcher.Invoke(() => {
                        foreach (var line in tail) AppendLine(line);
                        _lastPosition = new FileInfo(file).Length;
                        FilteredEntries.Refresh();
                    });

                    // 2) live «tail -f»
                    using (var fs = new FileStream(
                        file, FileMode.Open, FileAccess.Read,
                        FileShare.ReadWrite | FileShare.Delete))
                    using (var reader = new StreamReader(fs, Encoding.UTF8)) {
                        fs.Seek(_lastPosition, SeekOrigin.Begin);

                        while (!token.IsCancellationRequested) {
                            var line = reader.ReadLine();
                            if (line != null) {
                                Application.Current.Dispatcher.Invoke(() => {
                                    AppendLine(line);
                                    FilteredEntries.Refresh();
                                });
                                _lastPosition = fs.Position;
                            } else {
                                Thread.Sleep(200);

                                // если файл «сброшен» (ротация) — начинаем с начала
                                if (fs.Length < _lastPosition) {
                                    _lastPosition = 0;
                                    fs.Seek(0, SeekOrigin.Begin);
                                    reader.DiscardBufferedData();
                                }
                            }
                        }
                    }
                } catch (OperationCanceledException) {
                    // ожидаемое завершение
                } catch (Exception ex) {
                    // тут можно логировать
                }
            });
        }

        private void AppendLine(string raw) {
            // формат: yyyy-MM-dd HH:mm:ss|LEVEL|message
            var parts = raw.Split(new[] { '|' }, 3);
            if (parts.Length < 3) return;

            if (!Enum.TryParse(parts[1], true, out LogLevel lvl))
                lvl = LogLevel.All;

            if (!DateTime.TryParse(parts[0], out DateTime dt))
                dt = DateTime.Now;

            var entry = new LogEntry {
                Index = ++_counter,
                Time = dt,
                Level = lvl,
                Message = parts[2]
            };
            Entries.Add(entry);

            // держим только MaxLines
            while (Entries.Count > MaxLines)
                Entries.RemoveAt(0);
        }

        public void Dispose() {
            if (_cts != null) {
                _cts.Cancel();
                _cts.Dispose();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string prop)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
