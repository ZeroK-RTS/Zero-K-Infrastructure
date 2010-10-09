using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace MissionEditor2
{
    public class IntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int ret;
            if (Int32.TryParse((string)value, out ret)) return ret;
            throw new ArgumentException("Field must be a number.");
        }
    }
}
