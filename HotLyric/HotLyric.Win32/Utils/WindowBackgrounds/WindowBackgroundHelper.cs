using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Vanara.PInvoke;

namespace HotLyric.Win32.Utils.WindowBackgrounds
{
    public class WindowBackgroundHelper : ObservableObject, IDisposable
    {
        public WindowBackgroundHelper(Window window)
        {
            this.window = window;

            popupTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(0.5)
            };
            popupTimer.Tick += (s, a) => UpdateHitTestVisible();

            UpdateProvider();
        }

        private static readonly Brush hitTestBrush = new SolidColorBrush(Color.FromArgb(1, 255, 255, 255));
        private static readonly Brush transparentBrush = Brushes.Transparent;
        private readonly Window window;
        private bool disposedValue;

        private WindowBackgroundProvider? provider;
        private bool isHitTestVisible;
        private bool isTransparent;
        private bool forceVisible;
        private DispatcherTimer popupTimer;

        public bool IsTransparent
        {
            get => isTransparent;
            set
            {
                if (isTransparent != value)
                {
                    isTransparent = value;

                    if (provider != null)
                    {
                        provider.IsTransparent = value;
                    }

                    UpdateHitTestVisible();
                }
            }
        }

        public bool ForceVisible
        {
            get => forceVisible;
            set
            {
                forceVisible = value;
                UpdateHitTestVisible();
            }
        }

        public bool IsHitTestVisible
        {
            get => isHitTestVisible;
            private set
            {
                if (SetProperty(ref isHitTestVisible, value))
                {
                    OnPropertyChanged(nameof(Background));
                }
            }
        }

        public Brush Background => IsHitTestVisible ? hitTestBrush : transparentBrush;

        private void UpdateHitTestVisible()
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(WindowBackgroundHelper));

            var hasOpenedPopup = WindowTopmostHelper.HasOpenedPopup();

            if (hasOpenedPopup)
            {
                popupTimer.Start();
                IsHitTestVisible = true;
            }
            else
            {
                popupTimer.Stop();
                IsHitTestVisible = !IsTransparent && (provider?.IsHitTestVisible ?? ForceVisible);
            }
        }

        public void UpdatePopupState()
        {
            UpdateHitTestVisible();
        }

        private void UpdateProvider()
        {
            if (provider != null)
            {
                provider.IsHitTestVisibleChanged -= Provider_IsHitTestVisibleChanged;
                provider?.Dispose();
            }
            provider = CreateProvider(window);
            if (provider != null)
            {
                provider.IsTransparent = IsTransparent;
                provider.IsHitTestVisibleChanged += Provider_IsHitTestVisibleChanged;
            }

            UpdateHitTestVisible();
        }

        private void Provider_IsHitTestVisibleChanged(object? sender, EventArgs e)
        {
            UpdateHitTestVisible();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    popupTimer?.Stop();
                    popupTimer = null!;

                    if (provider != null)
                    {
                        provider.IsHitTestVisibleChanged -= Provider_IsHitTestVisibleChanged;
                        provider?.Dispose();
                    }

                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~WindowBackgroundHelper()
        // {
        //     // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private static WindowBackgroundProvider CreateProvider(Window window)
        {
            return new DefaultWindowBackgroundProvider(window);
        }
    }
}
