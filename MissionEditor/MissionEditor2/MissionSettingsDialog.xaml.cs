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
using CMissionLib.Actions;
using Microsoft.Win32;

namespace MissionEditor2
{
    /// <summary>
    /// Interaction logic for MissionSettingsDialog.xaml
    /// </summary>
    public partial class MissionSettingsDialog : Window
    {
    	public Mission Mission { get; private set; }
    	// todo: refresh boxes when mod changes
        public MissionSettingsDialog()
        {
            InitializeComponent();
        	Mission = MainWindow.Instance.Mission;
        	DataContext = Mission;
			WidgetsBox.BindCollection(Mission.DisabledWidgets);
			GadgetsBox.BindCollection(Mission.DisabledGadgets);
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

		private void AIBox_Loaded(object sender, RoutedEventArgs e)
		{
			var comboBox = (ComboBox) e.Source;
			comboBox.ItemsSource = Mission.Mod.AllAis;
		}

    	private void ModButton_Click(object sender, RoutedEventArgs e)
		{
			new ModSelectionDialog().ShowDialog();
		}

		private void NewPlayerButton_Click(object sender, RoutedEventArgs e)
		{
			var player = new Player();
			Mission.Players.Add(player);
			((INotifyPropertyChanged)player).PropertyChanged += (s, eventArgs) => // fixme: leak
			{
				if (eventArgs.PropertyName == "Alliance")
				{
					Mission.RaisePropertyChanged("Alliances");
				}
			};
		}

		private void SelectImageButton_Click(object sender, RoutedEventArgs e)
		{
			var filter = "Image Files(*.BMP;*.JPG;*.PNG)|*.BMP;*.JPG;*.PNG|All files (*.*)|*.*";
			var dialog = new OpenFileDialog { Filter = filter, RestoreDirectory = true };
			if (dialog.ShowDialog() == true)
			{
				Mission.ImagePath = dialog.FileName;
			}
		}

		private void ClearImageButton_Click(object sender, RoutedEventArgs e)
		{
			Mission.ImagePath = null;
		}
    }
}
