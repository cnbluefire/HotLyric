using HotLyric.Win32.Controls;
using HotLyric.Win32.Models;
using HotLyric.Win32.Utils;
using HotLyric.Win32.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace HotLyric.Win32.Views
{
    /// <summary>
    /// SettingsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            ResetLyricHorizontalAlignment();
            ResetSecondRow();
            ResetLyricOpacity();
            this.Icon = WindowHelper.GetDefaultAppIconImage();

            this.IsVisibleChanged += SettingsWindow_IsVisibleChanged;
        }

        HwndSource? hwndSource;

        public SettingsWindowViewModel VM => (DataContext as SettingsWindowViewModel)!;

        private async void SettingsWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            await VM.StartupTaskHelper.RefreshAsync();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var hwnd = new WindowInteropHelper(this).Handle;
            hwndSource = HwndSource.FromHwnd(hwnd);

            WindowHelper.SetWindowIconVisible(hwnd, false);

            var selectTag = VM.ThemeIsPresetVisible ? "Theme_Presets" : "Theme_Customize";
            ThemeModeComboBox.SelectedItem = ThemeModeComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(c => object.Equals(c.Tag, selectTag));
        }

        private void LyricHorizontalAlignment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            var comboBox = (ComboBox)sender;

            if (comboBox.SelectedItem is ComboBoxItem comboBoxItem && comboBoxItem.Tag is HorizontalAlignment ha && ha != HorizontalAlignment.Stretch)
            {
                VM.LyricHorizontalAlignment = ha;
            }
            else
            {
                ResetLyricHorizontalAlignment();
            }
        }

        private void ResetLyricHorizontalAlignment()
        {
            LyricHorizontalAlignmentComboBox.SelectedItem = LyricHorizontalAlignmentComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(c => object.Equals(VM.LyricHorizontalAlignment, c.Tag));
        }

        private void ThemeModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            var comboBox = (ComboBox)sender;

            if (comboBox.SelectedItem is ComboBoxItem comboBoxItem && comboBoxItem.Tag is string str)
            {
                VM.ThemeIsPresetVisible = str != "Theme_Customize";
            }

            var selectTag = VM.ThemeIsPresetVisible ? "Theme_Presets" : "Theme_Customize";

            comboBox.SelectedItem = comboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(c => object.Equals(c.Tag, selectTag));
        }

        private async void ThemePresetsGridView_ItemClick(object sender, ModernWpf.Controls.ItemClickEventArgs e)
        {
            var item = e.ClickedItem as LyricThemeView;
            await Task.Delay(1);
            var gridView = ((ModernWpf.Controls.GridView)sender);
            gridView.SelectedIndex = -1;

            if (item != null)
            {
                VM.CurrentTheme = item;
            }
        }

        private void SecondRowComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            var comboBox = (ComboBox)sender;

            if (comboBox.SelectedItem is ComboBoxItem comboBoxItem && comboBoxItem.Tag is SecondRowType type)
            {
                VM.SecondRowType = type;
            }
            else
            {
                ResetSecondRow();
            }
        }


        private void ResetSecondRow()
        {
            SecondRowComboBox.SelectedItem = SecondRowComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(c => object.Equals(VM.SecondRowType, c.Tag));
        }

        private void LyricOpacityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            var comboBox = (ComboBox)sender;

            if (comboBox.SelectedItem is ComboBoxItem comboBoxItem
                && comboBoxItem.Content is string str
                && str.Length > 1
                && double.TryParse(str.Substring(0, str.Length - 1), out var value))
            {
                var value2 = value / 100;
                VM.LyricOpacity = value2;
            }
            else
            {
                ResetLyricOpacity();
            }
        }

        private void ResetLyricOpacity()
        {
            LyricOpacityComboBox.SelectedItem = LyricOpacityComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(c => object.Equals($"{VM.LyricOpacity * 100}%", c.Content));
            if (LyricOpacityComboBox.SelectedItem == null) LyricOpacityComboBox.SelectedItem = LyricOpacityComboBox.Items[0];
        }

        private void ResetWindowBoundsButton_Click(object sender, RoutedEventArgs e)
        {
            var hostWindow = App.Current.Windows.OfType<HostWindow>().FirstOrDefault();
            if (hostWindow == null) return;

            var vm = ViewModelLocator.Instance.LyricWindowViewModel;

            if (vm.IsMinimized) vm.IsMinimized = false;

            // 此时窗口仍不可见
            if (vm.ActualMinimized) return;

            var hwnd = new WindowInteropHelper(hostWindow).Handle;

            WindowBoundsHelper.ResetWindowBounds(hwnd);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                hostWindow.SaveBounds();
                vm.ShowBackgroundTransient(TimeSpan.FromSeconds(2));
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }
}
