using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace MissionEditor2
{
    public class CsvConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return String.Join(",", ((IEnumerable<string>)value).ToArray());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new ObservableCollection<string>(((string)value).Split(','));
        }
    }
}
