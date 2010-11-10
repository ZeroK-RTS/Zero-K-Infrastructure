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
using System.Windows.Navigation;
using System.Windows.Shapes;
using CMissionLib.Conditions;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for UnitBuiltOnGhostConditionControl.xaml
	/// </summary>
	public partial class UnitBuiltOnGhostConditionControl : UserControl
	{
		public UnitBuiltOnGhostConditionControl()
		{
			InitializeComponent();
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			var collection = ((UnitBuiltOnGhostCondition)DataContext).Groups;
			groupsList.BindCollection(collection);
		}
	}
}
