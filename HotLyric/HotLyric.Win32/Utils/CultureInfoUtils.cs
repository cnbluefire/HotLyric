using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotLyric.Win32.Utils
{
    public static class CultureInfoUtils
    {
        public static CultureInfo DefaultUICulture { get; private set; } = null!;

        public static CultureInfo DefaultUINeutralCulture { get; private set; } = null!;

        public static void Initialize()
        {
            DefaultUICulture = CultureInfo.CurrentUICulture;

            DefaultUINeutralCulture = DefaultUICulture;

            while (!DefaultUINeutralCulture.IsNeutralCulture)
            {
                DefaultUINeutralCulture = DefaultUINeutralCulture.Parent;
            }
        }

        public static bool IsMatch(CultureInfo cultureInfo, string languageName)
        {
            if (cultureInfo == null) return false;
            if (languageName.Length < 2) return false;

            if (string.Equals(languageName, cultureInfo.Name, StringComparison.OrdinalIgnoreCase))
                return true;

            if (cultureInfo.IsNeutralCulture)
            {
                if (string.Equals(languageName.Substring(0, 2), cultureInfo.TwoLetterISOLanguageName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
