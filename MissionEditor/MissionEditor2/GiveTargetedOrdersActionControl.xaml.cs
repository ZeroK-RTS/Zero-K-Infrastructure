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
using CMissionLib;
using CMissionLib.Actions;

namespace MissionEditor2
{
	/// <summary>
    /// Interaction logic for GiveTargetedOrdersActionControl.xaml
	/// </summary>
	public partial class GiveTargetedOrdersActionControl : UserControl
	{
        GiveTargetedOrdersAction action;

        public GiveTargetedOrdersActionControl()
		{
			InitializeComponent();
		}

        void CreateNewOrder(Point position)
        {
            var selectedItem = (ListBoxItem)orderTypeListBox.SelectedItem;
            var orderTypeName = (string)selectedItem.Content;
            IOrder newOrder;
            switch (orderTypeName)
            {
                case "Attack":
                    newOrder = new AttackOrder(0, 0);
                    break;
                case "Guard":
                    //newOrder = new GuardOrder(0, 0);
                    break;
                case "Repair":
                    //newOrder = new RepairOrder(0, 0);
                    break;
                default:
                    throw new Exception("Ordertype not expected: " + orderTypeName);
            }
            //action.Orders.Add(newOrder);
        }

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			var action = (GiveTargetedOrdersAction) MainWindow.Instance.CurrentLogic;
			orderedGroupBox.BindCollection(action.Groups);
			targetGroupBox.BindCollection(action.TargetGroups);

		}
	}
}
