using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CMissionLib;

namespace MissionEditor2
{
    /// <summary>
    /// Interaction logic for MissionSettingsDialog.xaml
    /// </summary>
    public partial class MissionSettingsDialog : Window
    {
    	Mission mission;
    	// todo: refresh boxes when mod changes
        public MissionSettingsDialog()
        {
            InitializeComponent();

        	mission = MainWindow.Instance.Mission;
        	DataContext = mission;
			foreach (var unit in mission.DisabledUnits)
			{
				var unitDef = mission.Mod.UnitDefs.FirstOrDefault(u => u.Name == unit);
				if (unitDef != null)
				{
					UnitBox.SelectedItems.Add(unitDef);
				}
			}
			UnitBox.SelectedItems.Clear();
			WidgetsBox.BindCollection(mission.DisabledWidgets);
			GadgetsBox.BindCollection(mission.DisabledGadgets);
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
			var player = (Player)((Button)e.Source).DataContext;
			var picker = new Microsoft.Samples.CustomControls.ColorPickerDialog { StartingColor = player.Color, Owner = this };
			var result = picker.ShowDialog();
			if (result == true)
			{
				player.Color = picker.SelectedColor;
			}
        }

		private void MapButton_Click(object sender, RoutedEventArgs e)
		{
			new MapSelectionDialog().ShowDialog();
		}

		private void ModButton_Click(object sender, RoutedEventArgs e)
		{
			new ModSelectionDialog().ShowDialog();
		}

		private void UnitBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			foreach (var item in e.AddedItems) mission.DisabledUnits.Add(item.ToString());
			foreach (var item in e.RemovedItems)
			{
				while (mission.DisabledUnits.Remove(item.ToString())) { }
			}
		}

		private void NewPlayerButton_Click(object sender, RoutedEventArgs e)
		{
			var player = new Player();
			mission.Players.Add(player);
			((INotifyPropertyChanged)player).PropertyChanged += (s, eventArgs) => // fixme: leak
			{
				if (eventArgs.PropertyName == "Alliance")
				{
					mission.RaisePropertyChanged("Alliances");
				}
			};
		}
    }
}
