using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace MissionEditor2
{
    public class NullableBoolConverter : IValueConverter
    {

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Convert(value, targetType);
        }

        private static object Convert(object value, Type targetType)
        {
            if (targetType == typeof(bool?)) return new Nullable<bool>((bool)value);
            if (targetType == typeof(bool)) return ((bool?)value).HasValue && ((bool?)value).Value;
            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Convert(value, targetType);
        }

        #endregion
    }
}
