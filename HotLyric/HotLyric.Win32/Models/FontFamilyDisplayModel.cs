using HotLyric.Win32.Utils;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Windows.Globalization;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Xml.Linq;

namespace HotLyric.Win32.Models
{
    public class FontFamilyDisplayModel : IEquatable<FontFamilyDisplayModel>, IComparable<FontFamilyDisplayModel>
    {
        public static FontFamilyDisplayModel EmptyModel { get; } = new FontFamilyDisplayModel("", "空", 0);

        private static IReadOnlyList<FontFamilyDisplayModel>? allFamilies;
        private static object locker = new object();

        public static IReadOnlyList<FontFamilyDisplayModel> AllFamilies
        {
            get
            {
                if (allFamilies == null)
                {
                    lock (locker)
                    {
                        if (allFamilies == null)
                        {
                            allFamilies = GetAllFamilies();
                        }
                    }
                }

                return allFamilies;
            }
        }

        private FontFamilyDisplayModel(string source, string displayName, int order)
        {
            Source = source;
            DisplayName = displayName;
            Order = order;

            hashCode = HashCode.Combine(Source, DisplayName, Order);
        }

        private FontFamilyDisplayModel(string name, string displayName, string locale)
        {
            Source = name;
            DisplayName = displayName;

            if (locale.Length >= 2
                && (locale[0] == 'z' || locale[0] == 'Z')
                && (locale[1] == 'h' || locale[1] == 'H'))
            {
                // 当前UI为中文，将可能存在中文字体名的字体提取到顶部

                Order = (name != displayName) ? 50 : 60;
            }
            else
            {
                Order = 60;
            }

            hashCode = HashCode.Combine(Source, DisplayName, Order);
        }

        private int hashCode;
        private bool? isItalicStyleAvailable;

        public string Source { get; }

        public string DisplayName { get; }

        public int Order { get; }

        public bool IsItalicStyleAvailable
        {
            get
            {
                if (!isItalicStyleAvailable.HasValue)
                {
                    try
                    {
                        using var fontFamily = new System.Drawing.FontFamily(Source);
                        isItalicStyleAvailable = fontFamily.IsStyleAvailable(System.Drawing.FontStyle.Italic);
                    }
                    catch
                    {
                        isItalicStyleAvailable = false;
                    }
                }
                return isItalicStyleAvailable!.Value;
            }
        }

        public override string ToString()
        {
            return $"{DisplayName} ({Source})";
        }

        public bool Equals(FontFamilyDisplayModel? other)
        {
            return other != null
                && this.hashCode == other.hashCode
                && this.Order == other.Order
                && this.Source == other.Source
                && this.DisplayName == other.DisplayName;
        }

        public int CompareTo(FontFamilyDisplayModel? other)
        {
            if (other == null) return 1;
            var v1 = Order.CompareTo(other.Order);
            if (v1 != 0) return v1;

            return DisplayName.CompareTo(other.DisplayName);
        }

        private static IReadOnlyList<FontFamilyDisplayModel> GetAllFamilies()
        {
            var locale = new[] { CultureInfoUtils.DefaultUICulture.Name };

            string[]? names = null;
            string[]? displayNames = null;

            while (true)
            {
                names = CanvasTextFormat.GetSystemFontFamilies();
                displayNames = CanvasTextFormat.GetSystemFontFamilies(locale);

                if (names.Length == displayNames.Length) break;
            }

            var models = new FontFamilyDisplayModel[names.Length + 3];
            models[0] = new FontFamilyDisplayModel("SYSTEM-UI", "默认UI字体", 1);
            models[1] = new FontFamilyDisplayModel("UI-SERIF", "默认衬线体", 10);
            models[2] = new FontFamilyDisplayModel("UI-SANS-SERIF", "默认非衬线体", 10);

            for (int i = 0; i < names.Length; i++)
            {
                models[i + 3] = new FontFamilyDisplayModel(names[i], displayNames[i], locale[0]);
            }

            Array.Sort(models);

            return models;
        }

    }
}
