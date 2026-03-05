using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DepotService.Models
{
    public class SelectableItem : INotifyPropertyChanged
    {
        private string _name ="";
        private bool _isSelected;

        public SelectableItem(string name, bool isSelected = true)
        {
            _name = name;
            _isSelected = isSelected;
        }

        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; OnPropertyChanged(); } }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}