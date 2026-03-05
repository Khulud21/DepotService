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
        private bool _suppressSelectAllUpdate;
        private string _selectedJobName = "";

        public ObservableCollection<DepotDto> Depots { get; } = new();
        public ICollectionView DepotsView { get; private set; }
        public ObservableCollection<SelectableItem> Locations { get; } = new();
        public ObservableCollection<SelectableItem> Computers { get; } = new();
        public ObservableCollection<string> JobNames { get; } = new();

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

            SyncAllCommand = new AsyncRelayCommand(_ => SyncSelectedAsync(), _ => Depots.Any(d => d.IsSelected));
            RefreshCommand = new AsyncRelayCommand(_ => LoadAsync());
            ClearFilterCommand = new RelayCommand(_ => { ClearFilter(); });
            ClearComputerFilterCommand = new RelayCommand(_ => { ClearComputerFilter(); });
            RemoveLocationCommand = new RelayCommand(location => { RemoveLocation(location as SelectableItem); });
            RemoveComputerCommand = new RelayCommand(computer => { RemoveComputer(computer as SelectableItem); });
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
                    UpdateSelectAllState();
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
                if (_selectAll == value) return;
                _selectAll = value;
                OnPropertyChanged();

                if (value.HasValue)
                {
                    _suppressSelectAllUpdate = true;
                    foreach (DepotDto depot in DepotsView)
                        depot.IsSelected = value.Value;
                    _suppressSelectAllUpdate = false;

                    UpdateSelectAllState();
                }
            }
        }

        public bool? AllLocationsSelected
        {
            get
            {
                if (Locations.Count == 0) return false;
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
                    DepotsView.Refresh();
                    UpdateSelectAllState();
                }
            }
        }

        public bool? AllComputersSelected
        {
            get
            {
                if (Computers.Count == 0) return false;
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
                    UpdateSelectAllState();
                }
            }
        }

        public int SelectedCount => Locations.Count(l => l.IsSelected);

        public int SelectedComputerCount => Computers.Count(c => c.IsSelected);

        public ObservableCollection<SelectableItem> SelectedLocations =>
            new ObservableCollection<SelectableItem>(Locations.Where(l => l.IsSelected));

        public ObservableCollection<SelectableItem> SelectedComputers =>
            new ObservableCollection<SelectableItem>(Computers.Where(c => c.IsSelected));

        public string SelectedJobName
        {
            get => _selectedJobName;
            set
            {
                if (_selectedJobName != value)
                {
                    _selectedJobName = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Methods

        private void UpdateSelectAllState()
        {
            if (_suppressSelectAllUpdate) return;

            var visible = DepotsView.Cast<DepotDto>().ToList();
            if (visible.Count == 0)
            {
                _selectAll = false;
                OnPropertyChanged(nameof(SelectAll));
                return;
            }

            int selected = visible.Count(d => d.IsSelected);
            bool? newValue = selected == 0 ? false : selected == visible.Count ? true : (bool?)null;

            if (_selectAll != newValue)
            {
                _selectAll = newValue;
                OnPropertyChanged(nameof(SelectAll));
            }
        }

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

                var jobNames = await _repo.GetJobNamesAsync();
                JobNames.Clear();
                foreach (var jobName in jobNames.OrderBy(j => j))
                {
                    JobNames.Add(jobName);
                }
                if (JobNames.Any() && string.IsNullOrEmpty(SelectedJobName))
                {
                    SelectedJobName = JobNames.First();
                }

                var locations = allDepots.Select(d => d.Domain).Distinct().OrderBy(x => x).ToList();
                Locations.Clear();
                foreach (var loc in locations)
                {
                    var locationItem = new SelectableItem(loc) { IsSelected = true };
                    locationItem.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(SelectableItem.IsSelected))
                        {
                            OnPropertyChanged(nameof(AllLocationsSelected));
                            OnPropertyChanged(nameof(SelectedCount));
                            OnPropertyChanged(nameof(SelectedLocations));
                            DepotsView.Refresh();
                            UpdateSelectAllState();
                        }
                    };
                    Locations.Add(locationItem);
                }

                var computers = allDepots.Select(d => d.Computer).Distinct().OrderBy(c => c).ToList();
                Computers.Clear();
                foreach (var comp in computers)
                {
                    var computerItem = new SelectableItem(comp) { IsSelected = true };
                    computerItem.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(SelectableItem.IsSelected))
                        {
                            OnPropertyChanged(nameof(AllComputersSelected));
                            OnPropertyChanged(nameof(SelectedComputerCount));
                            OnPropertyChanged(nameof(SelectedComputers));
                            DepotsView.Refresh();
                            UpdateSelectAllState();
                        }
                    };
                    Computers.Add(computerItem);
                }

                Depots.Clear();
                foreach (var depot in allDepots)
                {
                    depot.PropertyChanged += (_, e) =>
                    {
                        if (e.PropertyName == nameof(DepotDto.IsSelected))
                        {
                            UpdateSelectAllState();
                            CommandManager.InvalidateRequerySuggested();
                        }
                    };
                    Depots.Add(depot);
                }

                DepotsView.Refresh();
                UpdateSelectAllState();
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

            var selectedLocations = Locations.Where(l => l.IsSelected).Select(l => l.Name).ToHashSet();
            if (selectedLocations.Count > 0 && !selectedLocations.Contains(depot.Domain))
                return false;

            var selectedComputers = Computers.Where(c => c.IsSelected).Select(c => c.Name).ToHashSet();
            if (selectedComputers.Count > 0 && !selectedComputers.Contains(depot.Computer))
                return false;

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                if (!depot.Computer.Contains(SearchText, StringComparison.OrdinalIgnoreCase) &&
                    !depot.Domain.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
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

                if (string.IsNullOrEmpty(SelectedJobName))
                {
                    MessageBox.Show("Bitte wählen Sie einen Job aus.", "Kein Job ausgewählt", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsLoading = true;
                StatusMessage = $"Erstelle {toSync.Count} Jobs...";

                await _repo.EnqueueStartSyncForManyAsync(toSync, SelectedJobName);

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
                location.IsSelected = true;
            }
            foreach (var computer in Computers)
            {
                computer.IsSelected = true;
            }
            SearchText = "";
            OnPropertyChanged(nameof(AllLocationsSelected));
            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(SelectedLocations));
            OnPropertyChanged(nameof(AllComputersSelected));
            OnPropertyChanged(nameof(SelectedComputerCount));
            OnPropertyChanged(nameof(SelectedComputers));
        }

        private void RemoveLocation(SelectableItem? location)
        {
            if (location != null)
            {
                location.IsSelected = false;
            }
        }

        private void RemoveComputer(SelectableItem? computer)
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
                computer.IsSelected = true;
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