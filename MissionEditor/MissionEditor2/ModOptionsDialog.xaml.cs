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
        }

        void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
		}
    }
}