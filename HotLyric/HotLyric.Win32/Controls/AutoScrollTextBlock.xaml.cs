using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HotLyric.Win32.Controls
{
    /// <summary>
    /// AutoScrollTextBlock.xaml 的交互逻辑
    /// </summary>
    public partial class AutoScrollTextBlock : UserControl
    {
        public AutoScrollTextBlock()
        {
            InitializeComponent();
            this.SizeChanged += AutoScrollTextBlock_SizeChanged;
            ContentBorder.SizeChanged += ContentBorder_SizeChanged;
            this.IsVisibleChanged += AutoScrollTextBlock_IsVisibleChanged;
        }

        private Storyboard? curSb;

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Text.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(AutoScrollTextBlock), new PropertyMetadata(""));



        public double Speed
        {
            get { return (double)GetValue(SpeedProperty); }
            set { SetValue(SpeedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Speed.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpeedProperty =
            DependencyProperty.Register("Speed", typeof(double), typeof(AutoScrollTextBlock), new PropertyMetadata(20d, (s, a) =>
            {
                if (!object.Equals(a.NewValue, a.OldValue) && s is AutoScrollTextBlock sender)
                {
                    sender.UpdateOffset();
                }
            }));


        private void ContentBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateOffset();
        }

        private void AutoScrollTextBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Container.Width = e.NewSize.Width;
            Container.Height = e.NewSize.Height;
            Container.UpdateLayout();

            MaskBrush.EndPoint = new Point(e.NewSize.Width, 0);

            OpacityMaskStop1.Offset = 4 / e.NewSize.Width;
            OpacityMaskStop2.Offset = 1 - 4 / e.NewSize.Width;

            UpdateOffset();
        }


        private void AutoScrollTextBlock_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateOffset();
        }

        private void UpdateOffset()
        {
            curSb?.Stop();
            curSb = null;

            var top = (this.ActualHeight - ContentBorder.ActualHeight) / 2;
            Canvas.SetTop(ContentBorder, top);
            Canvas.SetTop(ContentBorder2, top);

            if (!IsVisible || ContentBorder.ActualWidth < this.ActualWidth)
            {
                ContentBorder2.Visibility = Visibility.Collapsed;
                Canvas.SetLeft(ContentBorder, 0);
                Container.Opacity = 1;
            }
            //else if (this.ActualWidth < 80)
            //{
            //    Canvas.SetLeft(ContentBorder, 0);
            //    Container.Opacity = 0;
            //}
            else
            {
                Container.Opacity = 1;
                ContentBorder2.Visibility = Visibility.Visible;

                var textWidth = ContentBorder.ActualWidth;
                var viewportWidth = Container.ActualWidth;
                var padding = 12d;

                var speed = Speed;
                if (speed <= 0.01) speed = 0.1;

                var duration = TimeSpan.FromSeconds(textWidth / speed);
                var paddingDuration = TimeSpan.FromSeconds(padding / speed);
                var viewportDuration = TimeSpan.FromSeconds(viewportWidth / speed);

                var sb = new Storyboard();

                var an1 = new DoubleAnimationUsingKeyFrames();
                an1.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
                an1.KeyFrames.Add(new LinearDoubleKeyFrame(-textWidth, KeyTime.FromTimeSpan(duration)));
                an1.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(duration + paddingDuration)));

                Storyboard.SetTarget(an1, ContentBorder);
                Storyboard.SetTargetProperty(an1, new PropertyPath(Canvas.LeftProperty));

                an1.Duration = duration + paddingDuration;
                sb.Children.Add(an1);

                var an2 = new DoubleAnimationUsingKeyFrames();
                an2.KeyFrames.Add(new DiscreteDoubleKeyFrame(viewportWidth, KeyTime.FromTimeSpan(TimeSpan.Zero)));
                an2.KeyFrames.Add(new DiscreteDoubleKeyFrame(viewportWidth, KeyTime.FromTimeSpan(duration - viewportDuration + paddingDuration)));
                an2.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(duration + paddingDuration)));

                Storyboard.SetTarget(an2, ContentBorder2);
                Storyboard.SetTargetProperty(an2, new PropertyPath(Canvas.LeftProperty));

                an2.Duration = duration + paddingDuration;
                sb.Children.Add(an2);

                sb.RepeatBehavior = RepeatBehavior.Forever;
                Timeline.SetDesiredFrameRate(sb, 30);
                sb.Freeze();

                sb.Begin();
                curSb = sb;
            }
        }
    }
}
