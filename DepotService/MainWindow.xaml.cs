using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Configuration;
using DepotService.Data;
using DepotService.ViewModels;

namespace DepotService
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly MainViewModel _vm;
        private bool _filterComputerEnabled;
        private bool _filterJobNameEnabled;

        public MainWindow()
        {
            InitializeComponent();

            // appsettings.json aus Projektverzeichnis lesen
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            var configuration = builder.Build();
            var conn = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(conn))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' fehlt in appsettings.json.");
            }

            var repo = new SqlRepository(conn);
            _vm = new MainViewModel(repo);
            DataContext = _vm;

            Loaded += async (_, __) => await _vm.LoadAsync();
        }

        public bool FilterComputerEnabled
        {
            get => _filterComputerEnabled;
            set
            {
                if (_filterComputerEnabled == value) return;
                _filterComputerEnabled = value;
                OnPropertyChanged(nameof(FilterComputerEnabled));
                // TODO: Filterlogik auslösen (z. B. ViewModel/DB-Abfrage)
            }
        }

        public bool FilterJobNameEnabled
        {
            get => _filterJobNameEnabled;
            set
            {
                if (_filterJobNameEnabled == value) return;
                _filterJobNameEnabled = value;
                OnPropertyChanged(nameof(FilterJobNameEnabled));
                // TODO: Filterlogik auslösen (z. B. ViewModel/DB-Abfrage)
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void DataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }
    }
}
