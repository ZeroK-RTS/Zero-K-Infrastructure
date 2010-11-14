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
	/// Interaction logic for SoundActionControl.xaml
	/// </summary>
	public partial class SoundActionControl : UserControl
	{
		public SoundActionControl()
		{
			InitializeComponent();
		}

		void SoundButtonLoaded(object sender, RoutedEventArgs e)
		{
			var button = (Button)e.Source;
			button.Click += delegate
			{
				var filter = "Ogg Files (*.OGG)|*.OGG|Wave Files (*.WAV)|*.WAV";
				var dialog = new OpenFileDialog { Filter = filter, RestoreDirectory = true };
				if (dialog.ShowDialog() == true)
				{
					var action = (SoundAction)DataContext;
					action.SoundPath = dialog.FileName;
				}
			};
		}
	}
}
