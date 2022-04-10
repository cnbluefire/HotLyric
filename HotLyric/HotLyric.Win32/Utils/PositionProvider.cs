using Kfstorm.LrcParser;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotLyric.Win32.Utils
{
    public class PositionProvider
    {
        private readonly TimeSpan tolerance;
        private long startTick;
        private TimeSpan startPosition;
        private double playbackRate = 1;
        private IOneLineLyric? currentLyric;

        private TimeSpan currentPosition;

        public PositionProvider(TimeSpan tolerance, ILrcFile? lrcFile)
        {
            this.tolerance = tolerance;
            LrcFile = lrcFile;
        }

        public double PlaybackRate
        {
            get => playbackRate;
            set
            {
                if (playbackRate != value)
                {
                    playbackRate = value;
                    RaisePositionChanged();
                }
            }
        }

        public TimeSpan CurrentPosition => currentPosition;

        public ILrcFile? LrcFile { get; private set; }

        public void ChangePosition(TimeSpan position)
        {
            if (LrcFile == null) return;

            if (startTick == 0)
            {
                startTick = Stopwatch.GetTimestamp();
                startPosition = position;
                currentPosition = startPosition;
                UpdateLyric();

                RaisePositionChanged();
            }
            else
            {
                var diffTick = (Stopwatch.GetTimestamp() - startTick) * playbackRate;
                var internalPosition = startPosition + TimeSpan.FromTicks((long)diffTick);
                if (Math.Abs(internalPosition.Ticks - position.Ticks) < Math.Abs(tolerance.Ticks))
                {
                    // 误差内
                    var curLyric = currentLyric;
                    currentPosition = position;
                    UpdateLyric();
                    if (curLyric?.Timestamp != currentLyric?.Timestamp)
                    {
                        // 下一句 触发修改
                        RaisePositionChanged();
                    }
                }
                else
                {
                    // 误差外
                    startTick = Stopwatch.GetTimestamp();
                    startPosition = position;
                    currentPosition = startPosition;
                    UpdateLyric();
                    RaisePositionChanged();
                }
            }
        }

        public void Reset(ILrcFile? lrcFile)
        {
            LrcFile = lrcFile;
            currentLyric = null;
            startTick = 0;
        }

        private void UpdateLyric()
        {
            currentLyric = LrcFile?.BeforeOrAt(currentPosition);
        }

        private void RaisePositionChanged()
        {
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? PositionChanged;
    }
}
