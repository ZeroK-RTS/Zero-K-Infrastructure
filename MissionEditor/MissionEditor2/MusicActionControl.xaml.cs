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
using Microsoft.Win32;

namespace MissionEditor2
{
	/// <summary>
	/// Interaction logic for MusicActionControl.xaml
	/// </summary>
	public partial class MusicActionControl : UserControl
	{
		public MusicActionControl()
		{
			InitializeComponent();
		}

		void MusicButtonLoaded(object sender, RoutedEventArgs e)
		{
			var button = (Button)e.Source;
			button.Click += delegate
			{
				var filter = "Ogg Files (*.OGG)|*.OGG";
				var dialog = new OpenFileDialog { Filter = filter, RestoreDirectory = true };
				if (dialog.ShowDialog() == true)
				{
					var action = (MusicAction)DataContext;
					action.TrackPath = dialog.FileName;
				}
			};
		}
	}
}
