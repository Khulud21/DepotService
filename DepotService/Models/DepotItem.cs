using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DepotService.Models
{
    public class DepotItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private string _computer = "";
        private string _domain = "";
        private string _status = "";
        private string _information = "";

        public string Computer
        {
            get => _computer;
            set
            {
                if (_computer != value)
                {
                    _computer = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Domain
        {
            get => _domain;
            set
            {
                if (_domain != value)
                {
                    _domain = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(JobResult));
                }
            }
        }

        public string Information
        {
            get => _information;
            set
            {
                if (_information != value)
                {
                    _information = value;
                    OnPropertyChanged();
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
                    OnPropertyChanged();
                }
            }
        }

        public DateTime? LastCheck { get; set; }
        public string? LastJobName { get; set; }
        public string? Info { get; set; }

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