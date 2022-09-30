using HotLyric.Win32.Controls;
using HotLyric.Win32.Utils;
using HotLyric.Win32.ViewModels;
using Kfstorm.LrcParser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Vanara.PInvoke;

namespace HotLyric.Win32.Views
{
    /// <summary>
    /// LyricWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LyricWindow : Window
    {
        public LyricWindow()
        {
            InitializeComponent();
            this.Icon = WindowHelper.GetDefaultAppIconImage();
        }

        private LyricWindowViewModel VM => (LyricWindowViewModel)DataContext!;

        private HwndSource? hwndSource;
        private bool isDragMoving;

        public HostWindow ParentWindow
        {
            get { return (HostWindow)GetValue(ParentWindowProperty); }
            set { SetValue(ParentWindowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ParentWindow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ParentWindowProperty =
            DependencyProperty.Register("ParentWindow", typeof(HostWindow), typeof(LyricWindow), new PropertyMetadata(null));


        protected override async void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var hwnd = new WindowInteropHelper(this).Handle;
            hwndSource = HwndSource.FromHwnd(hwnd);
            hwndSource.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
            {
                if (msg == (int)User32.WindowMessage.WM_STYLECHANGING && (long)wParam == (long)User32.WindowLongFlags.GWL_EXSTYLE)
                {
                    var styleStruct = (NativeMethods.STYLESTRUCT?)Marshal.PtrToStructure(lParam, typeof(NativeMethods.STYLESTRUCT));
                    if (styleStruct.HasValue)
                    {
                        var styleStruct2 = styleStruct.Value;
                        styleStruct2.styleNew |= (int)User32.WindowStylesEx.WS_EX_LAYERED;
                        Marshal.StructureToPtr(styleStruct2, lParam, false);
                        handled = true;
                    }
                }
                return IntPtr.Zero;
            });

            await this.SetToolWindowStyle(true);
            await this.SetNoActivateStyle(true);
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            SessionComboBox.IsDropDownOpen = false;

            base.OnMouseLeftButtonDown(e);

            isDragMoving = true;
            ParentWindow?.DragMove();
            isDragMoving = false;

            ParentWindow?.SaveBounds();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            e.Cancel = (ParentWindow?.ApplicationExiting == false);
        }

        private async void SessionComboBox_DropDownOpened(object sender, EventArgs e)
        {
            var hwndSource = await ComboBoxEx.GetComboBoxPopupHwndSourceAsync(sender as ComboBox);
            var root = hwndSource?.RootVisual as FrameworkElement;

            if (root != null)
            {
                root.LostFocus += Root_LostFocus;

                void Root_LostFocus(object _, RoutedEventArgs e)
                {
                    if (Mouse.DirectlyOver is Visual visual)
                    {
                        var hwnd = HwndSource.FromVisual(visual);
                        if (hwnd?.RootVisual != this) return;
                    }

                    root.LostFocus -= Root_LostFocus;
                    ((ComboBox)sender).Dispatcher.BeginInvoke(
                        DispatcherPriority.Background,
                        new Action(() => ((ComboBox)sender).IsDropDownOpen = false));
                }
            }
        }

        private void SessionComboBox_DropDownClosed(object sender, EventArgs e)
        {
            VM.IsBackgroundTransientVisible = Mouse.DirectlyOver != null;
        }

        private async void SessionComboBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            await this.SetNoActivateStyle(false);
            Activate();
            await Task.Delay(10);
            ((ComboBox)sender).Focus();
            ((ComboBox)sender).IsDropDownOpen = true;
        }

        private void ResizeControl_Resizing(object sender, ResizeControlResizingEventArgs e)
        {
            var xOffset = 0d;
            var yOffset = 0d;
            var widthOffset = 0d;
            var heightOffset = 0d;

            if (e.Mode == ResizeControlResizeMode.Left
                || e.Mode == ResizeControlResizeMode.LeftTop
                || e.Mode == ResizeControlResizeMode.LeftBottom)
            {
                xOffset = e.DeltaX;
                widthOffset = -e.DeltaX;
            }
            if (e.Mode == ResizeControlResizeMode.Top
                || e.Mode == ResizeControlResizeMode.LeftTop
                || e.Mode == ResizeControlResizeMode.RightTop)
            {
                yOffset = e.DeltaY;
                heightOffset = -e.DeltaY;
            }
            if (e.Mode == ResizeControlResizeMode.Right
                || e.Mode == ResizeControlResizeMode.RightTop
                || e.Mode == ResizeControlResizeMode.RightBottom)
            {
                widthOffset = e.DeltaX;
            }
            if (e.Mode == ResizeControlResizeMode.Bottom
                || e.Mode == ResizeControlResizeMode.LeftBottom
                || e.Mode == ResizeControlResizeMode.RightBottom)
            {
                heightOffset = e.DeltaY;
            }


            var width = ParentWindow.Width + widthOffset;
            var height = ParentWindow.Height + heightOffset;
            var x = ParentWindow.Left + xOffset;
            var y = ParentWindow.Top + yOffset;

            ParentWindow.SetBounds(x, y, width, height);
            InvalidateMeasure();
            UpdateLayout();
        }

        private void LockButton_Click(object sender, RoutedEventArgs e)
        {
            App.Current.NotifyIcon?.ToggleWindowTransparent();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            VM.IsMinimized = true;
        }

        private void OpenSessionComboBoxButton_Click(object sender, RoutedEventArgs e)
        {
            SessionComboBox.Focus();
            SessionComboBox.IsDropDownOpen = true;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.Instance.SettingsWindowViewModel.ShowSettingsWindow();
        }
    }
}
