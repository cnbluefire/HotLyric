using HotLyric.Win32.BackgroundHelpers;
using HotLyric.Win32.Utils;
using HotLyric.Win32.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
using Windows.UI.Composition;

namespace HotLyric.Win32.Views
{
    /// <summary>
    /// HostWindow.xaml 的交互逻辑
    /// </summary>
    public partial class HostWindow : Window, IHostWindow
    {
        public HostWindow()
        {
            InitializeComponent();

            if (WindowBoundsHelper.TryGetWindowBounds("lyric", out var x, out var y, out var width, out var height))
            {
                this.Width = width;
                this.Height = height;
                this.Left = x;
                this.Top = y;
            }
            else
            {
                initWindowPosition = true;
            }

            topmostHelper = new WindowTopmostHelper(this)
            {
                HideWhenFullScreenAppOpen = HideWhenFullScreenAppOpen
            };
            this.Icon = WindowHelper.GetDefaultAppIconImage();

            this.IsVisibleChanged += HostWindow_IsVisibleChanged;
        }


        private LyricWindowViewModel VM => (LyricWindowViewModel)DataContext!;

        private bool initWindowPosition;
        private bool isClosing;
        private Windows.UI.Composition.ContainerVisual? rootVisual;
        private Compositor? compositor;
        private AcrylicVisualProvider? acrylicVisualProvider;
        private HwndSource? hwndSource;
        private WindowTopmostHelper topmostHelper;

        public bool ApplicationExiting { get; set; }

        public Window? ChildWindow => LyricWindowHostControl?.Window;

        #region Init Properties

        protected override async void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            Opacity = 0;

            var hwnd = new WindowInteropHelper(this).Handle;

            hwndSource = HwndSource.FromHwnd(hwnd);
            hwndSource.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
            {
                if (msg == (int)User32.WindowMessage.WM_COPYDATA)
                {
                    var message = WindowHelper.ProcessCopyDataMessage(lParam);
                    CommandLineArgsHelper.ProcessCopyDataMessage(message);
                }

                return IntPtr.Zero;
            });

            await this.SetToolWindowStyle(true);
            await this.SetNoActivateStyle(true);

            var dpi = VisualTreeHelper.GetDpi(this);

            rootVisual = CompositionThread.Instance.CreateRootVisual(hwnd, false);
            compositor = rootVisual.Compositor;

            acrylicVisualProvider = await AcrylicVisualProvider.CreateAsync(compositor, new System.Windows.Size(Width, Height), new Size(8, 8));
            acrylicVisualProvider.IsShadowVisible = IsShadowVisible;
            acrylicVisualProvider.ShadowBlurRadius = 12;
            acrylicVisualProvider.ShadowOpacity = 1;
            acrylicVisualProvider.ShadowColor = Windows.UI.Color.FromArgb((int)(255 * 0.8), 0, 0, 0);
            acrylicVisualProvider.ShadowOffset = new System.Numerics.Vector3(0, 3f, 0);

            acrylicVisualProvider.UseFallback = false;
            acrylicVisualProvider.ScaleRatio = dpi.PixelsPerDip;
            acrylicVisualProvider.TintColor = Windows.UI.Color.FromArgb(255, 44, 44, 44);
            acrylicVisualProvider.TintOpacity = 0;
            acrylicVisualProvider.TintLuminosityOpacity = 0.65;

            acrylicVisualProvider.Offset = new Point(12, 12);
            acrylicVisualProvider.Size = new Size(this.Width - 24, this.Height - 24);
            acrylicVisualProvider.Opacity = 0;

            var easing = compositor.CreateLinearEasingFunction();

            var imp = compositor.CreateImplicitAnimationCollection();
            var an = compositor.CreateScalarKeyFrameAnimation();
            an.InsertExpressionKeyFrame(1f, "this.FinalValue", easing);
            an.Duration = (TimeSpan)App.Current.Resources["DefaultAnimationTime"];
            an.Target = "Opacity";
            imp["Opacity"] = an;
            acrylicVisualProvider.Visual.ImplicitAnimations = imp;

            rootVisual.Children.InsertAtTop(acrylicVisualProvider.Visual);

            UpdateAcrylicBrush();

            await Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                var binding = new Binding("IsBackgroundVisible");
                this.SetBinding(IsBackgroundVisibleProperty, binding);

                var binding2 = new Binding("ActualMinimized");
                this.SetBinding(IsMinimizedProperty, binding2);

                var binding3 = new Binding("IsTransparent");
                this.SetBinding(IsTransparentProperty, binding3);

                var binding4 = new Binding("ShowShadow");
                this.SetBinding(IsShadowVisibleProperty, binding4);

                var binding5 = new Binding("LyricTheme.BackgroundBrush");
                this.SetBinding(AcrylicBackgroundProperty, binding5);

                var binding6 = new Binding("SettingViewModel.HideWhenFullScreenAppOpen");
                this.SetBinding(HideWhenFullScreenAppOpenProperty, binding6);

                Opacity = 1;
                if (ChildWindow != null)
                {
                    ChildWindow.Opacity = 1;
                }

                this.SizeChanged += HostWindow_SizeChanged;

                VM.BackgroundHelper = new Utils.WindowBackgrounds.WindowBackgroundHelper(this)
                {
                    IsTransparent = IsTransparent
                };
                //UpdateMouseEvent();

                CommandLineArgsHelper.ActivateMainInstanceEventReceived += CommandLineArgsHelper_ActivateMainInstanceEventReceived;

                SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
            }));
        }

        private void CommandLineArgsHelper_ActivateMainInstanceEventReceived(object? sender, EventArgs e)
        {
            ViewModelLocator.Instance.SettingsWindowViewModel.ActivateInstance();
        }

        #endregion Init Properties


        #region Window Events

        private void HostWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is true && initWindowPosition)
            {
                initWindowPosition = false;

                WindowBoundsHelper.ResetWindowBounds(new WindowInteropHelper(this).Handle);
                Dispatcher.BeginInvoke(new Action(SaveBounds), DispatcherPriority.Background);
            }
        }

        private void HostWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateAcrylicSize(ActualWidth, ActualHeight);
        }

        private void UpdateAcrylicSize(double width, double height)
        {
            if (acrylicVisualProvider != null)
            {
                acrylicVisualProvider.Size = new Size(width - 24, height - 24);
            }
        }

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            base.OnDpiChanged(oldDpi, newDpi);

            if (acrylicVisualProvider != null)
            {
                acrylicVisualProvider.ScaleRatio = newDpi.PixelsPerDip;
            }

            SaveBounds();
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);

            Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                if (!VM.IsBackgroundTransientVisible)
                {
                    VM.ShowBackgroundTransient(TimeSpan.FromSeconds(3));
                }
            }));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!ApplicationExiting)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                isClosing = true;
                SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;

                VM.BackgroundHelper?.Dispose();
                VM.BackgroundHelper = null;

                topmostHelper?.Dispose();
                topmostHelper = null!;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            LyricWindowHostControl?.Dispose();
        }


        private async void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
        {
            await Task.Delay(1000);
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                if (isClosing
                  || !IsVisible
                  || hwndSource == null
                  || !ViewModelLocator.Instance.SettingsWindowViewModel.AutoResetWindowPos) return;

                if (User32.GetWindowRect(hwndSource.Handle, out var _windowRect))
                {
                    var windowRect = (System.Drawing.Rectangle)_windowRect;
                    var minSize = 24;

                    var screenRects = System.Windows.Forms.Screen.AllScreens.Select(c => c.Bounds).ToArray();

                    var flag = false;

                    foreach (var rect in screenRects)
                    {
                        var tmp = windowRect;
                        tmp.Intersect(rect);

                        if (tmp.Width >= minSize && tmp.Height >= minSize)
                        {
                            flag = true;
                            break;
                        }
                    }

                    if (!flag)
                    {
                        WindowBoundsHelper.ResetWindowBounds(hwndSource.Handle);
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            SaveBounds();
                            VM.ShowBackgroundTransient(TimeSpan.FromSeconds(2));
                        }), DispatcherPriority.Background);
                    }
                }
            }), DispatcherPriority.Background);

        }


        #endregion Window Events


        #region Dependency Properties

        public bool IsBackgroundVisible
        {
            get { return (bool)GetValue(IsBackgroundVisibleProperty); }
            set { SetValue(IsBackgroundVisibleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsBackgroundVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsBackgroundVisibleProperty =
            DependencyProperty.Register("IsBackgroundVisible", typeof(bool), typeof(HostWindow), new PropertyMetadata(false, (s, a) =>
            {
                if (!object.Equals(a.NewValue, a.OldValue) && s is HostWindow sender && sender.acrylicVisualProvider != null)
                {
                    sender.acrylicVisualProvider.Opacity = a.NewValue is true ? 1 : 0;
                }
            }));



        public bool IsMinimized
        {
            get { return (bool)GetValue(IsMinimizedProperty); }
            set { SetValue(IsMinimizedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsMinimized.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsMinimizedProperty =
            DependencyProperty.Register("IsMinimized", typeof(bool), typeof(HostWindow), new PropertyMetadata(false, (s, a) =>
            {
                if (!object.Equals(a.NewValue, a.OldValue) && s is HostWindow sender)
                {
                    if (a.NewValue is true) sender.Hide();
                    else sender.Show();

                    //sender.UpdateMouseEvent();
                }
            }));



        public bool IsTransparent
        {
            get { return (bool)GetValue(IsTransparentProperty); }
            set { SetValue(IsTransparentProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsTransparent.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsTransparentProperty =
            DependencyProperty.Register("IsTransparent", typeof(bool), typeof(HostWindow), new PropertyMetadata(false, async (s, a) =>
            {
                if (!object.Equals(a.NewValue, a.OldValue) && s is HostWindow sender)
                {
                    if (sender.VM.BackgroundHelper != null)
                    {
                        sender.VM.BackgroundHelper.IsTransparent = a.NewValue is true;
                    }
                    await sender.SetTransparentAsync(a.NewValue is true);
                    //sender.UpdateMouseEvent();
                }
            }));



        public bool IsShadowVisible
        {
            get { return (bool)GetValue(IsShadowVisibleProperty); }
            set { SetValue(IsShadowVisibleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsShadowVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsShadowVisibleProperty =
            DependencyProperty.Register("IsShadowVisible", typeof(bool), typeof(HostWindow), new PropertyMetadata(true, (s, a) =>
            {
                if (!object.Equals(a.NewValue, a.OldValue) && s is HostWindow sender)
                {
                    if (sender.acrylicVisualProvider != null)
                    {
                        sender.acrylicVisualProvider.IsShadowVisible = a.NewValue is true;
                    }
                }
            }));



        public Brush AcrylicBackground
        {
            get { return (Brush)GetValue(AcrylicBackgroundProperty); }
            set { SetValue(AcrylicBackgroundProperty, value); }
        }


        // Using a DependencyProperty as the backing store for AcrylicBackground.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AcrylicBackgroundProperty =
            DependencyProperty.Register("AcrylicBackground", typeof(Brush), typeof(HostWindow), new PropertyMetadata(null, (s, a) =>
            {
                if (!object.Equals(a.NewValue, a.OldValue) && s is HostWindow sender)
                {
                    sender.UpdateAcrylicBrush();
                }
            }));



        public bool HideWhenFullScreenAppOpen
        {
            get { return (bool)GetValue(HideWhenFullScreenAppOpenProperty); }
            set { SetValue(HideWhenFullScreenAppOpenProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HideWhenFullScreenAppOpen.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HideWhenFullScreenAppOpenProperty =
            DependencyProperty.Register("HideWhenFullScreenAppOpen", typeof(bool), typeof(HostWindow), new PropertyMetadata(false, (s, a) =>
            {
                if (!object.Equals(a.NewValue, a.OldValue) && s is HostWindow sender)
                {
                    sender.topmostHelper.HideWhenFullScreenAppOpen = a.NewValue is true;
                }
            }));



        #endregion Dependency Properties


        #region Update Properties

        private void UpdateAcrylicBrush()
        {
            if (acrylicVisualProvider == null) return;

            var color = Windows.UI.Color.FromArgb(255, 44, 44, 44);

            if (AcrylicBackground is SolidColorBrush scb)
            {
                color = Windows.UI.Color.FromArgb(scb.Color.A, scb.Color.R, scb.Color.G, scb.Color.B);
            }

            if (color.A == 255 && color.R < 45 && color.G < 45 && color.B < 45)
            {
                acrylicVisualProvider.TintColor = Windows.UI.Color.FromArgb(255, 44, 44, 44);
                acrylicVisualProvider.TintOpacity = 0;
                acrylicVisualProvider.TintLuminosityOpacity = 0.65;
            }
            else
            {
                acrylicVisualProvider.TintColor = color;
                acrylicVisualProvider.TintOpacity = 0.85;
                acrylicVisualProvider.TintLuminosityOpacity = 0.45;
            }
        }

        #endregion Update Properties


        public void SetBounds(double left, double top, double width, double height)
        {
            if (width < MinWidth || height < MinHeight) return;
            if (hwndSource != null)
            {
                var hwnd = hwndSource.Handle;

                var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

                User32.SetWindowPos(hwnd, IntPtr.Zero, (int)(left * dpi), (int)(top * dpi), (int)(width * dpi), (int)(height * dpi), User32.SetWindowPosFlags.SWP_NOZORDER);
                UpdateAcrylicSize(width, height);

                InvalidateMeasure();
                UpdateLayout();
            }
            SaveBounds();
        }


        public void SaveBounds()
        {
            WindowBoundsHelper.SetWindowBounds("lyric", Left, Top, Width, Height);
        }

    }

    public class LyricHost : HwndHost
    {
        public LyricWindow? Window { get; private set; }

        protected override HandleRef BuildWindowCore(HandleRef hwndParent)
        {
            Window = new LyricWindow();
            Window.Owner = System.Windows.Window.GetWindow(this);

            Window.Top = 0;
            Window.Left = 0;

            var handle = new WindowInteropHelper(Window).EnsureHandle();

            var style = (long)User32.GetWindowLongAuto(handle, User32.WindowLongFlags.GWL_STYLE);
            style |= (int)(User32.WindowStyles.WS_CHILD);
            User32.SetWindowLong(handle, User32.WindowLongFlags.GWL_STYLE, (IntPtr)style);

            User32.SetParent(handle, hwndParent.Handle);

            Window.ParentWindow = (HostWindow)System.Windows.Window.GetWindow(this);

            Window.Opacity = 0;
            Window.Show();
            Window.Activate();

            return new HandleRef(this, handle);
        }


        protected override void DestroyWindowCore(HandleRef hwnd)
        {
        }


        private void LyricHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Window != null)
            {
                Window.Width = this.ActualWidth;
                Window.Height = this.ActualHeight;
            }
        }

    }
}
