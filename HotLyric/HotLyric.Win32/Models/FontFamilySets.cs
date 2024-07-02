using BlueFire.Toolkit.WinUI3.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HotLyric.Win32.Models
{
    public static class FontFamilySets
    {
        private const string LyricCompositeWesternTextFontFamilyName = "__LyricCompositeWesternTextFontFamilyName";
        private const string LyricCompositeJapaneseKanaFontFamilyName = "__LyricCompositeJapaneseKanaFontFamilyName";
        private const string LyricCompositeKoreanHangulFontFamilyName = "__LyricCompositeKoreanHangulFontFamilyName";

        private static string primaryFontFamily = "SYSTEM-UI";
        private static string? westernTextFontFamily;
        private static string? japaneseKanaFontFamily;
        private static string? koreanHangulFontFamily;

        public static string PrimaryFontFamily
        {
            get => primaryFontFamily;
            set => primaryFontFamily = value;
        }

        /// <summary>
        /// 西文字体
        /// </summary>
        public static string? WesternTextFontFamily
        {
            get => westernTextFontFamily;
            set
            {
                if (westernTextFontFamily != value)
                {
                    westernTextFontFamily = value;
                    UpdateFontFamilyCore(LyricCompositeJapaneseKanaFontFamilyName, westernTextFontFamily, WesternTextUnicodeRange);
                }
            }
        }

        /// <summary>
        /// 日语假名字体
        /// </summary>
        public static string? JapaneseKanaFontFamily
        {
            get => japaneseKanaFontFamily;
            set
            {
                if (japaneseKanaFontFamily != value)
                {
                    japaneseKanaFontFamily = value;
                    UpdateFontFamilyCore(LyricCompositeJapaneseKanaFontFamilyName, japaneseKanaFontFamily, JapaneseKanaUnicodeRange);
                }
            }
        }

        /// <summary>
        /// 朝鲜语谚文字体
        /// </summary>
        public static string? KoreanHangulFontFamily
        {
            get => koreanHangulFontFamily;
            set
            {
                if (koreanHangulFontFamily != value)
                {
                    koreanHangulFontFamily = value;
                    UpdateFontFamilyCore(LyricCompositeKoreanHangulFontFamilyName, koreanHangulFontFamily, KoreanHangulUnicodeRange);
                }
            }
        }

        public static string CompositedFontFamily
        {
            get
            {
                if (string.IsNullOrEmpty(westernTextFontFamily)
                    && string.IsNullOrEmpty(japaneseKanaFontFamily)
                    && string.IsNullOrEmpty(koreanHangulFontFamily))
                {
                    return !string.IsNullOrEmpty(primaryFontFamily) ? primaryFontFamily : "SYSTEM-UI";
                }

                var sb = new StringBuilder();
                if(!string.IsNullOrEmpty(westernTextFontFamily)) sb.Append(LyricCompositeWesternTextFontFamilyName).Append(',');
                if (!string.IsNullOrEmpty(japaneseKanaFontFamily)) sb.Append(LyricCompositeJapaneseKanaFontFamilyName).Append(',');
                if (!string.IsNullOrEmpty(koreanHangulFontFamily)) sb.Append(LyricCompositeKoreanHangulFontFamilyName).Append(',');
                sb.Append(!string.IsNullOrEmpty(primaryFontFamily) ? primaryFontFamily : "SYSTEM-UI").Append(',');

                if (sb.Length > 0) sb.Length--;

                return sb.ToString();
            }
        }


        private static readonly UnicodeRange[] WesternTextUnicodeRange = new[]
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

        private static readonly UnicodeRange[] JapaneseKanaUnicodeRange = new[]
        {
            new UnicodeRange() { first = '〄', last = '〄' }, // 〄 日本工业标准符号
            new UnicodeRange() { first = 0x3040, last = 0x309F }, // Hiragana
            new UnicodeRange() { first = 0x30A0, last = 0x30FF }, // Katakana
            new UnicodeRange() { first = 0x31F0, last = 0x31FF }, // Katakana Phonetic Ext
            new UnicodeRange() { first = 0x32D0, last = 0x32FF }, // Enclosed CJK Katakana
            new UnicodeRange() { first = 0x3300, last = 0x3357 }, // CJK Comp Square Katakana
        };

        private static readonly UnicodeRange[] KoreanHangulUnicodeRange = new[]
        {
            new UnicodeRange() { first = 0x1100, last = 0x11FF }, // Hangul Jamo
            new UnicodeRange() { first = 0x3130, last = 0x318F }, // Hangul Compatibility Jamo
            new UnicodeRange() { first = 0x3200, last = 0x321F }, // Enc. CJK (Paren Hangul)
            new UnicodeRange() { first = 0x3260, last = 0x327F }, // Enc. CJK (Circled Hangul)
            new UnicodeRange() { first = 0xAC00, last = 0xD7AF }, // Hangul Syllables
        };

        private static void UpdateFontFamilyCore(string compositeFontFamilyName, string? fontFamilyName, UnicodeRange[]? unicodeRanges)
        {
            CompositeFontManager.Unregister(compositeFontFamilyName);

            if (!string.IsNullOrEmpty(fontFamilyName))
            {
                var compositeFont = new CompositeFontFamily()
                {
                    FontFamilyName = compositeFontFamilyName,
                    FamilyMaps = new[]
                    {
                        new CompositeFontFamilyMap()
                        {
                            Target = fontFamilyName,
                            UnicodeRanges = unicodeRanges
                        }
                    }
                };

                CompositeFontManager.Register(compositeFont);
            }
        }
    }
}
