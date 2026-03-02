using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DepotService.Models
{
    public class DepotItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string Computer { get; set; } = "";
        public string Domain { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime? LastCheck { get; set; }
        public string? LastJobName { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public string JobName => LastJobName ?? "Kein Job";

        public string JobResult => Status switch
        {
            "Online" => "Success",
            "Offline" => "Error",
            _ => "Pending"
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}