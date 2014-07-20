using System.Windows;
using System.Windows.Controls;
using CMissionLib.Conditions;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for UnitDestroyedConditionControl.xaml
	/// </summary>
	public partial class UnitDestroyedConditionControl : UserControl
	{
        public UnitDestroyedConditionControl()
		{
			InitializeComponent();
		}

		void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			var condition = (UnitDestroyedCondition) MainWindow.Instance.CurrentLogic;
			groupBox.BindCollection(condition.Groups);
		}
	}
}