using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Text.RegularExpressions;

namespace ZeroKLobby {
    public class IsPrefixValueConverter : IValueConverter {
        private string _prefix = "";
        private Regex regex = null; 
        public string Prefix {
            get { return _prefix; }
            set {
                _prefix = value;
                regex = null;
            }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            if (value == null) return false;

            if (!(value is string))
                throw new InvalidOperationException("Value must be of type 'string'.");

            if (regex == null)
                regex = new Regex("^" + Regex.Escape(_prefix) + "(?:/|$)", RegexOptions.IgnoreCase);

            return regex.IsMatch((string)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}
