using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PlanetWars
{
    public class VisibilityConverter: IValueConverter
    {
        static object BothWays(Type targetType, object value)
        {
            if (targetType == typeof(Visibility) && value is int) return ToVisibility((int)value);
            if (targetType == typeof(bool) && value is Visibility) return ToBool((Visibility)value);
            if (targetType == typeof(Visibility) && value is bool) return ToVisibility((bool)value);
            if (targetType == typeof(Visibility) && value is bool?) return ToVisibility((bool?)value);
            if (targetType == typeof(bool?) && value is Visibility) return ToNullableBool((Visibility)value);
            throw new NotSupportedException();
        }

        static bool ToBool(Visibility value)
        {
            var b = value == Visibility.Visible;
            return b;
        }

        static bool? ToNullableBool(Visibility value)
        {
            bool? b = value == Visibility.Visible;
            return b;
        }

        static Visibility ToVisibility(int value)
        {
            return value != 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        static Visibility ToVisibility(bool value)
        {
            return value ? Visibility.Visible : Visibility.Collapsed;
        }

        static Visibility ToVisibility(bool? value)
        {
            return value.HasValue && value.Value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return BothWays(targetType, value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return BothWays(targetType, value);
        }
    }
}