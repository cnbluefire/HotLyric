using System;
using System.Windows;

namespace HotLyric.Win32.Utils.WindowBackgrounds
{
    internal abstract class WindowBackgroundProvider : IDisposable
    {
        private bool disposedValue;
        private bool isHitTestVisible;

        public bool IsDisposed => disposedValue;

        public WindowBackgroundProvider(Window window) { }

        public bool IsHitTestVisible
        {
            get => isHitTestVisible;
            protected set
            {
                if (isHitTestVisible != value)
                {
                    isHitTestVisible = value;
                    OnIsHitTestVisibleChanged();
                }
            }
        }

        protected virtual void OnIsHitTestVisibleChanged()
        {
            IsHitTestVisibleChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? IsHitTestVisibleChanged;

        protected abstract void DisposeCore();

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    DisposeCore();
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~WindowBackgroundProvider()
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
    }
}
