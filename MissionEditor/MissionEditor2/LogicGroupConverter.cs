using System;
using System.Globalization;
using System.Windows.Data;
using CMissionLib;

namespace MissionEditor2
{
    public class LogicGroupConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return MainWindow.Instance.Mission.FindLogicOwner((TriggerLogic) value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}