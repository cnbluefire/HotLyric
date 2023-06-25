using HotLyric.Win32.Utils;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace HotLyric.Win32.Controls
{
    internal class HotKeyInputBox : Control
    {
        public HotKeyInputBox()
        {
            this.DefaultStyleKey = typeof(HotKeyInputBox);
            this.IsEnabledChanged += HotKeyInputBox_IsEnabledChanged;
            this.Unloaded += HotKeyInputBox_Unloaded;
        }

        private TextBlock? PreviewTextBlock;
        private Button? ClearButton;

        private User32.VK newKey;
        private User32.HotKeyModifiers newModifiers;

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (ClearButton != null)
            {
                ClearButton.Click -= ClearButton_Click;
            }

            PreviewTextBlock = GetTemplateChild(nameof(PreviewTextBlock)) as TextBlock;
            ClearButton = GetTemplateChild(nameof(ClearButton)) as Button;

            if (ClearButton != null)
            {
                ClearButton.Click += ClearButton_Click;
            }

            UpdatePreviewText();
        }

        public User32.VK VirtualKey
        {
            get { return (User32.VK)GetValue(VirtualKeyProperty); }
            set { SetValue(VirtualKeyProperty, value); }
        }

        public static readonly DependencyProperty VirtualKeyProperty =
            DependencyProperty.Register("VirtualKey", typeof(User32.VK), typeof(HotKeyInputBox), new PropertyMetadata((User32.VK)0, (s, a) =>
            {
                if (s is HotKeyInputBox sender) sender.UpdatePreviewText();
            }));

        public User32.HotKeyModifiers Modifiers
        {
            get { return (User32.HotKeyModifiers)GetValue(ModifiersProperty); }
            set { SetValue(ModifiersProperty, value); }
        }

        public static readonly DependencyProperty ModifiersProperty =
            DependencyProperty.Register("Modifiers", typeof(User32.HotKeyModifiers), typeof(HotKeyInputBox), new PropertyMetadata((User32.HotKeyModifiers)0, (s, a) =>
            {
                if (s is HotKeyInputBox sender) sender.UpdatePreviewText();
            }));

        public string InvalidKeyDisplayText
        {
            get { return (string)GetValue(InvalidKeyDisplayTextProperty); }
            set { SetValue(InvalidKeyDisplayTextProperty, value); }
        }

        public static readonly DependencyProperty InvalidKeyDisplayTextProperty =
            DependencyProperty.Register("InvalidKeyDisplayText", typeof(string), typeof(HotKeyInputBox), new PropertyMetadata("空", (s, a) =>
            {
                if (s is HotKeyInputBox sender) sender.UpdatePreviewText();
            }));

        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (CapturePointer(e.Pointer))
            {
                this.Focus(FocusState.Pointer);
                e.Handled = true;
            }
        }

        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            base.OnPointerReleased(e);

            ReleasePointerCapture(e.Pointer);
            this.Focus(FocusState.Pointer);
            e.Handled = true;
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            base.OnGotFocus(e);

            newKey = 0;
            newModifiers = 0;

            UpdateCommonVisualState();

            UpdatePreviewText();
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);

            if (HotKeyHelper.IsCompleted(newModifiers, newKey))
            {
                Modifiers = newModifiers;
                VirtualKey = newKey;
            }

            newKey = 0;
            newModifiers = 0;

            UpdateCommonVisualState();

            UpdatePreviewText();
        }

        protected override void OnPreviewKeyDown(KeyRoutedEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (e.Key == Windows.System.VirtualKey.Tab
                || e.Key == Windows.System.VirtualKey.Enter
                || e.Key == Windows.System.VirtualKey.Escape)
            {
                if (e.Key == Windows.System.VirtualKey.Tab)
                {
                    return;
                }
                else if (e.Key == Windows.System.VirtualKey.Escape)
                {
                    this.newModifiers = Modifiers;
                    this.newKey = VirtualKey;
                }

                var focusElement = FocusManager.FindFirstFocusableElement(XamlRoot.Content);

                if (focusElement != null)
                {
                    _ = FocusManager.TryFocusAsync(focusElement, FocusState.Programmatic);
                }

                e.Handled = true;
                return;
            }

            var newKey = (User32.VK)e.Key;
            var isModifier = HotKeyHelper.MapModifiers(newKey) != 0;

            e.Handled = true;

            if (isModifier)
            {
                newModifiers = HotKeyHelper.GetCurrentModifiersStates();
            }
            else
            {
                this.newKey = newKey;
            }

            UpdatePreviewText();
        }

        protected override void OnPreviewKeyUp(KeyRoutedEventArgs e)
        {
            base.OnPreviewKeyUp(e);

            if (e.Key == Windows.System.VirtualKey.Tab
                || e.Key == Windows.System.VirtualKey.Enter
                || e.Key == Windows.System.VirtualKey.Escape)
            {
                return;
            }

            if (HotKeyHelper.IsCompleted(this.newModifiers, this.newKey))
            {
                e.Handled = true;
                return;
            }

            var newKey = (User32.VK)e.Key;
            var isModifier = HotKeyHelper.MapModifiers(newKey) != 0;

            if (isModifier)
            {
                newModifiers = HotKeyHelper.GetCurrentModifiersStates();
            }
            else if (newKey == this.newKey)
            {
                this.newKey = 0;
            }

            UpdatePreviewText();
        }


        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            this.newKey = 0;
            this.newModifiers = 0;

            Modifiers = newModifiers;
            VirtualKey = newKey;

            UpdateCommonVisualState();

            UpdatePreviewText();
        }


        private void UpdatePreviewText()
        {
            if (PreviewTextBlock == null) return;

            var modifiers = Modifiers;
            var key = VirtualKey;

            if (FocusState == FocusState.Unfocused)
            {
                // 失去焦点时，组合键无效则显示空

                var text = HotKeyHelper.MapKeyToString(modifiers, key);
                if (HotKeyHelper.IsCompleted(modifiers, key))
                {
                    PreviewTextBlock.Text = text;
                }
                else
                {
                    PreviewTextBlock.Text = InvalidKeyDisplayText;
                }
            }
            else
            {
                // 获得焦点时，组合键无文本则显示空

                string text = "";
                if (newModifiers == 0 && newKey == 0)
                {
                    text = HotKeyHelper.MapKeyToString(modifiers, key);
                }
                else
                {
                    text = HotKeyHelper.MapKeyToString(newModifiers, newKey);
                }

                if (!string.IsNullOrEmpty(text))
                {
                    PreviewTextBlock.Text = text;
                }
                else
                {
                    PreviewTextBlock.Text = InvalidKeyDisplayText;
                }
            }
        }

        #region UpdateVisualState

        private void HotKeyInputBox_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateCommonVisualState();
        }

        private bool pointerOver;

        private void HotKeyInputBox_Unloaded(object sender, RoutedEventArgs e)
        {
            pointerOver = false;
            UpdateCommonVisualState();
        }

        protected override void OnPointerEntered(PointerRoutedEventArgs e)
        {
            base.OnPointerEntered(e);
            pointerOver = true;

            UpdateCommonVisualState();
        }

        protected override void OnPointerExited(PointerRoutedEventArgs e)
        {
            base.OnPointerExited(e);
            pointerOver = false;

            UpdateCommonVisualState();
        }

        protected override void OnPointerCanceled(PointerRoutedEventArgs e)
        {
            base.OnPointerCanceled(e);
            pointerOver = false;

            UpdateCommonVisualState();
        }

        private void UpdateCommonVisualState()
        {
            if (!IsEnabled)
            {
                VisualStateManager.GoToState(this, "Disabled", true);
            }
            else if (FocusState != FocusState.Unfocused)
            {
                VisualStateManager.GoToState(this, "Focused", true);
            }
            else if (pointerOver)
            {
                VisualStateManager.GoToState(this, "PointerOver", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "Normal", true);
            }
        }

        #endregion UpdateVisualState
    }
}
