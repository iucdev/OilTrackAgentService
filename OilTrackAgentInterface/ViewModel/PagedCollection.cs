using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;

namespace OilTrackAgentInterface.ViewModel {
    public class PagedCollection<T> : ObservableCollection<T> {
        private ObservableCollection<T> _allItems;
        private int _currentPage;
        private int _itemsPerPage;

        public PagedCollection(ObservableCollection<T> items, int itemsPerPage) {
            _allItems = items ?? throw new ArgumentNullException(nameof(items));
            _itemsPerPage = itemsPerPage > 0 ? itemsPerPage : throw new ArgumentException("Items per page must be greater than zero.");

            _currentPage = 1;
            UpdateItems();
        }

        public int CurrentPage {
            get => _currentPage;
            private set {
                if (value >= 1 && value <= TotalPages) {
                    _currentPage = value;
                    UpdateItems();
                }
            }
        }

        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)_allItems.Count / _itemsPerPage));

        private void UpdateItems() {
            Clear();
            var itemsToDisplay = _allItems
                .Skip((_currentPage - 1) * _itemsPerPage)
                .Take(_itemsPerPage);

            foreach (var item in itemsToDisplay) {
                Add(item);
            }
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(CurrentPage)));
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(TotalPages)));
        }

        public void NextPage() {
            if (CurrentPage < TotalPages) {
                CurrentPage++;
            }
        }

        public void PreviousPage() {
            if (CurrentPage > 1) {
                CurrentPage--;
            }
        }

        /// <summary>
        /// Обновляет данные списка, сохраняя текущую страницу.
        /// </summary>
        public void UpdateData(IEnumerable<T> newItems) {
            if (newItems == null)
                throw new ArgumentNullException(nameof(newItems));

            int currentPage = _currentPage; // Запоминаем текущую страницу

            _allItems.Clear();
            foreach (var item in newItems) {
                _allItems.Add(item);
            }

            _currentPage = Math.Min(currentPage, TotalPages); // Гарантируем, что страница не выходит за пределы
            UpdateItems();
        }

        /// <summary>
        /// Устанавливает текущую страницу и обновляет список.
        /// </summary>
        public void SetPage(int pageNumber) {
            if (pageNumber < 1) pageNumber = 1;
            if (pageNumber > TotalPages) pageNumber = TotalPages;

            CurrentPage = pageNumber;
        }

        public void Refresh() {
            UpdateItems(); // Обновляем элементы на текущей странице
        }
    }
}
