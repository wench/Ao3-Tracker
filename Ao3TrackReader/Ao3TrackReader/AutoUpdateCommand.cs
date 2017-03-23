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
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Xamarin.Forms;

namespace Ao3TrackReader
{
    public abstract class VirtualAutoUpdateCommand<T> : AutoUpdateCommand<T>
        where T : INotifyPropertyChanged
    {

        public VirtualAutoUpdateCommand(params string[] triggerProperties) : base(triggerProperties)
        {
            base.Execute = (p) => Execute();
            base.CanExecute = (p) => CanExecute();
        }

        new protected abstract bool CanExecute();
        new protected abstract void Execute();
    }

    public class AutoUpdateCommand<T> : AutoUpdateCommand
        where T : INotifyPropertyChanged
    {
        new protected T Target
        {
            get { return (T)base.Target; }
            set { base.Target = value; }
        }

        protected AutoUpdateCommand(string[] triggerProperties) : base(triggerProperties)
        {
        }

        public AutoUpdateCommand(Action<T> execute, Func<T, bool> canExecute, params string[] triggerProperties)
            : base((o) => execute((T)o), (o) => canExecute((T)o), triggerProperties)
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));
            if (canExecute == null)
                throw new ArgumentNullException(nameof(canExecute));
        }
    }

    public class AutoUpdateCommand : BindableObject, System.Windows.Input.ICommand
    {
        protected Action<INotifyPropertyChanged> Execute { get; set; }
        protected Func<INotifyPropertyChanged, bool> CanExecute { get; set; }
        protected string[] TriggerProperties { get; set; }
        protected INotifyPropertyChanged Target { get; set; }

        protected AutoUpdateCommand(string[] triggerProperties) 
        {
            this.TriggerProperties = triggerProperties;
        }

        public AutoUpdateCommand(Action<object> execute, Func<object, bool> canExecute, params string[] triggerProperties)
        {
            this.Execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.CanExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
            this.TriggerProperties = triggerProperties;
        }
        public AutoUpdateCommand(Action execute, Func<bool> canExecute, params string[] triggerProperties)
        {
            if (execute == null)
                throw new ArgumentNullException(nameof(execute));
            if (canExecute == null)
                throw new ArgumentNullException(nameof(canExecute));

            this.Execute = (o) => execute();
            this.CanExecute = (o) => canExecute();
            this.TriggerProperties = triggerProperties;
        }

        void UpdateTarget(INotifyPropertyChanged newTarget)
        {
            if (Target != newTarget)
            {
                if (Target != null) Target.PropertyChanged -= Target_PropertyChanged;
                Target = newTarget;
                if (Target != null) Target.PropertyChanged += Target_PropertyChanged;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Target_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) || Array.IndexOf(TriggerProperties,e.PropertyName) != -1)
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler CanExecuteChanged;

        bool System.Windows.Input.ICommand.CanExecute(object parameter)
        {
            UpdateTarget((INotifyPropertyChanged)parameter);
            return CanExecute(Target);
        }

        void System.Windows.Input.ICommand.Execute(object parameter)
        {
            UpdateTarget((INotifyPropertyChanged)parameter);
            Execute(Target);
        }
    }
}
