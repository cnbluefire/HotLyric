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
using WinRT;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using HotLyric.Win32.Base;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Diagnostics;
using Microsoft.Win32;
using System.Reflection.Metadata;
using Windows.ApplicationModel;
using Microsoft.UI;
using BlueFire.Toolkit.WinUI3;
using BlueFire.Toolkit.WinUI3.Extensions;

namespace HotLyric.Win32.Views
{
    internal sealed partial class LyricView : TransparentWindow
    {
        public LyricView()
        {
            InitializeComponent();

            this.Title = "Hotlyric.LyricView";

            ContentRoot.PointerPressed += ContentRoot_PointerPressed;
            ContentRoot.PointerCanceled += ContentRoot_PointerCanceled;
            ContentRoot.PointerCaptureLost += ContentRoot_PointerCaptureLost;

            ContentRoot.PointerEntered += ContentRoot_PointerEntered;
            ContentRoot.PointerExited += ContentRoot_PointerExited;

            AcrylicController.Window = this.XamlWindow;

            WindowHelper.SetTopmost(this.XamlWindow, true);

            TopmostHelper = new WindowTopmostHelper(this.XamlWindow);

            if (WindowBoundsHelper.TryGetWindowBounds("lyric", out var x, out var y, out var width, out var height))
            {
                var rect = new Windows.Graphics.RectInt32((int)x, (int)y, (int)width, (int)height);
                this.AppWindow.MoveAndResize(rect);
                this.AppWindow.MoveAndResize(rect);
                // 连续调用两次防止跨显示器时被自动缩放
            }
            else
            {
                ResetWindowBounds(true);
            }

            InitBorderAndTitleOpacityAnimation();

            VM.PropertyChanged += VM_PropertyChanged;

            this.AppWindow.Closing += AppWindow_Closing;
            this.XamlWindow.Closed += LyricView_Closed;

            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                if (AppWindow.IsVisible)
                {
                    RefreshWindowSize();

                    if (WindowBoundsHelper.IsWindowOutsideScreen(this.GetWindowHandle()))
                    {
                        ResetWindowBounds(false);
                    }

                    this.AppWindow.Changed += AppWindow_Changed;
                }

                UpdateActualMinimized();
                UpdateTransparent();

                AcrylicController.Visible = true;
                LayoutRoot.Opacity = 1;
            });
        }

        private ExpressionAnimation? centerPointBind;
        private ExpressionAnimation? scaleBind;


        public WindowTopmostHelper? TopmostHelper { get; private set; }

        public LyricWindowViewModel VM => (LyricWindowViewModel)LayoutRoot.DataContext;

        #region View Model Property Changed

        private void VM_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs? e)
        {
            if (e?.PropertyName == nameof(VM.ActualMinimized))
            {
                UpdateActualMinimized();
            }
            else if (e?.PropertyName == nameof(VM.IsTransparent))
            {
                UpdateTransparent();
            }
        }

        private void UpdateActualMinimized()
        {
            if (VM.ActualMinimized && this.XamlWindow.Visible)
            {
                this.AppWindow.Hide();
            }
            else if (!VM.ActualMinimized && !this.XamlWindow.Visible)
            {
                this.AppWindow.Show();

                if (!ActivationArgumentsHelper.RedirectMode)
                {
                    DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                    {
                        if (AppWindow.IsVisible)
                        {
                            if (WindowBoundsHelper.IsWindowOutsideScreen(this.GetWindowHandle()))
                            {
                                ResetWindowBounds(false);
                            }
                        }
                    });
                }
            }
        }

        private void UpdateTransparent()
        {
            WindowHelper.SetLayeredWindow(this.XamlWindow, VM.IsTransparent);
            WindowHelper.SetTransparent(this.XamlWindow, VM.IsTransparent);
        }

        #endregion View Model Property Changed

        #region Window Close Events

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (App.Current.Exiting)
            {
                ReleaseResources();
            }
            else
            {
                args.Cancel = true;
            }
        }


        private void LyricView_Closed(object sender, WindowEventArgs args)
        {
            ReleaseResources();
        }

        private void ReleaseResources()
        {
            VM.PropertyChanged -= VM_PropertyChanged;

            this.AppWindow.Changed -= AppWindow_Changed;

            AcrylicController.Window = null;
            TopmostHelper?.Dispose();
            TopmostHelper = null!;
        }

        #endregion Window Close Events

        #region Window Bounds Helper

        private void RefreshWindowSize()
        {
            var size = AppWindow.Size;

            var dpi = this.WindowDpi;
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

        private void ResetWindowBounds(bool force)
        {
            if (force || (VM.SettingViewModel.AutoResetWindowPos && AppWindow.IsVisible))
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

        #endregion Window Bounds Helper

        #region Window Proc

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (args.DidSizeChange || args.DidPositionChange)
            {
                var handle = this.GetWindowHandle();
                User32.GetWindowRect(handle, out var rect);
                WindowBoundsHelper.SetWindowBounds("lyric", rect.Left, rect.Top, rect.Width, rect.Height);
            }
        }

        protected override void OnDpiChanged(WindowExDpiChangedEventArgs args)
        {
            base.OnDpiChanged(args);

            if (!AppWindow.IsVisible) return;
            if (ActivationArgumentsHelper.RedirectMode) return;

            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, async () =>
            {
                await Task.Delay(1000);

                if (AppWindow.IsVisible)
                {
                    var handle = this.GetWindowHandle();
                    if (WindowBoundsHelper.IsWindowOutsideScreen(handle))
                    {
                        ResetWindowBounds(false);
                    }
                }
            });
        }

        protected override void OnWindowMessageReceived(WindowMessageReceivedEventArgs e)
        {
            base.OnWindowMessageReceived(e);
            if (e.MessageId == (uint)User32.WindowMessage.WM_ENDSESSION)
            {
                LogHelper.LogInfo("WM_ENDSESSION");
                e.Handled = true;
                e.LResult = 0;
                DispatcherQueue.TryEnqueue(() =>
                {
                    App.Current.Exit();
                });
            }
        }

        #endregion Window Proc

        #region Pointer Events

        private void ContentRoot_PointerEntered(object sender, global::Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (!VM.IsTransparent)
            {
                VM.IsMouseOver = true;
            }
        }

        private async void ContentRoot_PointerExited(object sender, global::Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            await OnPointerExited(e);
        }

        private async void ContentRoot_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            await OnPointerExited(e);
        }

        private async void ContentRoot_PointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            await OnPointerExited(e);
        }

        private void ContentRoot_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;

            _ = this.XamlWindow.DragMoveAsync(e);
        }


        private async Task OnPointerExited(PointerRoutedEventArgs e)
        {
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
                    catch (Exception ex)
                    {
                        HotLyric.Win32.Utils.LogHelper.LogError(ex);
                    }
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
            var handle = this.GetWindowHandle();

            if (AppWindow.IsVisible
                && User32.GetWindowRect(handle, out var rect)
                && User32.GetCursorPos(out var point))
            {
                var rect2 = (System.Drawing.Rectangle)rect;
                var point2 = (System.Drawing.Point)point;

                return rect2.Contains(point2);
            }

            return false;
        }

        #endregion Pointer Events

        #region Element Events

        private void ResizePanel_DraggerPointerPressed(Controls.ResizePanel sender, Controls.ResizePanelDraggerPressedEventArgs args)
        {
            _ = this.XamlWindow.DragResizeAsync(args.PointerEventArgs, args.Edge);
        }

        private void ContentRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            VM.ShowBackgroundTransient(TimeSpan.FromSeconds(10));
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

        #endregion Element Events

        #region xBindFunctions

        private int TitleColumnSpan(bool isTitleButtonVisible)
        {
            return isTitleButtonVisible ? 1 : 3;
        }

        #endregion xBindFunctions

        private void InitBorderAndTitleOpacityAnimation()
        {
            var scaleAnimationSize = "32f";

            var visual = ElementCompositionPreview.GetElementVisual(NonContentPanel);

            var compositor = visual.Compositor;

            var imp = compositor.CreateImplicitAnimationCollection();

            var animation = compositor.CreateScalarKeyFrameAnimation();
            animation.InsertExpressionKeyFrame(1, "this.FinalValue");
            animation.Duration = WindowAcrylicContext.opacityAnimationDuration;
            animation.Target = "Opacity";

            imp[animation.Target] = animation;

            visual.ImplicitAnimations = imp;

            centerPointBind = compositor.CreateExpressionAnimation("Vector3(this.Target.Size.X / 2, this.Target.Size.Y / 2, 0)");
            visual.StartAnimation("CenterPoint", centerPointBind);

            scaleBind = compositor.CreateExpressionAnimation($"Vector3(({scaleAnimationSize} / this.Target.Size.X) * (1 - this.Target.Opacity) + 1, ({scaleAnimationSize} / this.Target.Size.Y) * (1 - this.Target.Opacity) + 1, 1)");
            visual.StartAnimation("Scale", scaleBind);

        }

    }
}
