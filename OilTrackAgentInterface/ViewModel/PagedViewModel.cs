using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace OilTrackAgentInterface.ViewModel {
    public class PagedViewModel<T> : INotifyPropertyChanged {
        private ObservableCollection<T> _allItems;
        private int _pageSize;

        public PagedCollection<T> PagedItems { get; private set; }

        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }

        public string PageInfo => $"{PagedItems.CurrentPage} из {PagedItems.TotalPages}";

        public PagedViewModel(IEnumerable<T> items, int pageSize) {
            _allItems = new ObservableCollection<T>(items);
            _pageSize = pageSize;
            PagedItems = new PagedCollection<T>(_allItems, _pageSize);

            NextPageCommand = new RelayCommand(_ => GoToNextPage(), _ => PagedItems.CurrentPage < PagedItems.TotalPages);
            PreviousPageCommand = new RelayCommand(_ => GoToPreviousPage(), _ => PagedItems.CurrentPage > 1);
        }

        private void GoToNextPage() {
            PagedItems.NextPage();
            OnPropertyChanged(nameof(PageInfo));
            OnPropertyChanged(nameof(PagedItems));
            CommandManager.InvalidateRequerySuggested();
        }

        private void GoToPreviousPage() {
            PagedItems.PreviousPage();
            OnPropertyChanged(nameof(PageInfo));
            OnPropertyChanged(nameof(PagedItems));
            CommandManager.InvalidateRequerySuggested();
        }

        public void Refresh() {
            PagedItems.Refresh();
            OnPropertyChanged(nameof(PagedItems));
            OnPropertyChanged(nameof(PageInfo));
        }

        public void UpdateSource(IEnumerable<T> newItems) {
            _allItems.Clear();
            foreach (var item in newItems) {
                _allItems.Add(item);
            }

            PagedItems = new PagedCollection<T>(_allItems, _pageSize); // Пересоздаем PagedCollection

            OnPropertyChanged(nameof(PagedItems));
            OnPropertyChanged(nameof(PageInfo));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
