using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace HotLyric.Win32.Controls
{
    internal class ColorPickerButton : DropDownButton
    {
        private Border border;
        private SolidColorBrush backgroundBrush;
        private Flyout colorPickerFlyout;
        private ColorPicker colorPicker;

        public ColorPickerButton()
        {
            backgroundBrush = new SolidColorBrush()
            {
                Color = SelectedColor
            };

            border = new Border()
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(4, 0, 0, 4),
                Background = backgroundBrush
            };

            colorPicker = new ColorPicker()
            {
                Color = SelectedColor,
                IsMoreButtonVisible = false,
                IsColorSliderVisible = true,
                IsColorChannelTextInputVisible = false,
                IsHexInputVisible = true,
                IsAlphaEnabled = false,
                ColorSpectrumShape = ColorSpectrumShape.Box,
                IsColorPreviewVisible = false,
                Width = 256,
                Height = 350,
            };

            colorPicker.ColorChanged += ColorPicker_ColorChanged;

            colorPickerFlyout = new Flyout()
            {
                Content = colorPicker,
                Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.BottomEdgeAlignedLeft,
                ShowMode = Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowMode.Transient
            };

            colorPickerFlyout.Closed += ColorPickerFlyout_Closed;

            this.Padding = new Thickness(0, 0, 6, 0);
            this.Flyout = colorPickerFlyout;
            this.Content = border;
        }

        private void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            SelectedColor = colorPicker.Color;
        }

        private void ColorPickerFlyout_Closed(object? sender, object e)
        {
            SelectedColor = colorPicker.Color;
        }

        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register("SelectedColor", typeof(Color), typeof(ColorPickerButton), new PropertyMetadata(Color.FromArgb(255, 255, 255, 255), (s, a) =>
            {
                if (s is ColorPickerButton sender && !Equals(a.NewValue, a.OldValue))
                {
                    var color = (Color)a.NewValue;
                    sender.colorPicker.Color = color;
                    sender.backgroundBrush.Color = color;
                    sender.SelectedColorChanged?.Invoke(sender, EventArgs.Empty);
                }
            }));


        public event EventHandler? SelectedColorChanged;
    }
}
