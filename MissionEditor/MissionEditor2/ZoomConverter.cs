using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace MissionEditor2
{
    class ZoomConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var x = (double)value;
        	x = 2 - x - 0.01;
            return x > 1 ? 1 / (x + (x - 1) * 2) : 1 / x;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var x = (double)value;
            return x;
        }
    }
}
