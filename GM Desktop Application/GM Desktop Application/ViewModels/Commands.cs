using System.Windows.Input;

namespace GM_Desktop_Application.ViewModels
{
    public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;
        public void Execute(object? parameter) { if (CanExecute(parameter)) execute(); }
        public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public sealed class AsyncCommand(Func<Task> execute, Func<bool>? canExecute = null) : ICommand
    {
        private bool executing;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => !executing && (canExecute?.Invoke() ?? true);
        public async void Execute(object? parameter) => await ExecuteAsync();
        public async Task ExecuteAsync()
        {
            if (!CanExecute(null)) return;
            executing = true;
            Refresh();
            try { await execute(); }
            finally { executing = false; Refresh(); }
        }
        public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
