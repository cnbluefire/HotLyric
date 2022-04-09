using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace HotLyric.Win32.Controls
{
    public class ResizeControl : ContentControl
    {
        static ResizeControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ResizeControl), new FrameworkPropertyMetadata(typeof(ResizeControl)));
            BorderThicknessProperty.OverrideMetadata(typeof(ResizeControl), new FrameworkPropertyMetadata(default(Thickness), OnBorderThicknessPropertyChanged));
        }

        private Thumb? leftThumb;
        private Thumb? topThumb;
        private Thumb? rightThumb;
        private Thumb? bottomThumb;

        private Thumb? leftTopThumb;
        private Thumb? rightTopThumb;
        private Thumb? rightBottomThumb;
        private Thumb? leftBottomThumb;

        #region Thumb Properties

        private Thumb? LeftThumb
        {
            get => leftThumb;
            set
            {
                if (leftThumb != value)
                {
                    if (leftThumb != null)
                    {
                        leftThumb.DragStarted -= Thumb_DragStarted;
                        leftThumb.DragDelta -= Thumb_DragDelta;
                        leftThumb.DragCompleted -= Thumb_DragCompleted;
                    }

                    leftThumb = value;

                    if (leftThumb != null)
                    {
                        leftThumb.DragStarted += Thumb_DragStarted;
                        leftThumb.DragDelta += Thumb_DragDelta;
                        leftThumb.DragCompleted += Thumb_DragCompleted;
                    }
                }
            }
        }

        private Thumb? TopThumb
        {
            get => topThumb;
            set
            {
                if (topThumb != value)
                {
                    if (topThumb != null)
                    {
                        topThumb.DragStarted -= Thumb_DragStarted;
                        topThumb.DragDelta -= Thumb_DragDelta;
                        topThumb.DragCompleted -= Thumb_DragCompleted;
                    }

                    topThumb = value;

                    if (topThumb != null)
                    {
                        topThumb.DragStarted += Thumb_DragStarted;
                        topThumb.DragDelta += Thumb_DragDelta;
                        topThumb.DragCompleted += Thumb_DragCompleted;
                    }
                }
            }
        }

        private Thumb? RightThumb
        {
            get => rightThumb;
            set
            {
                if (rightThumb != value)
                {
                    if (rightThumb != null)
                    {
                        rightThumb.DragStarted -= Thumb_DragStarted;
                        rightThumb.DragDelta -= Thumb_DragDelta;
                        rightThumb.DragCompleted -= Thumb_DragCompleted;
                    }

                    rightThumb = value;

                    if (rightThumb != null)
                    {
                        rightThumb.DragStarted += Thumb_DragStarted;
                        rightThumb.DragDelta += Thumb_DragDelta;
                        rightThumb.DragCompleted += Thumb_DragCompleted;
                    }
                }
            }
        }

        private Thumb? BottomThumb
        {
            get => bottomThumb;
            set
            {
                if (bottomThumb != value)
                {
                    if (bottomThumb != null)
                    {
                        bottomThumb.DragStarted -= Thumb_DragStarted;
                        bottomThumb.DragDelta -= Thumb_DragDelta;
                        bottomThumb.DragCompleted -= Thumb_DragCompleted;
                    }

                    bottomThumb = value;

                    if (bottomThumb != null)
                    {
                        bottomThumb.DragStarted += Thumb_DragStarted;
                        bottomThumb.DragDelta += Thumb_DragDelta;
                        bottomThumb.DragCompleted += Thumb_DragCompleted;
                    }
                }
            }
        }
        private Thumb? LeftTopThumb
        {
            get => leftTopThumb;
            set
            {
                if (leftTopThumb != value)
                {
                    if (leftTopThumb != null)
                    {
                        leftTopThumb.DragStarted -= Thumb_DragStarted;
                        leftTopThumb.DragDelta -= Thumb_DragDelta;
                        leftTopThumb.DragCompleted -= Thumb_DragCompleted;
                    }

                    leftTopThumb = value;

                    if (leftTopThumb != null)
                    {
                        leftTopThumb.DragStarted += Thumb_DragStarted;
                        leftTopThumb.DragDelta += Thumb_DragDelta;
                        leftTopThumb.DragCompleted += Thumb_DragCompleted;
                    }
                }
            }
        }
        private Thumb? RightTopThumb
        {
            get => rightTopThumb;
            set
            {
                if (rightTopThumb != value)
                {
                    if (rightTopThumb != null)
                    {
                        rightTopThumb.DragStarted -= Thumb_DragStarted;
                        rightTopThumb.DragDelta -= Thumb_DragDelta;
                        rightTopThumb.DragCompleted -= Thumb_DragCompleted;
                    }

                    rightTopThumb = value;

                    if (rightTopThumb != null)
                    {
                        rightTopThumb.DragStarted += Thumb_DragStarted;
                        rightTopThumb.DragDelta += Thumb_DragDelta;
                        rightTopThumb.DragCompleted += Thumb_DragCompleted;
                    }
                }
            }
        }

        private Thumb? RightBottomThumb
        {
            get => rightBottomThumb;
            set
            {
                if (rightBottomThumb != value)
                {
                    if (rightBottomThumb != null)
                    {
                        rightBottomThumb.DragStarted -= Thumb_DragStarted;
                        rightBottomThumb.DragDelta -= Thumb_DragDelta;
                        rightBottomThumb.DragCompleted -= Thumb_DragCompleted;
                    }

                    rightBottomThumb = value;

                    if (rightBottomThumb != null)
                    {
                        rightBottomThumb.DragStarted += Thumb_DragStarted;
                        rightBottomThumb.DragDelta += Thumb_DragDelta;
                        rightBottomThumb.DragCompleted += Thumb_DragCompleted;
                    }
                }
            }
        }

        private Thumb? LeftBottomThumb
        {
            get => leftBottomThumb;
            set
            {
                if (leftBottomThumb != value)
                {
                    if (leftBottomThumb != null)
                    {
                        leftBottomThumb.DragStarted -= Thumb_DragStarted;
                        leftBottomThumb.DragDelta -= Thumb_DragDelta;
                        leftBottomThumb.DragCompleted -= Thumb_DragCompleted;
                    }

                    leftBottomThumb = value;

                    if (leftBottomThumb != null)
                    {
                        leftBottomThumb.DragStarted += Thumb_DragStarted;
                        leftBottomThumb.DragDelta += Thumb_DragDelta;
                        leftBottomThumb.DragCompleted += Thumb_DragCompleted;
                    }
                }
            }
        }

        #endregion Thumb Properties


        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            LeftThumb = GetTemplateChild("LeftThumb") as Thumb;
            TopThumb = GetTemplateChild("TopThumb") as Thumb;
            RightThumb = GetTemplateChild("RightThumb") as Thumb;
            BottomThumb = GetTemplateChild("BottomThumb") as Thumb;
            LeftTopThumb = GetTemplateChild("LeftTopThumb") as Thumb;
            RightTopThumb = GetTemplateChild("RightTopThumb") as Thumb;
            RightBottomThumb = GetTemplateChild("RightBottomThumb") as Thumb;
            LeftBottomThumb = GetTemplateChild("LeftBottomThumb") as Thumb;

            UpdateBorderThickness();
        }

        private void Thumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            e.Handled = true;

            //var mode = GetResizeMode(sender);
            //if (mode.HasValue)
            //{
            //    OnResizing(mode.Value,e.HorizontalOffset,e.VerticalOffset);
            //}
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            e.Handled = true;

            var mode = GetResizeMode(sender);
            if (mode.HasValue)
            {
                OnResizing(mode.Value, e.HorizontalChange, e.VerticalChange);
            }
        }

        private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            e.Handled = true;

            //var mode = GetResizeMode(sender);
            //if (mode.HasValue)
            //{
            //    OnResizing(mode.Value, e.HorizontalChange, e.VerticalChange);
            //}
        }

        private static void OnBorderThicknessPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!object.Equals(e.NewValue, e.OldValue) && d is ResizeControl sender)
            {
                sender.UpdateBorderThickness();
            }
        }

        private void UpdateBorderThickness()
        {
            var border = BorderThickness;

            if (LeftThumb != null) LeftThumb.Width = border.Left;
            if (TopThumb != null) TopThumb.Height = border.Top;
            if (RightThumb != null) RightThumb.Width = border.Right;
            if (BottomThumb != null) BottomThumb.Height = border.Bottom;

            if (LeftTopThumb != null)
            {
                LeftTopThumb.Width = border.Left;
                LeftTopThumb.Height = border.Top;
            }
            if (RightTopThumb != null)
            {
                RightTopThumb.Width = border.Right;
                RightTopThumb.Height = border.Top;
            }
            if (RightBottomThumb != null)
            {
                RightBottomThumb.Width = border.Right;
                RightBottomThumb.Height = border.Bottom;
            }
            if (LeftBottomThumb != null)
            {
                LeftBottomThumb.Width = border.Left;
                LeftBottomThumb.Height = border.Bottom;
            }

        }

        public event EventHandler<ResizeControlResizingEventArgs>? Resizing;

        protected virtual void OnResizing(ResizeControlResizeMode mode, double deltaX, double deltaY)
        {
            if (deltaX == 0 && deltaY == 0) return;
            Resizing?.Invoke(this, new ResizeControlResizingEventArgs(mode, deltaX, deltaY));
        }

        private ResizeControlResizeMode? GetResizeMode(object sender)
        {
            if (sender == LeftThumb) return ResizeControlResizeMode.Left;
            else if (sender == TopThumb) return ResizeControlResizeMode.Top;
            else if (sender == RightThumb) return ResizeControlResizeMode.Right;
            else if (sender == BottomThumb) return ResizeControlResizeMode.Bottom;

            else if (sender == LeftTopThumb) return ResizeControlResizeMode.LeftTop;
            else if (sender == RightTopThumb) return ResizeControlResizeMode.RightTop;
            else if (sender == RightBottomThumb) return ResizeControlResizeMode.RightBottom;
            else if (sender == LeftBottomThumb) return ResizeControlResizeMode.LeftBottom;

            return null;
        }
    }

    public class ResizeControlResizingEventArgs : EventArgs
    {
        public ResizeControlResizingEventArgs(ResizeControlResizeMode mode, double deltaX, double deltaY)
        {
            Mode = mode;
            DeltaX = deltaX;
            DeltaY = deltaY;
        }

        public ResizeControlResizeMode Mode { get; }

        public double DeltaX { get; }

        public double DeltaY { get; }
    }

    public enum ResizeControlResizeMode
    {
        Left,
        Top,
        Right,
        Bottom,

        LeftTop,
        RightTop,
        RightBottom,
        LeftBottom,
    }
}
