using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using Windows.UI.Popups;

namespace HotLyric.Win32.Controls
{
    public class ComboBoxEx
    {
        public static PlacementMode GetPlacement(DependencyObject obj)
        {
            return (PlacementMode)obj.GetValue(PlacementProperty);
        }

        public static void SetPlacement(DependencyObject obj, PlacementMode value)
        {
            obj.SetValue(PlacementProperty, value);
        }

        // Using a DependencyProperty as the backing store for Placement.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PlacementProperty =
            DependencyProperty.RegisterAttached("Placement", typeof(PlacementMode), typeof(ComboBoxEx), new PropertyMetadata(PlacementMode.Bottom, (s, a) =>
            {
                if (!object.Equals(a.NewValue, a.OldValue) && s is ComboBox sender)
                {
                    var action = new Action(() =>
                    {
                        var popup = FindChild<Popup>(sender, "PART_Popup");
                        if (popup != null)
                        {
                            popup.Placement = GetPlacement(sender);
                        }
                    });
                    if (sender.IsLoaded)
                    {
                        action.Invoke();
                    }
                    else
                    {
                        sender.IsVisibleChanged += Sender_IsVisibleChanged;
                    }


                    void Sender_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
                    {
                        if (e.NewValue is false) return;
                        var comboBox = (ComboBox)sender;
                        comboBox.IsVisibleChanged -= Sender_IsVisibleChanged;
                        comboBox.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, action);
                    }
                }
            }));

        public static bool GetIsDropdownIconVisible(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsDropdownIconVisibleProperty);
        }

        public static void SetIsDropdownIconVisible(DependencyObject obj, bool value)
        {
            obj.SetValue(IsDropdownIconVisibleProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsDropdownIconVisible.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsDropdownIconVisibleProperty =
            DependencyProperty.RegisterAttached("IsDropdownIconVisible", typeof(bool), typeof(ComboBoxEx), new PropertyMetadata(true, (s, a) =>
            {
                if (!object.Equals(a.NewValue, a.OldValue) && s is ComboBox sender)
                {
                    var action = new Action(() =>
                    {
                        var dropdown = FindChild<FrameworkElement>(sender, "DropDownGlyph");
                        if (dropdown != null)
                        {
                            dropdown.Visibility = GetIsDropdownIconVisible(sender) ? Visibility.Visible : Visibility.Collapsed;
                        }
                    });
                    if (sender.IsLoaded)
                    {
                        action.Invoke();
                    }
                    else
                    {
                        sender.IsVisibleChanged += Sender_IsVisibleChanged;
                    }


                    void Sender_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
                    {
                        if (e.NewValue is false) return;
                        var comboBox = (ComboBox)sender;
                        comboBox.IsVisibleChanged -= Sender_IsVisibleChanged;
                        comboBox.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, action);
                    }
                }
            }));

        private static T? FindChild<T>(FrameworkElement element, string name)
            where T : FrameworkElement
        {
            if (string.IsNullOrEmpty(name) || element == null || !element.IsLoaded) return null;

            if (VisualTreeHelper.GetChildrenCount(element) == 0) return null;
            var child = VisualTreeHelper.GetChild(element, 0) as FrameworkElement;

            return child?.FindName(name) as T;
        }

        public static async Task<HwndSource?> GetComboBoxPopupHwndSourceAsync(ComboBox? comboBox)
        {
            if (comboBox == null || !comboBox.IsLoaded || !comboBox.IsDropDownOpen) return null;

            var popup = FindChild<Popup>(comboBox, "PART_Popup");
            if (popup == null) return null;

            var tcs = new TaskCompletionSource<HwndSource?>();

            await comboBox.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
            {
                var hwndSource = PresentationSource.CurrentSources.OfType<HwndSource>()
                    .FirstOrDefault(c => c.RootVisual is FrameworkElement ele && ele.Parent == popup);

                tcs.SetResult(hwndSource);
            }));

            return await tcs.Task.ConfigureAwait(false);
        }
    }
}
