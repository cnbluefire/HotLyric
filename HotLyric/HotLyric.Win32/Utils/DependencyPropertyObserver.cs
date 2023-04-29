using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace HotLyric.Win32.Utils
{
    public class DependencyPropertiesObserver : IDisposable
    {
        private bool disposedValue;
        private WeakReference<FrameworkElement> weakElement;
        private Dictionary<DependencyProperty, DependencyPropertyObserver> dict;

        public DependencyPropertiesObserver(FrameworkElement element)
        {
            if (element is null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            weakElement = new WeakReference<FrameworkElement>(element);
            dict = new Dictionary<DependencyProperty, DependencyPropertyObserver>();
        }

        public DependencyPropertyObserver? this[DependencyProperty dp] => GetObserver(dp);

        public DependencyPropertyObserver? GetObserver(DependencyProperty dp)
        {
            if (dp is null)
            {
                throw new ArgumentNullException(nameof(dp));
            }

            if (weakElement.TryGetTarget(out var target))
            {
                lock (dict)
                {
                    if (!dict.TryGetValue(dp, out var observer))
                    {
                        observer = new DependencyPropertyObserver(target, dp);
                        observer.Disposing += (s, a) =>
                        {
                            lock (dict)
                            {
                                if (s is DependencyPropertyObserver _s)
                                {
                                    dict.Remove(_s.DependencyProperty);
                                }
                            }
                        };
                        dict[dp] = observer;
                    }

                    return observer;
                }
            }

            return null;
        }

        public void Remove(DependencyProperty dp)
        {
            if (dp is null)
            {
                throw new ArgumentNullException(nameof(dp));
            }

            lock (dict)
            {
                if (dict.TryGetValue(dp, out var observer))
                {
                    observer.Dispose();
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                disposedValue = true;
            }
        }

        ~DependencyPropertiesObserver()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

    }

    public class DependencyPropertyObserver : IDisposable
    {
        private WeakReference<FrameworkElement> weakElement;
        private WeakEventListener<DependencyPropertyObserver, object, RoutedEventArgs> loadedListener;
        private WeakEventListener<DependencyPropertyObserver, object, RoutedEventArgs> unloadedListener;
        private DependencyProperty dp;
        private long? propertyToken;
        private bool disposedValue;
        private PropertyChangedEventHandler? propertyChangedHandler;
        private object? value;

        public DependencyPropertyObserver(FrameworkElement element, DependencyProperty property)
        {
            if (element is null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            if (property is null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            weakElement = new WeakReference<FrameworkElement>(element);
            this.dp = property;

            loadedListener = new WeakEventListener<DependencyPropertyObserver, object, RoutedEventArgs>(this)
            {
                OnEventAction = (obj, s, a) =>
                {
                    if (s is FrameworkElement sender && sender.IsLoaded)
                    {
                        AddDependencyHandler(obj);
                    }
                }
            };
            unloadedListener = new WeakEventListener<DependencyPropertyObserver, object, RoutedEventArgs>(this)
            {
                OnEventAction = (obj, s, a) =>
                {
                    if (s is FrameworkElement sender && !sender.IsLoaded)
                    {
                        RemoveDependencyHandler(obj);
                    }
                }
            };

            element.Loaded += loadedListener.OnEvent;
            element.Unloaded += unloadedListener.OnEvent;
        }

        public DependencyProperty DependencyProperty => dp;

        public object? CurrentValue => value;

        public T? GetValueOrDefault<T>(T? defaultValue = default)
        {
            var curValue = CurrentValue;

            if (!Equals(curValue, null) && curValue is T value) return value;
            return defaultValue;
        }

        private void OnPropertyChanged(DependencyObject sender, DependencyProperty dp)
        {
            var oldValue = CurrentValue;
            var newValue = sender.GetValue(dp);
            value = sender.GetValue(dp);
            propertyChangedHandler?.Invoke(this, new PropertyChangedEventArgs(sender, dp, newValue, oldValue));
        }

        public void AddHandler(PropertyChangedEventHandler handler)
        {
            propertyChangedHandler += handler;
        }

        public void RemoveHandler(PropertyChangedEventHandler handler)
        {
            propertyChangedHandler -= handler;
        }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => propertyChangedHandler += value;
            remove => propertyChangedHandler -= value;
        }

        internal event EventHandler? Disposing;


        private static void AddDependencyHandler(DependencyPropertyObserver obj)
        {
            if (obj.weakElement.TryGetTarget(out var target))
            {
                obj.propertyToken = target.RegisterPropertyChangedCallback(obj.dp, obj.OnPropertyChanged);

                obj.value = target.GetValue(obj.dp);

                var metadata = obj.dp.GetMetadata(target.GetType());
                var defaultValue = metadata.DefaultValue;

                if (!Equals(obj.value, defaultValue))
                {
                    obj.OnPropertyChanged(target, obj.dp);
                }
            }
        }

        private static void RemoveDependencyHandler(DependencyPropertyObserver obj)
        {
            if (obj.weakElement.TryGetTarget(out var target) && obj.propertyToken.HasValue)
            {
                target.UnregisterPropertyChangedCallback(obj.dp, obj.propertyToken!.Value);
                obj.propertyToken = null;
                obj.value = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Disposing?.Invoke(this, EventArgs.Empty);

                if (disposing)
                {
                }

                RemoveDependencyHandler(this);
                loadedListener.Detach();
                unloadedListener.Detach();

                disposedValue = true;
            }
        }

        ~DependencyPropertyObserver()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public delegate void PropertyChangedEventHandler(DependencyPropertyObserver sender, PropertyChangedEventArgs args);

        public record PropertyChangedEventArgs(DependencyObject Source, DependencyProperty Property, object? NewValue, object? OldValue);
    }
}
