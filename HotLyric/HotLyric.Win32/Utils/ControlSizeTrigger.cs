using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace HotLyric.Win32.Utils
{
    internal class ControlSizeTrigger : StateTriggerBase
    {
        private bool isActive = false;

        public FrameworkElement Control
        {
            get { return (FrameworkElement)GetValue(ControlProperty); }
            set { SetValue(ControlProperty, value); }
        }

        public static readonly DependencyProperty ControlProperty =
            DependencyProperty.Register("Control", typeof(FrameworkElement), typeof(ControlSizeTrigger), new PropertyMetadata(null, (s, a) =>
            {
                if (s is ControlSizeTrigger sender && a.NewValue != a.OldValue)
                {
                    if (a.OldValue is FrameworkElement oldValue)
                    {
                        oldValue.SizeChanged -= sender.Control_SizeChanged;
                    }

                    if (a.NewValue is FrameworkElement newValue)
                    {
                        newValue.SizeChanged += sender.Control_SizeChanged;
                    }

                    sender.UpdateState();
                }
            }));

        public double MinWidth
        {
            get { return (double)GetValue(MinWidthProperty); }
            set { SetValue(MinWidthProperty, value); }
        }

        public static readonly DependencyProperty MinWidthProperty =
            DependencyProperty.Register("MinWidth", typeof(double), typeof(ControlSizeTrigger), new PropertyMetadata(double.PositiveInfinity, (s, a) =>
            {
                if (s is ControlSizeTrigger sender && !Equals(a.NewValue, a.OldValue))
                {
                    sender.UpdateState();
                }
            }));


        public double MinHeight
        {
            get { return (double)GetValue(MinHeightProperty); }
            set { SetValue(MinHeightProperty, value); }
        }

        public static readonly DependencyProperty MinHeightProperty =
            DependencyProperty.Register("MinHeight", typeof(double), typeof(ControlSizeTrigger), new PropertyMetadata(double.PositiveInfinity, (s, a) =>
            {
                if (s is ControlSizeTrigger sender && !Equals(a.NewValue, a.OldValue))
                {
                    sender.UpdateState();
                }
            }));


        private void Control_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateState();
        }


        private void UpdateState()
        {
            var control = Control;
            var width = MinWidth;
            var height = MinHeight;

            bool flag = false;

            if (control != null)
            {
                if (control.ActualWidth > width || control.ActualHeight > height)
                {
                    flag = true;
                }
            }

            if (isActive != flag)
            {
                isActive = flag;
                SetActive(flag);
            }
        }
    }
}
