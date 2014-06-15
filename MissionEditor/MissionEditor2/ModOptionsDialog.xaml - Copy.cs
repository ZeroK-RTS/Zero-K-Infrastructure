using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CMissionLib;

namespace MissionEditor2
{
    /// <summary>
    /// Interaction logic for ModOptionsDialog.xaml
    /// </summary>
    public partial class ModOptionsDialog : Window
    {
        public Mission Mission { get; private set; }

        public ModOptionsDialog()
        {
            InitializeComponent();
            Mission = MainWindow.Instance.Mission;
            DataContext = Mission;
            OptionList.ItemsSource = Mission.ModOptions;
        }

        void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        void OptionList_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var selected = OptionList.SelectedItem.ToString();
            var option = Array.Find(Mission.Mod.Options, x => x.Name == selected);
            var desc = option.Description + "\nType: " + option.Type;
            if (option.Type == CMissionLib.UnitSyncLib.OptionType.Number)
            {
                desc = desc + "\nMin: " + option.Min;
                desc = desc + "\nMax: " + option.Max;
            }
            else
            desc = desc + "\nDefault: " + option.Default;
            Description.Text = desc;
            string val;
            Mission.ModOptions.TryGetValue(option.Key, out val);
            Value.Text = val;
        }

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
		}
    }
}