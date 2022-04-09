using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Vanara.PInvoke;

namespace HotLyric.Input
{
    public class MouseRecognizer : IDisposable
    {
        static MouseRecognizer()
        {

        }

        private const long VelocityRecognizerMinInterval = 100 * TimeSpan.TicksPerMillisecond;
        private const int TimerInterval = 200;

        private System.Threading.Timer timer;
        private volatile bool timerEnabled;
        private bool timerStartFirstCall;
        private bool disposedValue;
        private MouseButtonClickData clickData;
        private volatile int xVelocity;
        private volatile int yVelocity;
        private volatile int x;
        private volatile int y;
        private volatile int lastX;
        private volatile int lastY;

        private int lastAngleX;
        private int lastAngleY;
        private bool isFirstAnglePos;

        private long lastTime;
        private MouseShakeHelper shakeHelper;

        public MouseRecognizer()
        {
            timer = new System.Threading.Timer(Timer_Callback, null, 0, TimerInterval);
            shakeHelper = new MouseShakeHelper();
            shakeHelper.Shaked += (s, a) =>
            {
                MouseShaked?.Invoke(this, EventArgs.Empty);
            };

            clickData = new MouseButtonClickData();
            clickData.Click += (a) =>
            {
                MouseClick?.Invoke(this, a);
            };
            clickData.DoubleClick += (a) =>
            {
                MouseDoubleClick?.Invoke(this, a);
            };
        }

        public int XVelocity => xVelocity;

        public int YVelocity => yVelocity;

        public int ShakeCount
        {
            get => shakeHelper.ShakeCount;
            set => shakeHelper.ShakeCount = value;
        }

        public void ProcessMouseMove(int x, int y)
        {
            this.x = x;
            this.y = y;

            if (timerStartFirstCall)
            {
                lastX = x;
                lastY = y;
                lastTime = Stopwatch.GetTimestamp();
                timerStartFirstCall = false;
            }
            else
            {
                var t = Stopwatch.GetTimestamp();
                var diff = t - lastTime;
                if (diff > VelocityRecognizerMinInterval)
                {
                    lastTime = t;

                    var vx = (int)((x - lastX) * TimeSpan.TicksPerSecond / diff);
                    var vy = (int)((y - lastY) * TimeSpan.TicksPerSecond / diff);
                    lastX = x;
                    lastY = y;

                    if (vx != xVelocity || vy != yVelocity)
                    {
                        xVelocity = vx;
                        yVelocity = vy;

                        MouseVelocitiesChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            }

            ProcessMouseMoveAngle(x, y);

            ResetTimer();
        }

        public void ProcessMouseMove(System.Drawing.Point point) =>
            ProcessMouseMove(point.X, point.Y);

        private void ProcessMouseMoveAngle(int x, int y)
        {
            if (isFirstAnglePos)
            {
                isFirstAnglePos = false;
                lastAngleX = x;
                lastAngleY = y;
            }
            else
            {
                var distX = (x - lastAngleX);
                var distY = (y - lastAngleY);

                var sqrDist = distX * distX + distY * distY;
                if (sqrDist > 22500)
                {
                    var startPoint = new System.Drawing.Point(lastAngleX, lastAngleY);
                    var endPoint = new System.Drawing.Point(x, y);

                    var vector = new System.Drawing.Point(endPoint.X - startPoint.X, endPoint.Y - startPoint.Y);
                    var unit = new System.Drawing.Point(1, 0);

                    var cross = vector.X * unit.Y - vector.Y * unit.X;
                    var dot = vector.X * unit.X + vector.Y * unit.Y;

                    var angle = Math.Atan2(cross, dot) * 180 / Math.PI;

                    while (angle < 0)
                    {
                        angle += 360;
                    }

                    lastAngleX = x;
                    lastAngleY = y;

                    MouseMoveAngleChanged?.Invoke(this, new Events.MouseMoveAngleChangedEventArgs(startPoint, endPoint, angle));
                    shakeHelper.ProcessMouseMoveAngleChanged(angle);
                }
            }
        }

        public void ProcessMouseDown(int x, int y, MouseButton button)
        {
            shakeHelper.Reset();
            clickData.Down(x, y, button);
        }

        public void ProcessMouseDown(System.Drawing.Point point, MouseButton button) =>
            ProcessMouseDown(point.X, point.Y, button);


        public void ProcessMouseUp(int x, int y, MouseButton button)
        {
            shakeHelper.Reset();
            clickData.Up(x, y, button);
        }

        public void ProcessMouseUp(System.Drawing.Point point, MouseButton button) =>
            ProcessMouseUp(point.X, point.Y, button);

        public void ProcessMouseWheel(int delta, Events.Orientation orientation)
        {

        }


        public event EventHandler<Events.MouseButtonEventArgs>? MouseClick;
        public event EventHandler<Events.MouseButtonEventArgs>? MouseDoubleClick;
        public event EventHandler? MouseVelocitiesChanged;
        public event EventHandler<Events.MouseMoveAngleChangedEventArgs>? MouseMoveAngleChanged;
        public event EventHandler? MouseShaked;

        private void Timer_Callback(object? state)
        {
            if (xVelocity != 0 || yVelocity != 0)
            {
                xVelocity = 0;
                yVelocity = 0;
                MouseVelocitiesChanged?.Invoke(this, EventArgs.Empty);
            }
            timerEnabled = false;
        }

        private void StartTimer()
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(MouseRecognizer));

            if (timerEnabled) return;

            timerEnabled = true;
            timer.Change(TimerInterval, Timeout.Infinite);
        }

        private void StopTimer()
        {
            if (!timerEnabled) return;

            timerEnabled = false;
            timer.Change(-1, 0);
        }

        private void ResetTimer()
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(MouseRecognizer));

            if (timerEnabled)
            {
                timer.Change(TimerInterval, Timeout.Infinite);
            }
            else
            {
                StartTimer();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    StopTimer();
                    timer.Dispose();
                    // TODO: 释放托管状态(托管对象)
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        // ~MouseRecognizer()
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

        private class MouseButtonClickData
        {
            public MouseButtonClickData()
            {
                LeftButton = new MouseButtonClickDataItem(MouseButton.Left, this);
                RightButton = new MouseButtonClickDataItem(MouseButton.Right, this);
                MiddleButton = new MouseButtonClickDataItem(MouseButton.Middle, this);
                XButton1 = new MouseButtonClickDataItem(MouseButton.XButton1, this);
                XButton2 = new MouseButtonClickDataItem(MouseButton.XButton2, this);
            }

            public void Down(int x, int y, MouseButton button)
            {
                switch (button)
                {
                    case MouseButton.Left:
                        LeftButton.Down(x, y);
                        break;
                    case MouseButton.Middle:
                        RightButton.Down(x, y);
                        break;
                    case MouseButton.Right:
                        MiddleButton.Down(x, y);
                        break;
                    case MouseButton.XButton1:
                        XButton1.Down(x, y);
                        break;
                    case MouseButton.XButton2:
                        XButton2.Down(x, y);
                        break;
                    default:
                        break;
                }
            }

            public void Up(int x, int y, MouseButton button)
            {
                switch (button)
                {
                    case MouseButton.Left:
                        LeftButton.Up(x, y);
                        break;
                    case MouseButton.Middle:
                        RightButton.Up(x, y);
                        break;
                    case MouseButton.Right:
                        MiddleButton.Up(x, y);
                        break;
                    case MouseButton.XButton1:
                        XButton1.Up(x, y);
                        break;
                    case MouseButton.XButton2:
                        XButton2.Up(x, y);
                        break;
                    default:
                        break;
                }
            }

            public MouseButtonClickDataItem LeftButton { get; }

            public MouseButtonClickDataItem RightButton { get; }

            public MouseButtonClickDataItem MiddleButton { get; }

            public MouseButtonClickDataItem XButton1 { get; }

            public MouseButtonClickDataItem XButton2 { get; }

            public event Events.MouseButtonEventHandler? Click;

            public event Events.MouseButtonEventHandler? DoubleClick;

            internal void RaiseClickEvent(Events.MouseButtonEventArgs args)
            {
                Click?.Invoke(args);
            }

            internal void RaiseDoubleClickEvent(Events.MouseButtonEventArgs args)
            {
                DoubleClick?.Invoke(args);
            }
        }

        private class MouseButtonClickDataItem
        {
            static MouseButtonClickDataItem()
            {
                UpdateDoubleClickTime();
                SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
            }

            private const uint DefaultDoubleClickTime = 500;

            private static uint DoubleClickTime { get; set; }

            private static void UpdateDoubleClickTime()
            {
                var v = User32.GetDoubleClickTime();
                DoubleClickTime = v == 0 ? DefaultDoubleClickTime : v;
            }


            private MouseButtonClickData parent;

            private volatile int x;
            private volatile int y;

            //private volatile int clicks;
            private long lastClickTick;

            public MouseButtonClickDataItem(MouseButton mouseButton, MouseButtonClickData parent)
            {
                MouseButton = mouseButton;
                this.parent = parent;
            }

            public MouseButton MouseButton { get; }


            public void Down(int x, int y)
            {
                if (this.x != x || this.y != y)
                {
                    lastClickTick = 0;
                }

                this.x = x;
                this.y = y;
            }

            public void Up(int x, int y)
            {
                if (this.x != x || this.y != y)
                {
                    lastClickTick = 0;

                    this.x = x;
                    this.y = y;
                }
                else
                {
                    this.x = x;
                    this.y = y;

                    var tick = Stopwatch.GetTimestamp() / TimeSpan.TicksPerMillisecond;

                    if (tick - lastClickTick <= DoubleClickTime)
                    {
                        lastClickTick = 0;

                        var args = Events.MouseButtonEventArgs.Create(MouseButton);
                        parent.RaiseDoubleClickEvent(args);
                    }
                    else
                    {
                        lastClickTick = tick;

                        var args = Events.MouseButtonEventArgs.Create(MouseButton);
                        parent.RaiseClickEvent(args);
                    }
                }
            }


            private static void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
            {
                if (e.Category == UserPreferenceCategory.Mouse)
                {
                    UpdateDoubleClickTime();
                }
            }

        }
    }
}
