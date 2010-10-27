using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CMissionLib.UnitSyncLib;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for UnitDefsGrid.xaml
	/// </summary>
	public partial class UnitDefsGrid: UserControl
	{
		public UnitDefsGrid()
		{
			InitializeComponent();
		}

		public void GoToText(string text)
		{
			var item =
				Grid.ItemsSource.Cast<UnitInfo>().FirstOrDefault(
					ud => ud.Name.ToLower().Contains(text.ToLower()) || ud.FullName.ToLower().Contains(text.ToLower()));
			if (item == null) return;
			Grid.SelectedItem = item;
			Grid.ScrollIntoView(item);
		}

		void Find_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new StringRequest { Title = "Insert text to search.", Owner = MainWindow.Instance };
			if (!(dialog.ShowDialog().GetValueOrDefault())) return;
			GoToText(dialog.TextBox.Text);
		}
	}
}