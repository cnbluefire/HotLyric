using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;

namespace HotLyric.Win32.Base
{
    public class WindowAcrylicController : DependencyObject
    {
        private static IReadOnlyList<DependencyProperty> acrylicProperties = new[]
        {
            AcrylicBrush.AlwaysUseFallbackProperty,
            AcrylicBrush.FallbackColorProperty,
            AcrylicBrush.OpacityProperty,
            AcrylicBrush.TintColorProperty,
            AcrylicBrush.TintLuminosityOpacityProperty,
            AcrylicBrush.TintOpacityProperty
        };

        private WindowAcrylicContext? context;
        private Dictionary<DependencyProperty, long> acrylicPropertyEventTokens = new Dictionary<DependencyProperty, long>();

        public Window? Window
        {
            get { return (Window)GetValue(WindowProperty); }
            set { SetValue(WindowProperty, value); }
        }

        public static readonly DependencyProperty WindowProperty =
            DependencyProperty.Register("Window", typeof(Window), typeof(WindowAcrylicController), new PropertyMetadata(null, (s, a) =>
            {
                if (s is WindowAcrylicController sender && !Equals(a.NewValue, a.OldValue))
                {
                    if (a.NewValue is Window window)
                    {
                        if (sender.context == null)
                        {
                            sender.context = new WindowAcrylicContext(sender)
                            {
                                Opacity = sender.VisualOpacity,
                                Visible = sender.Visible
                            };
                        }
                        sender.context.Window = window;
                        UpdateProperties(sender, null);
                    }
                    else
                    {
                        if (sender.context != null)
                        {
                            sender.context.Window = null;
                            sender.context.Dispose();
                            sender.context = null;
                        }
                    }
                }
            }));

        public AcrylicBrush? AcrylicBrush
        {
            get { return (AcrylicBrush)GetValue(AcrylicBrushProperty); }
            set { SetValue(AcrylicBrushProperty, value); }
        }

        public static readonly DependencyProperty AcrylicBrushProperty =
            DependencyProperty.Register("AcrylicBrush", typeof(AcrylicBrush), typeof(WindowAcrylicController), new PropertyMetadata(null, (s, a) =>
            {
                if (s is WindowAcrylicController sender && !Equals(a.NewValue, a.OldValue))
                {
                    if (a.OldValue is AcrylicBrush oldValue)
                    {
                        sender.RemoveAcrylicBrushEventHandlers(oldValue);
                    }

                    if (a.NewValue is AcrylicBrush newValue)
                    {
                        sender.AddAcrylicBrushEventHandlers(newValue);
                    }

                    UpdateAcrylicProperties(s, a);
                }
            }));

        public double ShadowOpacity
        {
            get { return (double)GetValue(ShadowOpacityProperty); }
            set { SetValue(ShadowOpacityProperty, value); }
        }

        public static readonly DependencyProperty ShadowOpacityProperty =
            DependencyProperty.Register("ShadowOpacity", typeof(double), typeof(WindowAcrylicController), new PropertyMetadata(0.8d, UpdateShadowProperties));


        public double ShadowOffsetX
        {
            get { return (double)GetValue(ShadowOffsetXProperty); }
            set { SetValue(ShadowOffsetXProperty, value); }
        }

        public static readonly DependencyProperty ShadowOffsetXProperty =
            DependencyProperty.Register("ShadowOffsetX", typeof(double), typeof(WindowAcrylicController), new PropertyMetadata(0d, UpdateShadowProperties));

        public double ShadowOffsetY
        {
            get { return (double)GetValue(ShadowOffsetYProperty); }
            set { SetValue(ShadowOffsetYProperty, value); }
        }

        public static readonly DependencyProperty ShadowOffsetYProperty =
            DependencyProperty.Register("ShadowOffsetY", typeof(double), typeof(WindowAcrylicController), new PropertyMetadata(1d, UpdateShadowProperties));

        public Color ShadowColor
        {
            get { return (Color)GetValue(ShadowColorProperty); }
            set { SetValue(ShadowColorProperty, value); }
        }

        public static readonly DependencyProperty ShadowColorProperty =
            DependencyProperty.Register("ShadowColor", typeof(Color), typeof(WindowAcrylicController), new PropertyMetadata(Color.FromArgb(255, 0, 0, 0), UpdateShadowProperties));



        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(WindowAcrylicController), new PropertyMetadata(new CornerRadius(0, 0, 0, 0), UpdateProperties));



        public double VisualOpacity
        {
            get { return (double)GetValue(VisualOpacityProperty); }
            set { SetValue(VisualOpacityProperty, value); }
        }

        public static readonly DependencyProperty VisualOpacityProperty =
            DependencyProperty.Register("VisualOpacity", typeof(double), typeof(WindowAcrylicController), new PropertyMetadata(1d, (s, a) =>
            {
                if (s is WindowAcrylicController sender && !Equals(a.NewValue, a.OldValue))
                {
                    if (sender.context != null)
                    {
                        sender.context.Opacity = (double)a.NewValue;
                    }
                }
            }));



        public bool Visible
        {
            get { return (bool)GetValue(VisibleProperty); }
            set { SetValue(VisibleProperty, value); }
        }

        public static readonly DependencyProperty VisibleProperty =
            DependencyProperty.Register("Visible", typeof(bool), typeof(WindowAcrylicController), new PropertyMetadata(true, (s, a) =>
            {
                if (s is WindowAcrylicController sender && !Equals(a.NewValue, a.OldValue))
                {
                    if (sender.context != null)
                    {
                        sender.context.Visible = a.NewValue is true;
                    }
                }
            }));




        private static void UpdateShadowProperties(DependencyObject sender, DependencyPropertyChangedEventArgs? args)
        {
            if (sender is WindowAcrylicController controller)
            {
                controller.context?.UpdateShadowProperties();
            }
        }


        private static void UpdateAcrylicProperties(DependencyObject sender, DependencyPropertyChangedEventArgs? args)
        {
            if (sender is WindowAcrylicController controller)
            {
                controller.context?.UpdateAcrylicProperties();
            }
        }

        private static void UpdateProperties(DependencyObject d, DependencyPropertyChangedEventArgs? e)
        {
            UpdateShadowProperties(d, null);
            UpdateAcrylicProperties(d, null);
        }

        private void AddAcrylicBrushEventHandlers(AcrylicBrush acrylicBrush)
        {
            foreach (var prop in acrylicProperties)
            {
                acrylicPropertyEventTokens.Add(prop, acrylicBrush.RegisterPropertyChangedCallback(prop, AcrylicBrushPropertyChanged));
            }
        }

        private void RemoveAcrylicBrushEventHandlers(AcrylicBrush acrylicBrush)
        {
            foreach (var (prop, token) in acrylicPropertyEventTokens)
            {
                acrylicBrush.UnregisterPropertyChangedCallback(prop, token);
            }
            acrylicPropertyEventTokens.Clear();
        }

        private void AcrylicBrushPropertyChanged(DependencyObject sender, DependencyProperty dp)
        {
            UpdateAcrylicProperties(this, null);
        }

    }
}
