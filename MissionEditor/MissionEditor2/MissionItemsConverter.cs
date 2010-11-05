using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using CMissionLib;

namespace MissionEditor2
{
	class MissionItemsConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null) return null;
			var triggers = MainWindow.Instance.Mission.Triggers;
			var regions = MainWindow.Instance.Mission.Regions;
			return triggers.Cast<object>().Concat(regions);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
