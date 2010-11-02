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
using CMissionLib.Actions;
using CMissionLib.Conditions;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for ModifyScoreControl.xaml
	/// </summary>
	public partial class ModifyScoreControl : UserControl
	{
		ModifyScoreAction action;

		public ModifyScoreControl()
		{
			InitializeComponent();
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			action = (ModifyScoreAction)MainWindow.Instance.CurrentLogic;
			playerList.BindCollection(action.Players);
		}
	}
}
