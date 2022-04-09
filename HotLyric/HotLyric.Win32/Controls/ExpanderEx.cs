using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace HotLyric.Win32.Controls
{
    public class ExpanderEx : Expander
    {
        private static readonly DependencyPropertyKey TemplateSettingsPropertyKey;
        public static readonly DependencyProperty TemplateSettingsProperty;

        static ExpanderEx()
        {
            TemplateSettingsPropertyKey = DependencyProperty.RegisterReadOnly("TemplateSettings", typeof(ExpanderExTemplateSettings), typeof(ExpanderEx), new PropertyMetadata(null));
            TemplateSettingsProperty = TemplateSettingsPropertyKey.DependencyProperty;

            DefaultStyleKeyProperty.OverrideMetadata(typeof(ExpanderEx), new FrameworkPropertyMetadata(typeof(ExpanderEx)));
        }

        private Storyboard? curSb;
        private VisualStateGroup? exExpansionStates;
        private CancellationTokenSource? sbCts;
        private Border? ExpanderContent;

        private VisualStateGroup? ExExpansionStates
        {
            get => exExpansionStates;
            set
            {
                if (exExpansionStates != value)
                {
                    exExpansionStates = value;
                }
            }
        }

        public ExpanderEx()
        {
            TemplateSettings = new ExpanderExTemplateSettings(this);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            ExExpansionStates = GetTemplateChild("ExExpansionStates") as VisualStateGroup;
            ExpanderContent = GetTemplateChild("ExpanderContent") as Border;

            UpdateExpandStates(false);
        }

        protected override void OnExpanded()
        {
            base.OnExpanded();
            UpdateExpandStates(true);
        }

        protected override void OnCollapsed()
        {
            base.OnCollapsed();
            UpdateExpandStates(true);
        }

        private void UpdateExpandStates(bool useTransitions)
        {
            if (IsExpanded)
            {
                VisualStateManager.GoToState(this, "ExExpandDown", useTransitions);
            }
            else
            {
                VisualStateManager.GoToState(this, "ExCollapseUp", useTransitions);
            }

            BeginAnimation(useTransitions);
        }

        private void BeginAnimation(bool useTransitions)
        {
            sbCts?.Cancel();
            sbCts = new CancellationTokenSource();
            var token = sbCts.Token;

            bool isExpand = IsExpanded;

            if (useTransitions && isExpand && ExpanderContent != null)
            {
                ExpanderContent.Opacity = 0;
            }

            _ = Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() =>
            {
                if (token.IsCancellationRequested) return;

                if (isExpand)
                {
                    this.UpdateLayout();
                }

                curSb?.SkipToFill();
                curSb = null;

                var sb = new Storyboard();

                var duration = isExpand ? TimeSpan.FromSeconds(0.333) : TimeSpan.FromSeconds(0.167);
                var an = new DoubleAnimationUsingKeyFrames();
                an.Duration = new Duration(duration);

                Storyboard.SetTarget(an, TemplateSettings);
                Storyboard.SetTargetProperty(an, new PropertyPath(ExpanderExTemplateSettings.PercentageProperty));

                if (isExpand)
                {
                    an.KeyFrames.Add(new DiscreteDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
                    an.KeyFrames.Add(new SplineDoubleKeyFrame(0, KeyTime.FromTimeSpan(duration), new KeySpline(0, 0, 0, 1)));
                }
                else
                {
                    an.KeyFrames.Add(new DiscreteDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
                    an.KeyFrames.Add(new SplineDoubleKeyFrame(1, KeyTime.FromTimeSpan(duration), new KeySpline(1, 1, 0, 1)));
                }

                sb.Children.Add(an);

                sb.Freeze();
                sb.Begin();
                if (!useTransitions)
                {
                    sb.SkipToFill();
                }
                
                if (ExpanderContent != null)
                {
                    ExpanderContent.Opacity = 1;
                }

                curSb = sb;
            }));
        }

        public ExpanderExTemplateSettings TemplateSettings
        {
            get { return (ExpanderExTemplateSettings)GetValue(TemplateSettingsProperty); }
            private set { SetValue(TemplateSettingsPropertyKey, value); }
        }
    }

    public class ExpanderExTemplateSettings : DependencyObject
    {
        internal ExpanderExTemplateSettings(ExpanderEx expander)
        {
            this.expander = expander;
            expander.Unloaded += Expander_Unloaded;
        }

        private readonly ExpanderEx expander;
        private TranslateTransform? contentTrans;
        private FrameworkElement? expanderContent;

        public double Percentage
        {
            get { return (double)GetValue(PercentageProperty); }
            set { SetValue(PercentageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Percentage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PercentageProperty =
            DependencyProperty.Register("Percentage", typeof(double), typeof(ExpanderExTemplateSettings), new PropertyMetadata(0d, (s, a) =>
            {
                if (!object.Equals(a.NewValue, a.OldValue) && s is ExpanderExTemplateSettings sender)
                {
                    sender.UpdatePercentage();
                }
            }));

        public void UpdatePercentage()
        {
            var trans = GetContentTranslate();
            var ele = GetExpandSite();

            if (ele == null || trans == null || ele.ActualHeight == 0) return;

            trans.Y = -ele.ActualHeight * Math.Clamp(Percentage, 0, 1);
        }

        private TranslateTransform? GetContentTranslate()
        {
            if (contentTrans == null)
            {
                if (VisualTreeHelper.GetChildrenCount(expander) > 0)
                {
                    var child = VisualTreeHelper.GetChild(expander, 0) as FrameworkElement;
                    contentTrans = child?.FindName("ExpanderContentTransform") as TranslateTransform;
                }
            }

            return contentTrans;
        }

        private FrameworkElement? GetExpandSite()
        {
            if (expanderContent == null)
            {
                if (VisualTreeHelper.GetChildrenCount(expander) > 0)
                {
                    var child = VisualTreeHelper.GetChild(expander, 0) as FrameworkElement;
                    expanderContent = child?.FindName("ExpanderContent") as FrameworkElement;
                    if(expanderContent != null)
                    {
                        expanderContent.SizeChanged += ExpanderContent_SizeChanged;
                    }
                }
            }

            return expanderContent;
        }

        private void Expander_Unloaded(object sender, RoutedEventArgs e)
        {
            if (expanderContent != null)
            {
                expanderContent.SizeChanged -= ExpanderContent_SizeChanged;
            }

            contentTrans = null;
            expanderContent = null;
        }

        private void ExpanderContent_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePercentage();
        }
    }
}
