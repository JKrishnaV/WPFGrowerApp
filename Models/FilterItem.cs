using System.ComponentModel;

namespace WPFGrowerApp.Models
{
    /// <summary>
    /// Generic wrapper for filter items that supports checkbox-based selection
    /// </summary>
    /// <typeparam name="T">The type of the wrapped item</typeparam>
    public class FilterItem<T> : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _displayText = string.Empty;

        public T Item { get; set; } = default(T)!;
        
        public string DisplayText 
        { 
            get => _displayText;
            set 
            {
                if (_displayText != value)
                {
                    _displayText = value;
                    OnPropertyChanged(nameof(DisplayText));
                }
            }
        }
        
        public bool IsSelected 
        { 
            get => _isSelected;
            set 
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public FilterItem() { }

        public FilterItem(T item, string displayText, bool isSelected = false)
        {
            Item = item;
            DisplayText = displayText;
            IsSelected = isSelected;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override string ToString()
        {
            return DisplayText;
        }
    }
}

