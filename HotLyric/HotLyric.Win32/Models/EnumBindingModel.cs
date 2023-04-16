using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotLyric.Win32.Models
{
    public class EnumBindingModel<T> : ObservableObject where T : struct, Enum
    {
        private EnumDisplayModel<T>? selectedItem;
        private Action<Nullable<T>>? valueUpdatedCallback;

        public EnumBindingModel(IEnumerable<EnumDisplayModel<T>> items, Action<Nullable<T>>? valueUpdatedCallback = null)
        {
            Items = items.ToArray();

#if DEBUG
            Debug.Assert(Items.Count == Enum.GetValues(typeof(T)).Length);
#endif

            this.valueUpdatedCallback = valueUpdatedCallback;
        }

        public IReadOnlyList<EnumDisplayModel<T>> Items { get; }

        public Nullable<T> SelectedValue
        {
            get => SelectedItem?.Value;
            set => SelectedItem = value.HasValue ? SelectedItem = Items.First(c => Equals(c.Value, value.Value)) : null;
        }

        public EnumDisplayModel<T>? SelectedItem
        {
            get => selectedItem;
            set
            {
                if (SetProperty(ref selectedItem, value))
                {
                    OnPropertyChanged(nameof(SelectedValue));
                    valueUpdatedCallback?.Invoke(value?.Value);
                }
            }
        }
    }

    public record EnumDisplayModel<T>(string DisplayName, T Value)
    {
        public override string ToString() => DisplayName;
    }
}
