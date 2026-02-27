using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using DepotService.Data;
using DepotService.Models;
using System.Linq;

namespace DepotService.ViewModels
{
    public class MainViewModel
    {
        private readonly SqlRepository _repo;
        public ObservableCollection<DepotItem> Depots { get; } = new();

        public ICommand SyncAllCommand { get; }

        public MainViewModel(SqlRepository repo)
        {
            _repo = repo;
            SyncAllCommand = new RelayCommand(async _ => await SyncSelectedAsync(), _ => Depots.Any(d => d.IsSelected));
        }

        public async Task LoadAsync()
        {
            var list = await _repo.GetDepotsAsync();
            Depots.Clear();
            foreach (var d in list) Depots.Add(d);
        }

        public async Task SyncSelectedAsync(string? jobName = null)
        {
            // wenn jobName null: benutze LastJobName oder setze Default
            var toSync = Depots.Where(d => d.IsSelected).ToList();
            foreach (var d in toSync)
            {
                var jn = jobName ?? d.LastJobName ?? "DefaultJobName";
                var parameters = new { Computer = d.Computer, Domain = d.Domain, JobName = jn };
                await _repo.CreateJobAsync("StartSync", 0, parameters);
            }
        }
    }
}