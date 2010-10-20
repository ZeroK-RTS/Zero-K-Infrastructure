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
			Instance = this;
			InitializeComponent();
		}

		void UserControl_Loaded(object sender, RoutedEventArgs e) {}

		public NavigationControl Instance { get; private set; }

		class NavigationStep
		{
			public string[] Path { get; set; }
			public string DisplayName { get; set; }
		}


		Stack<NavigationStep> backStack = new Stack<NavigationStep>();
		Stack<NavigationStep> forwardStack = new Stack<NavigationStep>();
		NavigationStep currentPage;


		bool CanGoBack
		{
			get { return backStack.Any(); }
		}

		bool CanGoForward
		{
			get { return forwardStack.Any(); }
		}

		void GoBack()
		{
			forwardStack.Push(currentPage);
			currentPage = backStack.Pop();
			GoToPage(currentPage.Path);
		}
		void GoForward()
		{
			backStack.Push(currentPage);
			currentPage = forwardStack.Pop();
			GoToPage(currentPage.Path);
		}

		NavigationStep GoToPage(string[] path)
		{
			var tabs = tabControl.Items.OfType<INavigatable>();
			foreach (var tab in tabs)
			{
				var pathHumanName = tab.TryNavigate(path);
				if (pathHumanName != null)
				{
					return new NavigationStep { DisplayName = pathHumanName, Path = path };
				}
			}
			return null;
		}

		void NavigateTo(params string[] path)
		{
			var step = GoToPage(path);
			if (step != null)
			{
				backStack.Push(currentPage);
				currentPage = step;
				forwardStack.Clear();
			}
		}
	}
}