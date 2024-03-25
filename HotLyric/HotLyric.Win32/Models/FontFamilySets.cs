using BlueFire.Toolkit.WinUI3.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HotLyric.Win32.Models
{
    public class FontFamilySets : IEquatable<FontFamilySets>
    {
        public const string LyricCompositeFontFamilyName = "LyricCompositeFontFamily";

        private int _hashCode;

        public FontFamilySets(string primaryFontFamily = "SYSTEM-UI", string? westernTextFontFamily = null, string? japaneseKanaFontFamily = null, string? koreanHangulFontFamily = null)
        {
            PrimaryFontFamily = primaryFontFamily;
            WesternTextFontFamily = westernTextFontFamily;
            JapaneseKanaFontFamily = japaneseKanaFontFamily;
            KoreanHangulFontFamily = koreanHangulFontFamily;

            _hashCode = HashCode.Combine(primaryFontFamily, westernTextFontFamily, japaneseKanaFontFamily, koreanHangulFontFamily);
        }

        public string PrimaryFontFamily { get; } = "SYSTEM-UI";

        /// <summary>
        /// 西文字体
        /// </summary>
        public string? WesternTextFontFamily { get; }

        /// <summary>
        /// 日语假名字体
        /// </summary>
        public string? JapaneseKanaFontFamily { get; }

        /// <summary>
        /// 朝鲜语谚文字体
        /// </summary>
        public string? KoreanHangulFontFamily { get; }

        public bool IsCompositeFont =>
            !string.IsNullOrEmpty(WesternTextFontFamily)
            || !string.IsNullOrEmpty(JapaneseKanaFontFamily)
            || !string.IsNullOrEmpty(KoreanHangulFontFamily);

        public bool Equals(FontFamilySets? other)
        {
            return other is not null
                && PrimaryFontFamily == other.PrimaryFontFamily
                && WesternTextFontFamily == other.WesternTextFontFamily
                && JapaneseKanaFontFamily == other.JapaneseKanaFontFamily
                && KoreanHangulFontFamily == other.KoreanHangulFontFamily;
        }

        public override bool Equals(object? obj)
        {
            return obj is FontFamilySets obj1 && Equals(obj1);
        }

        public override int GetHashCode() => _hashCode;

        internal static readonly UnicodeRange[] WesternTextUnicodeRange = new[]
        {
            new UnicodeRange() { first = 0x0000, last = 0x007F }, // Basic Latin
            new UnicodeRange() { first = 0x0080, last = 0x00FF }, // Latin-1 Supplement
            new UnicodeRange() { first = 0x0100, last = 0x017F }, // Latin Extended-A
            new UnicodeRange() { first = 0x0180, last = 0x024F }, // Latin Extended-B
            new UnicodeRange() { first = 0x0250, last = 0x02AF }, // IPA Extensions
            new UnicodeRange() { first = 0x02B0, last = 0x02FF }, // Spacing Modifier Letters
            new UnicodeRange() { first = 0x0300, last = 0x036F }, // Combining Diacritical Marks 
            new UnicodeRange() { first = 0x0370, last = 0x03FF }, // Greek and Coptic
            new UnicodeRange() { first = 0x0400, last = 0x04FF }, // Cyrillic
            new UnicodeRange() { first = 0x0500, last = 0x052F }, // Cyrillic Supplement 
            new UnicodeRange() { first = 0x1D00, last = 0x1D7F }, // Phonetic Extensions
            new UnicodeRange() { first = 0x1D80, last = 0x1DBF }, // Phonetic Extensions Supplement
            new UnicodeRange() { first = 0x1DC0, last = 0x1DFF }, // Combining Diacritical Marks Supplement
            new UnicodeRange() { first = 0x1E00, last = 0x1EFF }, // Latin Extended Additional
            new UnicodeRange() { first = 0x1F00, last = 0x1FFF }, // Greek Extended
            new UnicodeRange() { first = 0x2C60, last = 0x2C7F }, // Latin Extended-C
            new UnicodeRange() { first = 0x2DE0, last = 0x2DFF }, // Cyrillic Extended-A
            new UnicodeRange() { first = 0xA640, last = 0xA69F }, // Cyrillic Extended-B
            new UnicodeRange() { first = 0xFB00, last = 0xFB0F }, // Alpha Pres Forms Latin
            new UnicodeRange() { first = 0xFB1D, last = 0xFB4F }, // Alpha Pres Forms(Hebrew)
            new UnicodeRange() { first = 0xFE20, last = 0xFE2F }, // Combining Half Marks
        };

        internal static readonly UnicodeRange[] JapaneseKanaUnicodeRange = new[]
        {
            new UnicodeRange() { first = '〄', last = '〄' }, // 〄 日本工业标准符号
            new UnicodeRange() { first = 0x3040, last = 0x309F }, // Hiragana
            new UnicodeRange() { first = 0x30A0, last = 0x30FF }, // Katakana
            new UnicodeRange() { first = 0x31F0, last = 0x31FF }, // Katakana Phonetic Ext
            new UnicodeRange() { first = 0x32D0, last = 0x32FF }, // Enclosed CJK Katakana
            new UnicodeRange() { first = 0x3300, last = 0x3357 }, // CJK Comp Square Katakana
        };

        internal static readonly UnicodeRange[] KoreanHangulUnicodeRange = new[]
        {
            new UnicodeRange() { first = 0x1100, last = 0x11FF }, // Hangul Jamo
            new UnicodeRange() { first = 0x3130, last = 0x318F }, // Hangul Compatibility Jamo
            new UnicodeRange() { first = 0x3200, last = 0x321F }, // Enc. CJK (Paren Hangul)
            new UnicodeRange() { first = 0x3260, last = 0x327F }, // Enc. CJK (Circled Hangul)
            new UnicodeRange() { first = 0xAC00, last = 0xD7AF }, // Hangul Syllables
        };

        public static void UpdateCompositeFont(FontFamilySets? fontFamilySets)
        {
            CompositeFontManager.Unregister(LyricCompositeFontFamilyName);

            if (fontFamilySets?.IsCompositeFont is true)
            {
                var compositeFont = new CompositeFontFamily()
                {
                    FontFamilyName = LyricCompositeFontFamilyName,
                    FamilyMaps = new[]
                    {
                        new CompositeFontFamilyMap()
                        {
                            Target = fontFamilySets.WesternTextFontFamily,
                            UnicodeRanges = WesternTextUnicodeRange
                        },
                        new CompositeFontFamilyMap()
                        {
                            Target = fontFamilySets.JapaneseKanaFontFamily,
                            UnicodeRanges = JapaneseKanaUnicodeRange
                        },
                        new CompositeFontFamilyMap()
                        {
                            Target = fontFamilySets.KoreanHangulFontFamily,
                            UnicodeRanges = KoreanHangulUnicodeRange
                        },
                        new CompositeFontFamilyMap()
                        {
                            Target = fontFamilySets.PrimaryFontFamily,
                        }
                    }
                };

                CompositeFontManager.Register(compositeFont);
            }
        }
    }
}
