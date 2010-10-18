using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace MissionEditor2
{
	class SpringSizeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{

			var footprint = (double)value;
			var mission = MainWindow.Instance.Mission;
			var ratio = mission.Map.Texture.Width/mission.Map.Size.Width/2;
			return footprint * ratio ;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var size = (double)value;
			var mission = MainWindow.Instance.Mission;
			var ratio = mission.Map.Texture.Width / mission.Map.Size.Width/2;
			return size /ratio;
		}
	}
}
