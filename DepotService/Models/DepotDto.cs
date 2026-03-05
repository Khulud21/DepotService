using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DepotService.Models
{
    public class DepotDto : INotifyPropertyChanged
    {
        private bool _isSelected;
        private int _status;
        private string? _info;

        public int Id { get; set; }
        public string Computer { get; set; } = "";
        public string Domain { get; set; } = "";
        public DateTime? LastCheck { get; set; }

        public int Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusIcon));
                    OnPropertyChanged(nameof(StatusDisplay));
                    OnPropertyChanged(nameof(JobResult));
                }
            }
        }

        public string? Info
        {
            get => _info;
            set
            {
                if (_info != value)
                {
                    _info = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Information));
                }
            }
        }

        public string Information => Info ?? "";
        public DateTime? CreatedTime { get; set; }
        public string? DepotSyncId { get; set; }
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

        public string StatusText => Status switch
        {
            0 => "Waiting",
            1 => "Running",
            2 => "Success",
            3 => "Error",
            _ => "Unknown"
        };

        public string StatusIcon => Status switch
        {
            0 => "⏳",
            1 => "🔄",
            2 => "✅",
            3 => "❌",
            _ => "❓"
        };

        public string StatusDisplay => $"{StatusIcon} {StatusText}";

        public string JobResult => Status switch
        {
            0 => "Pending",
            1 => "Running",
            2 => "Success",
            3 => "Error",
            _ => "Unknown"
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
