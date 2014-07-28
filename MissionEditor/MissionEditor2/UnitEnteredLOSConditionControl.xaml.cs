using System.Windows;
using System.Windows.Controls;
using CMissionLib.Conditions;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for UnitEnteredLOSConditionControl.xaml
	/// </summary>
	public partial class UnitEnteredLOSConditionControl : UserControl
	{
		public UnitEnteredLOSConditionControl()
		{
			InitializeComponent();
		}

		void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			var condition = (UnitEnteredLOSCondition) MainWindow.Instance.CurrentLogic;
			allyBox.BindCollection(condition.Alliances);
			groupBox.BindCollection(condition.Groups);
		}
	}
}