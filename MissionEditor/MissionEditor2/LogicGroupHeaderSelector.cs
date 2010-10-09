using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows;
using System.Linq;
using System.Text;
using System.Windows.Data;
using CMissionLib;

namespace MissionEditor2
{
    public class LogicGroupHeaderSelector : DataTemplateSelector
    {
        DataTemplate GetResource(string resourceName)
        {
            return (DataTemplate)Application.Current.MainWindow.FindResource(resourceName);
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var group = (CollectionViewGroup)item;
            if (group.Name is CMissionLib.Trigger) return GetResource("triggerGroupTemplate");
            var kvp = (KeyValuePair<string, CMissionLib.Trigger>)group.Name;
            if (kvp.Key == "Conditions") return GetResource("conditionGroupTemplate");
            if (kvp.Key == "Actions") return GetResource("actionGroupTemplate");
            throw new Exception("group not recognized");
        }

    }
}
