using System;
using System.Windows;
using Microsoft.Extensions.Configuration;
using DepotService.Data;
using DepotService.ViewModels;

namespace DepotService
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();

            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var configuration = builder.Build();
            var conn = configuration.GetConnectionString("DefaultConnection")
                       ?? throw new InvalidOperationException("Connection string fehlt.");

            var repo = new EmpirumRepository(conn);
            _vm = new MainViewModel(repo);
            DataContext = _vm;

            Loaded += async (_, __) => await _vm.InitializeAsync();
        }
    }
}
