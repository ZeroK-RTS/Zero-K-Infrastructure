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
	/// Interaction logic for MusicLoopActionControl.xaml
	/// </summary>
	public partial class MusicLoopActionControl : UserControl
	{
		public MusicLoopActionControl()
		{
			InitializeComponent();
		}

		void MusicIntroButtonLoaded(object sender, RoutedEventArgs e)
		{
			var button = (Button)e.Source;
			button.Click += delegate
			{
				var filter = "Ogg Files (*.OGG)|*.OGG";
				var dialog = new OpenFileDialog { Filter = filter, RestoreDirectory = true };
				if (dialog.ShowDialog() == true)
				{
					var action = (MusicLoopAction)DataContext;
					action.TrackIntroPath = dialog.FileName;
				}
			};
		}
        void MusicLoopButtonLoaded(object sender, RoutedEventArgs e)
        {
            var button = (Button)e.Source;
            button.Click += delegate
            {
                var filter = "Ogg Files (*.OGG)|*.OGG";
                var dialog = new OpenFileDialog { Filter = filter, RestoreDirectory = true };
                if (dialog.ShowDialog() == true)
                {
                    var action = (MusicLoopAction)DataContext;
                    action.TrackLoopPath = dialog.FileName;
                }
            };
        }
	}
}
