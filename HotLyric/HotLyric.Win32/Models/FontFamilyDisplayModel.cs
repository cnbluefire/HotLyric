using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace HotLyric.Win32.Models
{
    public class FontFamilyDisplayModel
    {
        public FontFamilyDisplayModel(FontFamily fontFamily)
        {
            FontFamily = fontFamily ?? throw new ArgumentNullException(nameof(fontFamily));
            Source = fontFamily.Source;

            if (Source == "Global User Interface")
            {
                DisplayName = "默认UI字体";
            }
            else if (Source == "Global Serif")
            {
                DisplayName = "默认衬线字体";
            }
            else if (Source == "Global Sans Serif")
            {
                DisplayName = "默认无衬线字体";
            }
            else if (Source == "Global Monospace")
            {
                DisplayName = "默认等宽字体";
            }
            else
            {
                var lang1 = System.Windows.Markup.XmlLanguage.GetLanguage("zh-cn");
                if (fontFamily.FamilyNames.TryGetValue(lang1, out var value) && !string.IsNullOrEmpty(value))
                {
                    DisplayName = value;
                    Order = 1;
                }

                if (string.IsNullOrEmpty(DisplayName))
                {
                    var lang2 = System.Windows.Markup.XmlLanguage.GetLanguage("zh-Hans");
                    if (fontFamily.FamilyNames.TryGetValue(lang2, out var value2) && !string.IsNullOrEmpty(value2))
                    {
                        DisplayName = value2;
                        Order = 1;
                    }
                }


                if (string.IsNullOrEmpty(DisplayName))
                {
                    var name = fontFamily.FamilyNames
                        .FirstOrDefault(c => c.Key.IetfLanguageTag.StartsWith("zh"));

                    if (!string.IsNullOrEmpty(name.Value))
                    {
                        DisplayName = name.Value;
                        Order = 2;
                    }
                }

                if (string.IsNullOrEmpty(DisplayName))
                {
                    var name = fontFamily.FamilyNames
                        .FirstOrDefault(c => c.Key.IetfLanguageTag.StartsWith("en"));

                    if (!string.IsNullOrEmpty(name.Value))
                    {
                        DisplayName = name.Value;
                        Order = 2;
                    }
                }

                if (string.IsNullOrEmpty(DisplayName))
                {
                    if (fontFamily.FamilyNames.Count > 0)
                    {
                        DisplayName = fontFamily.FamilyNames.Values.First();
                        Order = 4;
                    }
                }

                if (string.IsNullOrEmpty(DisplayName))
                {
                    DisplayName = Source;
                    Order = 10;
                }
            }
        }

        public string Source { get; }

        public FontFamily FontFamily { get; }

        public string DisplayName { get; }

        public int Order { get; }
    }
}
