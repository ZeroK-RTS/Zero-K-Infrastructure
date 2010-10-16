using System.Windows;
using System.Windows.Controls;
using CMissionLib.Conditions;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for UnitIsVisibleConditionControl.xaml
	/// </summary>
	public partial class UnitIsVisibleConditionControl : UserControl
	{
		public UnitIsVisibleConditionControl()
		{
			InitializeComponent();
		}

		void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			var condition = (UnitIsVisibleCondition) MainWindow.Instance.CurrentLogic;
			playerBox.BindCollection(condition.Players);
			groupBox.BindCollection(condition.Groups);
		}
	}
}