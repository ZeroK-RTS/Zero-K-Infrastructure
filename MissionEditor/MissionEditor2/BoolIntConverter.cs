using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace MissionEditor2
{
    class BoolIntConverter : IValueConverter
    {
        #region IValueConverter Members


        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Convert(value, targetType);
        }

        private static object Convert(object value, Type targetType)
        {
            if (targetType == typeof(int)) return (bool)value ? 1 : 0;
            if (targetType == typeof(int?)) return (bool)value ? new Nullable<int>(1) : new Nullable<int>(0);
            if (targetType == typeof(bool)) return (int)value == 0 ? true : false;
            if (targetType == typeof(bool?)) return (int)value == 0 ? new Nullable<bool>(true) : new Nullable<bool>(false);
            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Convert(value, targetType);
        }

        #endregion
    }
}
