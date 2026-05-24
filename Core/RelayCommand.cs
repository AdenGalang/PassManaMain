using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PassManaAlpha.Core
{
    class RelayCommand : ICommand
    {
        private Action<object?>? execute;
        private Func<object?, Task>? executeAsync;
        private Func<object?, bool>? canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            this.execute = execute;
            this.canExecute = canExecute;
        }

        public RelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
        {
            this.executeAsync = execute;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return canExecute?.Invoke(parameter) ?? true;
        }

        public void Execute(object? parameter)
        {
            if (executeAsync != null)
                _ = executeAsync(parameter);
            else
                execute!(parameter);
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
