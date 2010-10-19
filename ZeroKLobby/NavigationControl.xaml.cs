using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby
{
	/// <summary>
	/// Interaction logic for NavigationControl.xaml
	/// </summary>
	public partial class NavigationControl: UserControl
	{
		public ChatTab ChatTab { get { return chatTab; } }

		// todo: refresh settings config when settings tab is selected

		public NavigationControl()
		{
			InitializeComponent();
		}

		void UserControl_Loaded(object sender, RoutedEventArgs e) {}
	}
}