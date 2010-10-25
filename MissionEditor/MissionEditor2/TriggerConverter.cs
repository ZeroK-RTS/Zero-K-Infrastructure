using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using CMissionLib;

namespace MissionEditor2
{
	class TriggerConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var trigger = (Trigger)value;
			return new object[] {new ConditionsFolder(trigger) , new ActionsFolder(trigger), };
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException("Cannot perform reverse-conversion");
		}
	}
}
