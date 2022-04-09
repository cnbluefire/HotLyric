using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotLyric.Input
{
    internal class MouseShakeHelper
    {
        private static object staticLocker = new object();
        private static double[] angleArr = null!;
        private DateTime? lastTime;

        public MouseShakeHelper()
        {
            if (angleArr == null)
            {
                lock (staticLocker)
                {
                    if (angleArr == null)
                    {
                        angleArr = new double[] { 22.5, 67.5, 112.5, 157.5, 202.5, 247.5, 292.5, 337.5 };
                    }
                }
            }
        }

        private object locker = new object();
        private Stack<int> angleIdxes = new Stack<int>();
        private int shakeCount = 5;

        public int ShakeCount
        {
            get => shakeCount;
            set
            {
                if (value < 1) throw new ArgumentException(nameof(ShakeCount));
                if (shakeCount != value)
                {
                    bool shakedFlag = false;
                    lock (locker)
                    {
                        shakeCount = value;
                        if (shakeCount < angleIdxes.Count)
                        {
                            angleIdxes.Clear();
                            shakedFlag = true;
                        }
                    }

                    if (shakedFlag)
                    {
                        OnShaked();
                    }
                }
            }
        }

        public void ProcessMouseMoveAngleChanged(double angle)
        {
            bool shakedFlag = false;
            lock (locker)
            {
                var i = 0;
                var now = DateTime.Now;

                if (lastTime.HasValue && (now - lastTime.Value).TotalSeconds > 0.2)
                {
                    angleIdxes.Clear();
                }

                for (; i < angleArr.Length; i++)
                {
                    if (angleArr[i] >= angle) break;
                }

                if (i >= angleArr.Length)
                {
                    i = 0;
                }

                if (angleIdxes.Count == 0)
                {
                    lastTime = now;
                    angleIdxes.Push(i);
                }
                else
                {
                    var peek = angleIdxes.Peek();

                    if (i != peek)
                    {
                        lastTime = now;

                        if (i != peek + 4 && i != peek - 4)
                        {
                            angleIdxes.Clear();
                        }
                        angleIdxes.Push(i);
                    }
                }

                if (angleIdxes.Count >= ShakeCount)
                {
                    angleIdxes.Clear();
                    shakedFlag = true;
                }
            }

            if (shakedFlag)
            {
                OnShaked();
            }
        }

        public void Reset()
        {
            angleIdxes?.Clear();
        }

        private void OnShaked()
        {

            Shaked?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? Shaked;
    }
}
