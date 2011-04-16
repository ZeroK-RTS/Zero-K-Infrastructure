using System.Windows;
using ZkData;

namespace GalaxyDesigner
{
	/// <summary>
	/// Interaction logic for GalaxyDialog.xaml
	/// </summary>
	public partial class GalaxyDialog: Window
	{
		public int GalaxyNumber
		{
			get
			{
				int i;
				int.TryParse((string)cmbGal.SelectedValue, out i);
				return i;
			}
		}

		public GalaxyDialog()
		{
			InitializeComponent();
			var db = new ZkDataContext();
			cmbGal.Items.Add("as new");
			foreach (var g in db.Galaxies) cmbGal.Items.Add(g.GalaxyID.ToString());
		}

		void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		void btnOk_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}
	}
}