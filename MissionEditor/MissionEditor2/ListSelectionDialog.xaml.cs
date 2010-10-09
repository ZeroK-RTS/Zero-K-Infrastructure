using System;
using System.Collections;
using System.Collections.Generic;
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
using CMissionLib.UnitSyncLib;
using MissionEditor2.Properties;

namespace MissionEditor2
{
    /// <summary>
    /// Interaction logic for ListSelectionDialog.xaml
    /// </summary>
    public abstract partial class ListSelectionDialog: Window
    {

    	protected abstract IEnumerable<string> GetOptions();
		protected abstract object GetItem(string itemName);
    	protected abstract void SetItem(object item);

        public ListSelectionDialog()
        {
            InitializeComponent();
        }

        private void List_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DialogResult = true;
            Close();
        }

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			ProgressBar.Visibility = System.Windows.Visibility.Visible;
			Utils.InvokeInNewThread(delegate
			{
				var options = GetOptions();
				this.Invoke(delegate
				{
					List.ItemsSource = options;
					ProgressBar.Visibility = System.Windows.Visibility.Hidden;
				});
			});
		}

		private void OKButton_Click(object sender, RoutedEventArgs e)
		{
			var selectedItemName = (string)List.SelectedItem;
			OKButton.IsEnabled = false;
			CancelButton.IsEnabled = false;
			ProgressBar.Visibility = System.Windows.Visibility.Visible;
			Utils.InvokeInNewThread(delegate
			{
				var item = GetItem(selectedItemName);
				this.Invoke(delegate
				{
					SetItem(item);
					Close();
				});
			});
		}
    }

	class ModSelectionDialog : ListSelectionDialog
	{
		public ModSelectionDialog()
		{
			Title = "Select Mod";
		}

		protected override IEnumerable<string> GetOptions()
		{
				
			using (var unitSync = new UnitSync(Settings.Default.SpringPath))
			{
				return unitSync.GetModNames();
			}
		}

		protected override object GetItem(string itemName)
		{
			using (var unitSync = new UnitSync(Settings.Default.SpringPath))
			{
				return WelcomeDialog.LoadMod(unitSync, itemName);
			}
		}

		protected override void SetItem(object item)
		{
			MainWindow.Instance.Mission.Mod = (Mod)item;
		}
	}

	class MapSelectionDialog : ListSelectionDialog
	{
		public MapSelectionDialog()
		{
			Title = "Select Map";
		}

		protected override IEnumerable<string> GetOptions()
		{

			using (var unitSync = new UnitSync(Settings.Default.SpringPath))
			{
				return unitSync.GetMapNames();
			}
		}

		protected override object GetItem(string itemName)
		{
			using (var unitSync = new UnitSync(Settings.Default.SpringPath))
			{
				return WelcomeDialog.LoadMap(unitSync, itemName);
			}
		}

		protected override void SetItem(object item)
		{
			MainWindow.Instance.Mission.Map = (Map)item;
		}
	}
}
