using System.Collections.Generic;
using System.Linq;

namespace HotLyric.Win32.Utils.LyricFiles
{
    public record Lyric(ILyricDocument Content, string LyricContent, ILyricDocument? Translate, string? TranslateContent)
    {
        private static readonly HashSet<string> absoluteMusicLyricFlags = new HashSet<string>()
        {
            "纯音乐，请欣赏",
            "此歌曲为没有填词的纯音乐，请您欣赏"
        };

        public static Lyric? CreateClassicLyric(string lyricContent, string? translateContent)
        {
            ILyricDocument? content = null;
            ILyricDocument? translate = null;

            try
            {
                content = ClassicLyricDocument.Create(lyricContent);
                if (content != null)
                {
                    if (content.AllLines.Any(c => absoluteMusicLyricFlags.Contains(c.AllSpans.FirstOrDefault()?.Text ?? ""))
                        || content.AllLines.All(c => c.AllSpans.All(x => string.IsNullOrEmpty(x.Text))))
                    {
                        content = null;
                    }
                }
            }
            catch { }

            if (content != null)
            {
                if (!string.IsNullOrEmpty(translateContent))
                {
                    try
                    {
                        translate = ClassicLyricDocument.Create(translateContent!);
                        if (translate != null)
                        {
                            if (translate.AllLines.Any(c => absoluteMusicLyricFlags.Contains(c.AllSpans.FirstOrDefault()?.Text ?? ""))
                                || translate.AllLines.All(c => c.AllSpans.All(x => string.IsNullOrEmpty(x.Text))))
                            {
                                translate = null;
                            }
                        }
                    }
                    catch { }
                }

                return new Lyric(content, lyricContent, translate, translateContent);
            }

            return null;
        }
    }
}
