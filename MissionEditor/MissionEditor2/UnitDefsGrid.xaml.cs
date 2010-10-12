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
using System.Windows.Navigation;
using System.Windows.Shapes;
using CMissionLib.UnitSyncLib;
using CMissionLib;

namespace MissionEditor2
{
    /// <summary>
    /// Interaction logic for UnitDefsGrid.xaml
    /// </summary>
    public partial class UnitDefsGrid : UserControl
    {
        public UnitDefsGrid()
        {
            InitializeComponent();
        }

        private void Find_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new StringRequest { Title = "Insert text to search." };
            if (!(dialog.ShowDialog().GetValueOrDefault())) return;
            var item = Grid.ItemsSource.Cast<UnitInfo>().FirstOrDefault(ud => ud.Name.ToLower().Contains(dialog.TextBox.Text.ToLower()));
            if (item == null) return;
            Grid.SelectedItem = item;
            Grid.ScrollIntoView(item);
        }
    }
}
