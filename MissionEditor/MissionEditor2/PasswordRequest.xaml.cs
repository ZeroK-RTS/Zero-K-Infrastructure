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
using System.Windows.Shapes;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for PasswordRequest.xaml
	/// </summary>
	public partial class PasswordRequest : Window
	{
		public PasswordRequest()
		{
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void TextBox_Loaded(object sender, RoutedEventArgs e)
		{
			PasswordBox.SelectAll();
		}
	}
}
