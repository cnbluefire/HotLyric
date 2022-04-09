using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace HotLyric.Win32.Controls
{
    public class AnimationContentControl : ContentControl
    {
        static AnimationContentControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AnimationContentControl), new FrameworkPropertyMetadata(typeof(AnimationContentControl)));
        }

        public bool IsChildVisible
        {
            get { return (bool)GetValue(IsChildVisibleProperty); }
            set { SetValue(IsChildVisibleProperty, value); }
        }

        public static readonly DependencyProperty IsChildVisibleProperty =
            DependencyProperty.Register("IsChildVisible", typeof(bool), typeof(AnimationContentControl), new PropertyMetadata(true, (s, a) =>
            {
                if (s is AnimationContentControl sender)
                {
                    sender.UpdateChildVisibleState();
                }
            }));

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateChildVisibleState();
        }

        private void UpdateChildVisibleState()
        {
            if (IsChildVisible)
            {
                VisualStateManager.GoToState(this, "ChildVisible", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "ChildCollapsed", true);
            }
        }

    }
}
