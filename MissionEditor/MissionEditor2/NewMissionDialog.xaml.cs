using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MissionEditor2
{
    /// <summary>
    /// Interaction logic for NewMissionDialog.xaml
    /// </summary>
    public partial class NewMissionDialog : Window
    {
        public NewMissionDialog()
        {
            InitializeComponent();
        }

        void CheckReadiness()
        {
            OKButton.IsEnabled = !String.IsNullOrEmpty(NameBox.Text) && MapList.SelectedIndex >= 0 && ModList.SelectedIndex >= 0;
        }

        void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        void NameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (MapList == null || ModList == null || OKButton == null) {
                return;
            }
            CheckReadiness();
        }

        void MapList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CheckReadiness();
        }

        void ModList_MouseUp(object sender, MouseButtonEventArgs e)
        {
            CheckReadiness();
        }

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			CheckReadiness();
		}

		private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			try
			{
				Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
				e.Handled = true;
			}
			catch { }

		}
    }
}