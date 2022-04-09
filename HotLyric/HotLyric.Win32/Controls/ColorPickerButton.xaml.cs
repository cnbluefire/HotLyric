using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HotLyric.Win32.Controls
{
    /// <summary>
    /// ColorPickerButton.xaml 的交互逻辑
    /// </summary>
    public partial class ColorPickerButton : UserControl
    {
        public ColorPickerButton()
        {
            InitializeComponent();
        }

        public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Color.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Color), typeof(ColorPickerButton), new PropertyMetadata(Colors.Black, (s, a) =>
            {
                if (!object.Equals(a.NewValue, a.OldValue) && s is ColorPickerButton sender)
                {
                    sender.UpdateColor();
                }
            }));

        private void UpdateColor()
        {
            FillColorBorder.Background = new SolidColorBrush(Color);
            ColorChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Flyout_Opened(object sender, object e)
        {
            InnterColorPicker.Confirmed -= InnterColorPicker_Confirmed;
            InnterColorPicker.Confirmed += InnterColorPicker_Confirmed;

            InnterColorPicker.Canceled -= InnterColorPicker_Canceled;
            InnterColorPicker.Canceled += InnterColorPicker_Canceled;
        }


        private void Flyout_Closed(object sender, object e)
        {
            InnterColorPicker.Canceled -= InnterColorPicker_Canceled;
            InnterColorPicker.Confirmed -= InnterColorPicker_Confirmed;
        }

        private void Flyout_Opening(object sender, object e)
        {
            InnterColorPicker.SelectedBrush = new SolidColorBrush(Color);
        }

        private void InnterColorPicker_Confirmed(object? sender, HandyControl.Data.FunctionEventArgs<Color> e)
        {
            Flyout.Hide();
            Color = e.Info;
        }

        private void InnterColorPicker_Canceled(object? sender, EventArgs e)
        {
            Flyout.Hide();
        }

        public event EventHandler? ColorChanged;
    }
}
