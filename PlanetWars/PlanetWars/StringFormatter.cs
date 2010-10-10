using System;
using System.Globalization;
using System.Windows.Data;
using JetBrains.Annotations;

namespace PlanetWars
{
    public class StringFormatter: IValueConverter
    {
        [StringFormatMethod("parameter")]
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter != null) {
                var formatterString = parameter.ToString();
                if (!string.IsNullOrEmpty(formatterString)) return string.Format(culture, formatterString, value);
            }
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}