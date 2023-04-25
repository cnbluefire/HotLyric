using HotLyric.Win32.Controls;
using HotLyric.Win32.Utils;
using HotLyric.Win32.ViewModels;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vanara.Extensions;
using Vanara.PInvoke;
using Windows.UI.Xaml;
using WinUIEx;
using WinRT;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using HotLyric.Win32.Base;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Diagnostics;
using Microsoft.Win32;
using System.Reflection.Metadata;

namespace HotLyric.Win32.Views
{
    internal sealed partial class LyricView : WindowEx
    {
        public LyricView()
        {
            InitializeComponent();

            var handle = this.GetWindowHandle();

            #region Set Style

            var style = (uint)User32.GetWindowLongAuto(handle, User32.WindowLongFlags.GWL_STYLE);
            var exStyle = (uint)User32.GetWindowLongAuto(handle, User32.WindowLongFlags.GWL_EXSTYLE);

            UpdateStyleValue(ref style, ref exStyle);

            User32.SetWindowLong(handle, User32.WindowLongFlags.GWL_STYLE, (nint)style);
            User32.SetWindowLong(handle, User32.WindowLongFlags.GWL_EXSTYLE, (nint)exStyle);

            #endregion Set Style

            DwmApi.DwmExtendFrameIntoClientArea(handle, new DwmApi.MARGINS(-1));
            DwmApi.DwmEnableBlurBehindWindow(handle, new DwmApi.DWM_BLURBEHIND(true));

            ContentRoot.PointerPressed += ContentRoot_PointerPressed;
            ContentRoot.PointerCanceled += ContentRoot_PointerCanceled;
            ContentRoot.PointerCaptureLost += ContentRoot_PointerCaptureLost;

            ContentRoot.PointerEntered += ContentRoot_PointerEntered;
            ContentRoot.PointerExited += ContentRoot_PointerExited;

            VM.PropertyChanged += VM_PropertyChanged;

            if (!VM.ActualMinimized)
            {
                this.Hide();
            }

            InitBorderAndTitleOpacityAnimation();

            var manager = WindowManager.Get(this);
            manager.WindowMessageReceived += Manager_WindowMessageReceived;

            AcrylicController.Window = this;

            this.IsAlwaysOnTop = true;

            TopmostHelper = new WindowTopmostHelper(this);

            if (WindowBoundsHelper.TryGetWindowBounds("lyric", out var x, out var y, out var width, out var height))
            {
                var rect = new Windows.Graphics.RectInt32((int)x, (int)y, (int)width, (int)height);
                this.AppWindow.MoveAndResize(rect);
                this.AppWindow.MoveAndResize(rect);
                // 连续调用两次防止跨显示器时被自动缩放
            }
            else
            {
                WindowBoundsHelper.ResetWindowBounds(handle);
            }

            this.AppWindow.Closing += AppWindow_Closing;

            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                if (AppWindow.IsVisible)
                {
                    RefreshWindowSize();

                    if (WindowBoundsHelper.IsWindowOutsideScreen(handle))
                    {
                        ResetWindowBounds();
                    }

                    this.AppWindow.Changed += AppWindow_Changed;
                }
            });
        }

        public WindowTopmostHelper TopmostHelper { get; private set; }

        public LyricWindowViewModel VM => (LyricWindowViewModel)LayoutRoot.DataContext;

        private void VM_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs? e)
        {
            if (e?.PropertyName == nameof(VM.ActualMinimized))
            {
                if (VM.ActualMinimized && this.Visible)
                {
                    this.Hide();
                }
                else if (!VM.ActualMinimized && !this.Visible)
                {
                    this.Show();
                    DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        if (AppWindow.IsVisible)
                        {
                            var handle = this.GetWindowHandle();

                            if (WindowBoundsHelper.IsWindowOutsideScreen(handle))
                            {
                                ResetWindowBounds();
                            }
                        }
                    });
                }
            }
            else if (e?.PropertyName == nameof(VM.IsTransparent))
            {
                WindowHelper.SetLayeredWindow(this, VM.IsTransparent);
                WindowHelper.SetTransparent(this, VM.IsTransparent);
            }
        }
        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (App.Current.Exiting)
            {
                VM.PropertyChanged -= VM_PropertyChanged;

                var manager = WindowManager.Get(this);
                manager.WindowMessageReceived -= Manager_WindowMessageReceived;

                this.AppWindow.Changed -= AppWindow_Changed;

                AcrylicController.Window = null;
                TopmostHelper?.Dispose();
                TopmostHelper = null!;
            }
            else
            {
                args.Cancel = true;
            }
        }

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (args.DidSizeChange || args.DidPositionChange)
            {
                User32.GetWindowRect(this.GetWindowHandle(), out var rect);
                WindowBoundsHelper.SetWindowBounds("lyric", rect.Left, rect.Top, rect.Width, rect.Height);
            }
        }

        private void OnDisplayChanged()
        {
            if (!AppWindow.IsVisible) return;
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, async () =>
            {
                await Task.Delay(1000);

                if (AppWindow.IsVisible)
                {
                    var handle = this.GetWindowHandle();
                    if (WindowBoundsHelper.IsWindowOutsideScreen(handle))
                    {
                        ResetWindowBounds();
                    }
                }
            });
        }

        private void RefreshWindowSize()
        {
            var size = AppWindow.Size;

            var dpi = this.GetDpiForWindow();
            var minWidthPixel = (int)(MinWidth * dpi / 96);
            var minHeightPixel = (int)(MinHeight * dpi / 96);

            if (size.Width < minWidthPixel || size.Height < minHeightPixel)
            {
                size.Width = Math.Max(size.Width, minWidthPixel);
                size.Height = Math.Max(size.Height, minHeightPixel);
            }
            else
            {
                size.Height += 1;
                AppWindow.Resize(size);
                size.Height -= 1;
            }
            AppWindow.Resize(size);
        }

        private void ResetWindowBounds()
        {
            if (AppWindow.IsVisible)
            {
                var handle = this.GetWindowHandle();

                WindowBoundsHelper.ResetWindowBounds(handle);
                RefreshWindowSize();
                WindowBoundsHelper.ResetWindowBounds(handle);

                if (VM.IsTransparent)
                {
                    App.Current.NotifyIcon?.ToggleWindowTransparent();
                }
                VM.SettingViewModel.ActivateInstance();
            }
        }

        private void ContentRoot_PointerEntered(object sender, global::Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (!VM.IsTransparent)
            {
                VM.IsMouseOver = true;
            }
        }

        private async void ContentRoot_PointerExited(object sender, global::Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            var p = e.GetCurrentPoint(ContentRoot);

            if (ContentRoot.ActualWidth < 10 || ContentRoot.ActualHeight < 10)
            {
                VM.IsMouseOver = false;
            }
            else if (!VM.IsTransparent)
            {
                await UpdateMouseExitedState(e);
            }

            if (!VM.IsMouseOver && e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
            {
                VM.IsBackgroundTransientVisible = false;
            }
        }

        private async void ContentRoot_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            await UpdateMouseExitedState(e);
        }

        private async void ContentRoot_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            await UpdateMouseExitedState(e);
        }

        private void ContentRoot_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;

            _ = this.DragMoveAsync(e);
        }

        private async Task UpdateMouseExitedState(PointerRoutedEventArgs e)
        {
            var p = e.GetCurrentPoint(ContentRoot);
            if (p.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse)
            {
                var tcs = new TaskCompletionSource();
                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, async () =>
                {
                    try
                    {
                        await Task.Delay(100);

                        VM.IsMouseOver = IsMouseInsideWindow();
                    }
                    catch { }
                    tcs.SetResult();
                });

                await tcs.Task;
            }
            else
            {
                VM.IsMouseOver = false;
                VM.ShowBackgroundTransient(TimeSpan.FromSeconds(1));
            }
        }

        private bool IsMouseInsideWindow()
        {
            if (AppWindow.IsVisible
                && User32.GetWindowRect(this.GetWindowHandle(), out var rect)
                && User32.GetCursorPos(out var point))
            {
                var rect2 = (System.Drawing.Rectangle)rect;
                var point2 = (System.Drawing.Point)point;

                return rect2.Contains(point2);
            }

            return false;
        }

        public void Button_Click(object sender, RoutedEventArgs args)
        {
            GC.Collect();
        }

        private void ResizePanel_DraggerPointerPressed(Controls.ResizePanel sender, Controls.ResizePanelDraggerPressedEventArgs args)
        {
            _ = this.DragResizeAsync(args.PointerEventArgs, args.Edge);
        }

        private void ContentRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //InitializeLayoutRootShadow();
            //UpdateShadowSize();

            VM.ShowBackgroundTransient(TimeSpan.FromSeconds(10));
        }

        [System.Diagnostics.DebuggerNonUserCode]
        private unsafe void Manager_WindowMessageReceived(object? sender, WinUIEx.Messaging.WindowMessageEventArgs e)
        {
            if (e.Message.MessageId == (uint)User32.WindowMessage.WM_STYLECHANGING)
            {
                var style = ((STYLESTRUCT*)e.Message.LParam.ToPointer())->styleNew;
                var tmp = 0u;

                if (e.Message.WParam.ToUInt64() == unchecked((ulong)User32.WindowLongFlags.GWL_STYLE))
                {
                    UpdateStyleValue(ref style, ref tmp);
                }
                else
                {
                    UpdateStyleValue(ref tmp, ref style);
                }

                ((STYLESTRUCT*)e.Message.LParam.ToPointer())->styleNew = style;

                e.Handled = true;
            }
            else if (e.Message.MessageId == (uint)User32.WindowMessage.WM_DISPLAYCHANGE)
            {
                OnDisplayChanged();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct STYLESTRUCT
        {
            public uint styleOld;
            public uint styleNew;
        }


        private static void UpdateStyleValue(ref uint style, ref uint exStyle)
        {
            style &= ~(uint)(User32.WindowStyles.WS_OVERLAPPEDWINDOW);
            style |= (uint)(User32.WindowStyles.WS_POPUP);

            exStyle &= ~(uint)(User32.WindowStylesEx.WS_EX_APPWINDOW);
            exStyle |= (uint)(User32.WindowStylesEx.WS_EX_TOOLWINDOW
                | User32.WindowStylesEx.WS_EX_LAYERED
                | User32.WindowStylesEx.WS_EX_NOACTIVATE);
        }

        private void LockButton_Click(object sender, RoutedEventArgs e)
        {
            VM.SettingViewModel.WindowTransparent = true;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModelLocator.Instance.SettingsWindowViewModel.ShowSettingsWindow();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs args)
        {
            VM.IsMinimized = true;
        }

        private void InitBorderAndTitleOpacityAnimation()
        {
            var visual = ElementCompositionPreview.GetElementVisual(BackgroundAndShadowHost);
            var visual2 = ElementCompositionPreview.GetElementVisual(TitleContainer);

            var compositor = visual.Compositor;

            var imp = compositor.CreateImplicitAnimationCollection();

            var animation = compositor.CreateScalarKeyFrameAnimation();
            animation.InsertExpressionKeyFrame(1, "this.FinalValue");
            animation.Duration = TimeSpan.FromSeconds(0.2);
            animation.Target = "Opacity";

            imp[animation.Target] = animation;

            visual.ImplicitAnimations = imp;
            visual2.ImplicitAnimations = imp;
        }

        private void ListView_ItemClick(object sender, Microsoft.UI.Xaml.Controls.ItemClickEventArgs e)
        {
            if (sender is FrameworkElement ele
                && ele.Parent is Microsoft.UI.Xaml.Controls.FlyoutPresenter flyoutPresenter)
            {
                var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(flyoutPresenter.XamlRoot)
                    .FirstOrDefault(c => c.Child == flyoutPresenter);

                if (popup != null)
                {
                    popup.IsOpen = false;
                }
            }
        }

        private void MoreSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement ele
                && ele.Resources.TryGetValue("MoreSessionListFlyout", out var _flyout)
                && _flyout is FlyoutBase flyout)
            {
                this.SetForegroundWindow();

                flyout.ShowAt(ele);
            }
        }

    }
}
