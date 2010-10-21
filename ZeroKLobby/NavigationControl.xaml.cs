using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby
{
	/// <summary>
	/// Interaction logic for NavigationControl.xaml
	/// </summary>
	public partial class NavigationControl: UserControl, INotifyPropertyChanged
	{
		bool CanGoBack { get { return backStack.Any(); } }

		bool CanGoForward { get { return forwardStack.Any(); } }

		NavigationStep CurrentPage
		{
			get { return _currentPage; }
			set
			{
				_currentPage = value;
				PropertyChanged(this, new PropertyChangedEventArgs("CurrentPage"));
				PropertyChanged(this, new PropertyChangedEventArgs("Path"));
			}
		}

		public string Path
		{
			get { return CurrentPage != null ? CurrentPage.ToString() : string.Empty; }
			set
			{
				var step = GoToPage(value.Split('/'));
				if (step != null)
				{
					if (CurrentPage != null) backStack.Push(CurrentPage);
					CurrentPage = step;
					forwardStack.Clear();
				}
			}
		}

		NavigationStep _currentPage;
		Stack<NavigationStep> backStack = new Stack<NavigationStep>();
		Stack<NavigationStep> forwardStack = new Stack<NavigationStep>();
		public ChatTab ChatTab { get { return chatTab; } }
		public static NavigationControl Instance { get; private set; }

		public NavigationControl()
		{
			Instance = this;
			InitializeComponent();
		}

		INavigatable GetINavigatableFromControl(object obj)
		{
			if (obj is TabItem) obj = ((TabItem)obj).Content;
			if (obj is WindowsFormsHost) obj = ((WindowsFormsHost)obj).Child;
			return obj as INavigatable;
		}

		void GoBack()
		{
			forwardStack.Push(CurrentPage);
			CurrentPage = backStack.Pop();
			GoToPage(CurrentPage.Path);
		}

		void GoForward()
		{
			backStack.Push(CurrentPage);
			CurrentPage = forwardStack.Pop();
			GoToPage(CurrentPage.Path);
		}

		NavigationStep GoToPage(string[] path)
		{
			foreach (var item in tabControl.Items)
			{
				var navigatable = GetINavigatableFromControl(item);
				if (navigatable != null && navigatable.TryNavigate(path))
				{
					tabControl.SelectedItem = item;
					return new NavigationStep { Path = path };
				}
			}
			return null;
		}

		public event PropertyChangedEventHandler PropertyChanged = delegate { };

		void TabItem_MouseUp(object sender, RoutedEventArgs e)
		{
			var navigatable = GetINavigatableFromControl(e.Source);
			if (navigatable == null) return;
			var step = new NavigationStep { Path = new[] { navigatable.PathHead } };
			if (CurrentPage != null) backStack.Push(CurrentPage);
			CurrentPage = step;
			forwardStack.Clear();
		}

		void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			Path = "start";
		}

		void backButton_Click(object sender, RoutedEventArgs e)
		{
			if (CanGoBack) GoBack();
		}

		void forwardButton_Click(object sender, RoutedEventArgs e)
		{
			if (CanGoForward) GoForward();
		}

		class NavigationStep
		{
			public string[] Path { get; set; }

			public override string ToString()
			{
				return string.Join("/", Path);
			}
		}

		private void StartPage_Click(object sender, RoutedEventArgs e)
		{
			Path = "start";
		}

		private void BattleListPage_Click(object sender, RoutedEventArgs e)
		{
			Path = "battles";
		}

		private void ChatPage_Click(object sender, RoutedEventArgs e)
		{
			Path = "chat";
		}

		private void HelpPage_Click(object sender, RoutedEventArgs e)
		{
			Path = "help";
		}

		private void WidgetsPage_Click(object sender, RoutedEventArgs e)
		{
			Path = "widgets";
		}

		private void DownloaderPage_Click(object sender, RoutedEventArgs e)
		{
			Path = "downloader";
		}

		private void SettingsPage_Click(object sender, RoutedEventArgs e)
		{
			Path = "settings";
		}
	}
}