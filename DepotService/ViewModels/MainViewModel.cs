using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
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

    public class ComputerItem : INotifyPropertyChanged
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
        private readonly EmpirumRepository _repo;
        private string _searchText = "";
        private string _statusMessage = "Bereit";
        private bool _isLoading = false;
        private string _connectionState = "Unknown";
        private bool _isFilterPopupOpen = false;
        private bool _isComputerFilterPopupOpen = false;
        private bool? _selectAll = false;

        public ObservableCollection<DepotDto> Depots { get; } = new();
        public ICollectionView DepotsView { get; private set; }
        public ObservableCollection<LocationItem> Locations { get; } = new();
        public ObservableCollection<ComputerItem> Computers { get; } = new();

        public ICommand SyncAllCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ClearFilterCommand { get; }
        public ICommand ClearComputerFilterCommand { get; }
        public ICommand RemoveLocationCommand { get; }
        public ICommand RemoveComputerCommand { get; }

        public MainViewModel(EmpirumRepository repo)
        {
            _repo = repo;

            DepotsView = CollectionViewSource.GetDefaultView(Depots);
            DepotsView.Filter = FilterDepotsFunc;

            SyncAllCommand = new RelayCommand(async _ => await SyncSelectedAsync(), _ => Depots.Any(d => d.IsSelected));
            RefreshCommand = new RelayCommand(async _ => await LoadAsync());
            ClearFilterCommand = new RelayCommand(async _ => { ClearFilter(); await Task.CompletedTask; });
            ClearComputerFilterCommand = new RelayCommand(async _ => { ClearComputerFilter(); await Task.CompletedTask; });
            RemoveLocationCommand = new RelayCommand(async location => { RemoveLocation(location as LocationItem); await Task.CompletedTask; });
            RemoveComputerCommand = new RelayCommand(async computer => { RemoveComputer(computer as ComputerItem); await Task.CompletedTask; });
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
                    DepotsView.Refresh();
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

        public bool IsComputerFilterPopupOpen
        {
            get => _isComputerFilterPopupOpen;
            set
            {
                if (_isComputerFilterPopupOpen != value)
                {
                    _isComputerFilterPopupOpen = value;
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
                    _ = FilterDepots();
                }
            }
        }

        public bool? AllComputersSelected
        {
            get
            {
                var selected = Computers.Count(c => c.IsSelected);
                if (selected == 0) return false;
                if (selected == Computers.Count) return true;
                return null;
            }
            set
            {
                if (value.HasValue)
                {
                    foreach (var computer in Computers)
                    {
                        computer.IsSelected = value.Value;
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedComputerCount));
                    OnPropertyChanged(nameof(SelectedComputers));
                    DepotsView.Refresh();
                }
            }
        }

        public int SelectedCount => Locations.Count(l => l.IsSelected);

        public int SelectedComputerCount => Computers.Count(c => c.IsSelected);

        public ObservableCollection<LocationItem> SelectedLocations =>
            new ObservableCollection<LocationItem>(Locations.Where(l => l.IsSelected));

        public ObservableCollection<ComputerItem> SelectedComputers =>
            new ObservableCollection<ComputerItem>(Computers.Where(c => c.IsSelected));

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
                var (success, message) = await _repo.TestConnectionAsync();
                ConnectionState = success ? "Connected" : "Disconnected";
                StatusMessage = message;
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

                var allDepots = await _repo.GetDepotsAsync();

                var locations = allDepots.Select(d => d.Domain).Distinct().OrderBy(x => x).ToList();
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
                            _ = FilterDepots();
                        }
                    };
                    Locations.Add(locationItem);
                }

                var computers = allDepots.Select(d => d.Computer).Distinct().OrderBy(c => c).ToList();
                Computers.Clear();
                foreach (var comp in computers)
                {
                    var computerItem = new ComputerItem { Name = comp };
                    computerItem.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(ComputerItem.IsSelected))
                        {
                            OnPropertyChanged(nameof(AllComputersSelected));
                            OnPropertyChanged(nameof(SelectedComputerCount));
                            OnPropertyChanged(nameof(SelectedComputers));
                            DepotsView.Refresh();
                        }
                    };
                    Computers.Add(computerItem);
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

        private bool FilterDepotsFunc(object obj)
        {
            if (obj is not DepotDto depot)
                return false;

            var selectedComputers = Computers.Where(c => c.IsSelected).Select(c => c.Name).ToList();
            if (selectedComputers.Any() && !selectedComputers.Contains(depot.Computer))
                return false;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLower();
                if (!depot.Computer.ToLower().Contains(search) && !depot.Domain.ToLower().Contains(search))
                    return false;
            }

            return true;
        }

        private async Task FilterDepots()
        {
            try
            {
                var selectedLocations = Locations.Where(l => l.IsSelected).Select(l => l.Name).ToHashSet();
                var allDepots = await _repo.GetDepotsAsync();

                var filteredDepots = selectedLocations.Any()
                    ? allDepots.Where(d => selectedLocations.Contains(d.Domain)).ToList()
                    : allDepots;

                Depots.Clear();
                foreach (var depot in filteredDepots)
                {
                    Depots.Add(depot);
                }

                DepotsView.Refresh();
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

                var jobNames = await _repo.GetJobNamesAsync();
                var selectedJobName = jobNames.FirstOrDefault() ?? "ManualSync";

                IsLoading = true;
                StatusMessage = $"Erstelle {toSync.Count} Jobs...";

                await _repo.EnqueueStartSyncForManyAsync(toSync, selectedJobName);

                StatusMessage = $"{toSync.Count} Jobs erfolgreich erstellt";
                MessageBox.Show($"{toSync.Count} Sync-Jobs wurden erfolgreich erstellt und werden von Empirum verarbeitet.",
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
            foreach (var computer in Computers)
            {
                computer.IsSelected = false;
            }
            SearchText = "";
            OnPropertyChanged(nameof(AllLocationsSelected));
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(SelectedLocations));
            OnPropertyChanged(nameof(AllComputersSelected));
            OnPropertyChanged(nameof(SelectedComputerCount));
            OnPropertyChanged(nameof(SelectedComputers));
        }

        private void RemoveLocation(LocationItem? location)
        {
            if (location != null)
            {
                location.IsSelected = false;
            }
        }

        private void RemoveComputer(ComputerItem? computer)
        {
            if (computer != null)
            {
                computer.IsSelected = false;
            }
        }

        private void ClearComputerFilter()
        {
            foreach (var computer in Computers)
            {
                computer.IsSelected = false;
            }
            OnPropertyChanged(nameof(AllComputersSelected));
            OnPropertyChanged(nameof(SelectedComputerCount));
            OnPropertyChanged(nameof(SelectedComputers));
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