using System;
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
using ZkData;

namespace GalaxyDesigner
{
	/// <summary>
	/// Interaction logic for PlanetDialog.xaml
	/// </summary>
	public partial class PlanetDialog : Window
	{
		PlanetDrawing pd;
		public Planet planet { get; set; }

		public PlanetDialog(PlanetDrawing pd, IEnumerable<Resource> maps, IEnumerable<StructureType> structureTypes)
		{
			this.pd = pd;
			this.planet = pd.Planet;
			InitializeComponent();
			lbName.Text = planet.Name;
            maps = maps.OrderBy(x => x.MetadataName);
			foreach (var mn in maps) cbMaps.Items.Add(new ComboBoxItem() { Content = mn.InternalName, Tag = mn.ResourceID, IsSelected = mn.ResourceID == this.planet.MapResourceID });

			foreach (var s in structureTypes)
			{
				lbStructures.Items.Add(new ListBoxItem() {Content =  s.Name, Tag = s.StructureTypeID, IsSelected = planet.PlanetStructures.Any(y=>y.StructureTypeID==s.StructureTypeID) });
			}
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			//planet.Detach();
			planet.Name = lbName.Text;
			//planet.MapResourceID = (int?)((ComboBoxItem)cbMaps.SelectedItem).Tag;
			var mid = (int?)((ComboBoxItem)cbMaps.SelectedItem).Tag;
			
			planet.Resource = new ZkDataContext().Resources.Single(x => x.ResourceID == mid);
			planet.MapResourceID = planet.Resource.ResourceID;
			planet.PlanetStructures.Clear();
			foreach (ListBoxItem s in lbStructures.Items)
			{
				if (s.IsSelected) planet.PlanetStructures.Add(new PlanetStructure() { StructureTypeID = (int)s.Tag});
			}
			pd.UpdateData(lbStructures.Items.Cast<ListBoxItem>().Where(x => x.IsSelected).Select(x => x.Content.ToString()));
			DialogResult = true;
			Close();
		}

		void Cancel_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
