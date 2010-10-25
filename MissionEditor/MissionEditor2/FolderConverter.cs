using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;
using CMissionLib;

namespace MissionEditor2
{
	class FolderConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var list = new List<object>();
			var mission = MainWindow.Instance.Mission;
			list.AddRange(mission.Triggers.Where(t => t.Folder == null));
			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
