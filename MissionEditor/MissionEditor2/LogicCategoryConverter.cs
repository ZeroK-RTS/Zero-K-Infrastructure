using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using CMissionLib;
using CMissionLib.Conditions;

namespace MissionEditor2
{
    public class LogicCategoryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var logic = (TriggerLogic) value;
            var trigger = MainWindow.Instance.Mission.FindLogicOwner(logic);
            return new KeyValuePair<string, Trigger>(logic is Condition ? "Conditions" : "Actions", trigger);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}