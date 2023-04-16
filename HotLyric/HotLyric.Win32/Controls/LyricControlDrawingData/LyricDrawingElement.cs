using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotLyric.Win32.Controls.LyricControlDrawingData
{
    internal class LyricDrawingElement : IDisposable
    {
        private bool disposedValue;

        protected bool IsDisposed => disposedValue;

        protected virtual void DisposeCore(bool disposing) { }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                DisposeCore(disposing);

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
        }
    }
}
