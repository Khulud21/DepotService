using System;
using System.Windows.Input;

namespace DepotService.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Func<object?, System.Threading.Tasks.Task> _executeAsync;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Func<object?, System.Threading.Tasks.Task> executeAsync, Predicate<object?>? canExecute = null)
        {
            _executeAsync = executeAsync;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public async void Execute(object? parameter) => await _executeAsync(parameter);

        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}