using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OilTrackAgentInterface {
    public class SelectableField<T> : INotifyPropertyChanged {
        private T _selectedItem;
        private ObservableCollection<T> _items;
        private readonly Action<T> _onSelectedItemChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<T> Items {
            get => _items;
            set {
                if (_items != value) {
                    _items = value;
                    OnPropertyChanged(nameof(Items)); // Уведомляем об изменении
                }
            }
        }

        public SelectableField(Action<T> onSelectedItemChanged, IEnumerable<T> initialItems = null) {
            _onSelectedItemChanged = onSelectedItemChanged;
            Items = initialItems != null ? new ObservableCollection<T>(initialItems) : new ObservableCollection<T>();

            // Уведомляем об изменении коллекции (если заменяется полностью)
            Items.CollectionChanged += (s, e) => OnPropertyChanged(nameof(Items));
        }

        public T SelectedItem {
            get => _selectedItem;
            set {
                if (!EqualityComparer<T>.Default.Equals(_selectedItem, value)) {
                    _selectedItem = value;
                    _onSelectedItemChanged?.Invoke(value);
                    OnPropertyChanged(nameof(SelectedItem)); // Уведомляем об изменении
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
