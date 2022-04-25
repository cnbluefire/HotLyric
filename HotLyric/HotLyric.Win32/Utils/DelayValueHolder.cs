using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Windows.Threading;

namespace HotLyric.Win32.Utils
{
    internal class DelayValueHolder<T> : INotifyPropertyChanged, IDisposable
    {
        private static PropertyChangedEventArgs valueChangedArgs = new PropertyChangedEventArgs("Value");
        private static PropertyChangedEventArgs nextValueChangedArgs = new PropertyChangedEventArgs("NextValue");
        private static PropertyChangedEventArgs hasNextValueChangedArgs = new PropertyChangedEventArgs("HasNextValue");

        private bool disposeValue;
        private DispatcherTimer timer;

        [field: MaybeNull]
        [field: AllowNull]
        private T value;

        [field: MaybeNull]
        [field: AllowNull]
        private T nextValue;

        private bool hasNextValue;

        public DelayValueHolder() : this(default, TimeSpan.FromSeconds(1)) { }

        public DelayValueHolder(TimeSpan delay) : this(default, delay) { }

        public DelayValueHolder([AllowNull] T initValue, TimeSpan delay)
        {
            VerifyAccess();

            value = initValue;
            timer = new DispatcherTimer()
            {
                Interval = delay
            };
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            Value = nextValue;
        }

        [property: MaybeNull]
        [property: AllowNull]
        public T Value
        {
            get => value;
            set
            {
                var oldHasNext = hasNextValue;
                CancelCore();
                VerifyAccess();
                if (!object.Equals(this.value, value))
                {
                    this.value = value;
                    OnValueChanged();
                }

                if (oldHasNext)
                {
                    OnPropertyChanged(nextValueChangedArgs);
                    OnPropertyChanged(hasNextValueChangedArgs);
                }
            }
        }

        [property: MaybeNull]
        public T NextValue => nextValue;

        public bool HasNextValue => hasNextValue;

        public void SetValueDelay([AllowNull] T value) => SetValueDelay(value, timer.Interval);

        public void SetValueDelay([AllowNull] T value, TimeSpan delay)
        {
            CancelCore();
            VerifyAccess();
            timer.Interval = delay;
            hasNextValue = true;
            nextValue = value;
            timer.Start();

            OnPropertyChanged(nextValueChangedArgs);
            OnPropertyChanged(hasNextValueChangedArgs);
        }

        public void CancelCore()
        {
            ThrowIfDisposed();

            if (timer.IsEnabled)
            {
                timer.Stop();
            }
            nextValue = default;
            hasNextValue = false;
        }

        public void Cancel()
        {
            Value = value;
        }

        private void VerifyAccess()
        {
            if (DispatcherHelper.UIDispatcher == null || !DispatcherHelper.UIDispatcher.CheckAccess())
            {
                throw new InvalidOperationException(nameof(Dispatcher));
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposeValue)
            {
                throw new ObjectDisposedException("DelayValueHolder");
            }
        }

        private void OnValueChanged()
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
            OnPropertyChanged(valueChangedArgs);
        }

        private void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChanged?.Invoke(this, args);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler? ValueChanged;

        public void Dispose()
        {
            if (!disposeValue)
            {
                CancelCore();
                timer.Tick -= Timer_Tick;
                timer = null!;
                value = default;

                disposeValue = true;
            }
        }
    }
}
