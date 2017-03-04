using System;
using System.Reflection;
using System.Windows.Input;

namespace Ao3TrackReader
{
    public class DisableableCommand<T> : DisableableCommand
    {
        public DisableableCommand(Action<T> execute, bool enabled = true) :
            base((o) => execute((T)o), enabled)
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));
        }
    }

    public class DisableableCommand : ICommand
    {
        private Action<object> execute;
        private bool enabled;
        public DisableableCommand(Action<object> execute, bool enabled = true)
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));

            this.execute = execute;
            this.enabled = enabled;
        }
        public DisableableCommand(Action execute, bool enabled = true)
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));

            this.execute = (o) => execute();
            this.enabled = enabled;
        }

        public bool IsEnabled
        {
            get { return enabled; }
            set
            {
                if (enabled == value) return;
                enabled = value;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return IsEnabled;
        }

        public void Execute(object parameter)
        {
            execute(parameter);
        }
    }
}
