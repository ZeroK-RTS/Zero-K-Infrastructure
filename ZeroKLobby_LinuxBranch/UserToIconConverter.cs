using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby
{
	class UserToIconConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is string)) throw new NotSupportedException();
			var userName = (string)value;
			var icon = TextImage.GetUserImage(userName);
			return icon.ToBitmapSource();
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
