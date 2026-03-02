using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DepotService.Data;
using DepotService.Models;

namespace DepotService.ViewModels
{
    public class LocationItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string Name { get; set; } = "";

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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly SqlRepository _repo;
        private string _searchText = "";
        private string _statusMessage = "Bereit";
        private bool _isLoading = false;
        private string _connectionState = "Unknown";
        private bool _isFilterPopupOpen = false;
        private bool? _selectAll = false;
        private string _jobNameInput = "";

        public ObservableCollection<DepotItem> Depots { get; } = new();
        public ObservableCollection<LocationItem> Locations { get; } = new();

        public ICommand SyncAllCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ClearFilterCommand { get; }
        public ICommand RemoveLocationCommand { get; }

        public MainViewModel(SqlRepository repo)
        {
            _repo = repo;
            SyncAllCommand = new RelayCommand(async _ => await SyncSelectedAsync(), _ => Depots.Any(d => d.IsSelected));
            RefreshCommand = new RelayCommand(async _ => await LoadAsync());
            ClearFilterCommand = new RelayCommand(_ => ClearFilter());
            RemoveLocationCommand = new RelayCommand(location => RemoveLocation(location as LocationItem));
        }

        #region Properties

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    FilterDepots();
                }
            }
        }

        public string JobNameInput
        {
            get => _jobNameInput;
            set
            {
                if (_jobNameInput != value)
                {
                    _jobNameInput = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ConnectionState
        {
            get => _connectionState;
            set
            {
                if (_connectionState != value)
                {
                    _connectionState = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsFilterPopupOpen
        {
            get => _isFilterPopupOpen;
            set
            {
                if (_isFilterPopupOpen != value)
                {
                    _isFilterPopupOpen = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool? SelectAll
        {
            get => _selectAll;
            set
            {
                if (_selectAll != value)
                {
                    _selectAll = value;
                    OnPropertyChanged();
                    if (value.HasValue)
                    {
                        foreach (var depot in Depots)
                        {
                            depot.IsSelected = value.Value;
                        }
                    }
                }
            }
        }

        public bool? AllLocationsSelected
        {
            get
            {
                var selected = Locations.Count(l => l.IsSelected);
                if (selected == 0) return false;
                if (selected == Locations.Count) return true;
                return null;
            }
            set
            {
                if (value.HasValue)
                {
                    foreach (var location in Locations)
                    {
                        location.IsSelected = value.Value;
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedCount));
                    OnPropertyChanged(nameof(SelectedLocations));
                    FilterDepots();
                }
            }
        }

        public int SelectedCount => Locations.Count(l => l.IsSelected);

        public ObservableCollection<LocationItem> SelectedLocations =>
            new ObservableCollection<LocationItem>(Locations.Where(l => l.IsSelected));

        #endregion

        #region Methods

        public async Task InitializeAsync()
        {
            await TestConnectionAsync();
            await LoadAsync();
        }

        private async Task TestConnectionAsync()
        {
            try
            {
                var result = await _repo.TestConnectionAsync();
                ConnectionState = result.Success ? "Connected" : "Disconnected";
                StatusMessage = result.Message;
            }
            catch (Exception ex)
            {
                ConnectionState = "Disconnected";
                StatusMessage = $"Verbindungsfehler: {ex.Message}";
            }
        }

        public async Task LoadAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Lade Depots...";

                var locations = await _repo.GetLocationsAsync();
                Locations.Clear();
                foreach (var loc in locations)
                {
                    var locationItem = new LocationItem { Name = loc };
                    locationItem.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(LocationItem.IsSelected))
                        {
                            OnPropertyChanged(nameof(AllLocationsSelected));
                            OnPropertyChanged(nameof(SelectedCount));
                            OnPropertyChanged(nameof(SelectedLocations));
                            FilterDepots();
                        }
                    };
                    Locations.Add(locationItem);
                }

                await FilterDepots();

                StatusMessage = $"{Depots.Count} Depots geladen";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fehler beim Laden: {ex.Message}";
                MessageBox.Show($"Fehler beim Laden der Depots:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task FilterDepots()
        {
            try
            {
                var selectedLocations = Locations.Where(l => l.IsSelected).Select(l => l.Name).ToList();

                var allDepots = selectedLocations.Any()
                    ? (await Task.WhenAll(selectedLocations.Select(loc => _repo.GetDepotsAsync(loc)))).SelectMany(d => d).ToList()
                    : await _repo.GetDepotsAsync();

                var filtered = allDepots;

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var search = SearchText.ToLower();
                    filtered = filtered.Where(d =>
                        d.Computer.ToLower().Contains(search) ||
                        d.Domain.ToLower().Contains(search) ||
                        (d.LastJobName?.ToLower().Contains(search) ?? false)
                    ).ToList();
                }

                Depots.Clear();
                foreach (var depot in filtered)
                {
                    Depots.Add(depot);
                }

                OnPropertyChanged(nameof(Depots));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Filterfehler: {ex.Message}";
            }
        }

        public async Task SyncSelectedAsync()
        {
            try
            {
                var toSync = Depots.Where(d => d.IsSelected).ToList();

                if (!toSync.Any())
                {
                    MessageBox.Show("Bitte wählen Sie mindestens ein Depot aus.", "Keine Auswahl", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsLoading = true;
                StatusMessage = $"Erstelle {toSync.Count} Jobs...";

                int createdJobs = 0;
                foreach (var depot in toSync)
                {
                    var jobName = !string.IsNullOrWhiteSpace(JobNameInput)
                        ? JobNameInput
                        : depot.LastJobName ?? "ManualSync";

                    var parameters = new { Computer = depot.Computer, Domain = depot.Domain, JobName = jobName };
                    await _repo.CreateJobAsync("StartSync", 0, parameters);
                    createdJobs++;
                }

                StatusMessage = $"{createdJobs} Jobs erfolgreich erstellt";
                MessageBox.Show($"{createdJobs} Sync-Jobs wurden erfolgreich erstellt und werden von Empirum verarbeitet.",
                    "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fehler beim Erstellen der Jobs: {ex.Message}";
                MessageBox.Show($"Fehler beim Erstellen der Jobs:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ClearFilter()
        {
            foreach (var location in Locations)
            {
                location.IsSelected = false;
            }
            SearchText = "";
            OnPropertyChanged(nameof(AllLocationsSelected));
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(SelectedLocations));
        }

        private void RemoveLocation(LocationItem? location)
        {
            if (location != null)
            {
                location.IsSelected = false;
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}