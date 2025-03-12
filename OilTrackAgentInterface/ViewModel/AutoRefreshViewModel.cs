using OilTrackAgentInterface.Pages.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

public abstract class AutoRefreshViewModel<T> : ISortableViewModel, INotifyPropertyChanged where T : class {
    private readonly System.Timers.Timer _updateTimer;
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    public ObservableCollection<T> Items { get; private set; }

    private string _currentSortColumn;
    public string CurrentSortColumn {
        get => _currentSortColumn;
        set {
            if (_currentSortColumn != value) {
                _currentSortColumn = value;
                OnPropertyChanged(nameof(CurrentSortColumn));
            }
        }
    }

    private ListSortDirection _currentSortDirection = ListSortDirection.Ascending;
    public ListSortDirection CurrentSortDirection {
        get => _currentSortDirection;
        set {
            if (_currentSortDirection != value) {
                _currentSortDirection = value;
                OnPropertyChanged(nameof(CurrentSortDirection));
            }
        }
    }

    protected AutoRefreshViewModel(int refreshIntervalMs = 60000) {
        Items = new ObservableCollection<T>();

        // ⏳ Настройка таймера
        _updateTimer = new System.Timers.Timer(refreshIntervalMs);
        _updateTimer.Elapsed += async (s, e) => await UpdateDataAsync();
        _updateTimer.AutoReset = true;
        _updateTimer.Start();
    }

    /// <summary>
    /// 🔄 Метод для загрузки данных (переопределяется в дочерних классах)
    /// </summary>
    protected abstract Task<List<T>> LoadDataFromSourceAsync();

    /// <summary>
    /// 🔄 Автоматическое обновление данных
    /// </summary>
    protected async Task UpdateDataAsync() {
        Debug.WriteLine("[AutoRefresh] Обновление данных...");

        _cancellationTokenSource.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        var token = _cancellationTokenSource.Token;

        try {
            var newData = await LoadDataFromSourceAsync();
            if (token.IsCancellationRequested) return;

            Application.Current.Dispatcher.Invoke(() => {
                UpdateCollection(Items, newData);
                OnDataUpdated();
            });

            Debug.WriteLine("[AutoRefresh] Данные обновлены!");
        } catch (TaskCanceledException) {
            Debug.WriteLine("[AutoRefresh] Запрос отменен.");
        } catch (Exception ex) {
            Debug.WriteLine($"[AutoRefresh] Ошибка при загрузке: {ex.Message}");
        }
    }

    /// <summary>
    /// 📌 Обновление коллекции
    /// </summary>
    private void UpdateCollection(ObservableCollection<T> existing, List<T> updated) {
        var existingDict = existing.ToDictionary(x => x.GetHashCode());
        var updatedDict = updated.ToDictionary(x => x.GetHashCode());

        existing.Clear();
        foreach(var item in updated) {
            existing.Add(item);
        }
    }

    /// <summary>
    /// 📌 Вызывается после обновления данных (можно переопределять в дочерних классах)
    /// </summary>
    protected abstract void OnDataUpdated();

    /// <summary>
    /// 📌 Сортировка данных по столбцу
    /// </summary>
    public void SortData(string columnName) {
        if (CurrentSortColumn == columnName) {
            CurrentSortDirection = 
                (CurrentSortDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending);
        } else {
            CurrentSortColumn = columnName;
            CurrentSortDirection = ListSortDirection.Ascending;
        }

        var sortedData = CurrentSortDirection == ListSortDirection.Ascending
            ? Items.OrderBy(x => GetPropertyValue(x, columnName)).ToList()
            : Items.OrderByDescending(x => GetPropertyValue(x, columnName)).ToList();

        Application.Current.Dispatcher.Invoke(() => {
            UpdateCollection(Items, sortedData);
            OnDataUpdated();
        });
    }

    /// <summary>
    /// 📌 Получение свойства объекта по имени (для сортировки)
    /// </summary>
    private object GetPropertyValue(object obj, string propertyName) {
        return obj.GetType().GetProperty(propertyName)?.GetValue(obj, null);
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
