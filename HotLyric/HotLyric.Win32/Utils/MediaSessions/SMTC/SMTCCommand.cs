using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace HotLyric.Win32.Utils.MediaSessions.SMTC
{
    internal class SMTCCommand : ICommand, INotifyPropertyChanged
    {
        private static readonly PropertyChangedEventArgs CanExecutePropertyChangedEventArgs = new PropertyChangedEventArgs(nameof(CanExecute));

        private bool canExecute;
        private Func<Task> action;
        private Task? curTask;

        public SMTCCommand(Func<Task> action)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
        }

        internal bool CanExecute
        {
            get => canExecute;
            set
            {
                if (canExecute != value)
                {
                    canExecute = value;
                    NotifyCanExecuteChanged();
                }
            }
        }

        public event EventHandler? CanExecuteChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        internal void UpdateAction(Func<Task> action)
        {
            this.action = action ?? throw new ArgumentNullException(nameof(action));
        }

        bool ICommand.CanExecute(object? parameter)
        {
            return CanExecute;
        }

        public void Execute(object? parameter)
        {
            if (curTask != null) return;
            RunCore();

            async void RunCore()
            {
                try
                {
                    curTask = action.Invoke();
                    await curTask;
                }
                catch { }
                finally
                {
                    curTask = null;
                }
                NotifyCanExecuteChanged();
            }
        }

        internal void NotifyCanExecuteChanged()
        {
            _ = DispatcherHelper.UIDispatcher?.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                PropertyChanged?.Invoke(this, CanExecutePropertyChangedEventArgs);
            }));
        }
    }
}
