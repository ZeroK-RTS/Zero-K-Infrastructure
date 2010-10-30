using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using CMissionLib;
using CMissionLib.Actions;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

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
			var picker = new Microsoft.Samples.CustomControls.ColorPickerDialog {StartingColor = player.Color, Owner = this };
			var result = picker.ShowDialog();
			if (result == true)
			{
				player.Color = picker.SelectedColor;
			}
        }

		private void MapButton_Click(object sender, RoutedEventArgs e)
		{
			new MapSelectionDialog { Owner = this }.ShowDialog();
		}

		private void AIBox_Loaded(object sender, RoutedEventArgs e)
		{
			var comboBox = (ComboBox) e.Source;
			comboBox.ItemsSource = Mission.Mod.AllAis;
		}

    	private void ModButton_Click(object sender, RoutedEventArgs e)
		{
			new ModSelectionDialog { Owner = this }.ShowDialog();
		}

		private void newPlayerButton_Click(object sender, RoutedEventArgs e)
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

		private void BrowseButton_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new FolderBrowserDialog();
			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				Mission.ContentFolderPath = dialog.SelectedPath;
			}
		}

		private void removePlayerButton_Click(object sender, RoutedEventArgs e)
		{
			var selectedPlayer = (Player) playerGrid.SelectedItem;
			if (selectedPlayer == null) return;
			Mission.Players.Remove(selectedPlayer);
		}
    }
}
