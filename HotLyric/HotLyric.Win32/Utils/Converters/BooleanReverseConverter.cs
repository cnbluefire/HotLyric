using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;

namespace HotLyric.Win32.Utils.Converters
{
    public class BooleanReverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is not true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is not true;
        }
    }
}
