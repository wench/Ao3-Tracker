/*
Copyright 2017 Alexis Ryan

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

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
