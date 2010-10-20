using System;
using System.Collections.Generic;
using System.Diagnostics;
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
	public partial class NavigationControl: UserControl
	{
		public ChatTab ChatTab { get { return chatTab; } }

		// todo: refresh settings config when settings tab is selected

		public NavigationControl()
		{
			Instance = this;
			InitializeComponent();
	
		}

		void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			NavigateTo("start");
		}

		public NavigationControl Instance { get; private set; }

		class NavigationStep
		{
			public string[] Path { get; set; }
			public override string ToString()
			{
				return string.Join("/", Path);
			}
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

		void NavigateTo(string path)
		{
			var step = GoToPage(path.Split('/'));
			if (step != null)
			{
				if (currentPage != null) backStack.Push(currentPage);
				currentPage = step;
				forwardStack.Clear();
			}
		}

		private void backButton_Click(object sender, RoutedEventArgs e)
		{
			if (CanGoBack) GoBack();
		}

		private void forwardButton_Click(object sender, RoutedEventArgs e)
		{
			if (CanGoForward) GoForward();
		}

		INavigatable GetINavigatableFromControl(object obj)
		{
			if (obj is TabItem)
			{
				obj = ((TabItem)obj).Content;
			}
			if (obj is WindowsFormsHost)
			{
				obj = ((WindowsFormsHost)obj).Child;
			}
			return obj as INavigatable;
		}

		private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{


		}

		private void TabItem_MouseUp(object sender, RoutedEventArgs e)
		{
			var navigatable = GetINavigatableFromControl(e.Source);
			if (navigatable == null) return;
			var step = new NavigationStep { Path = new[] { navigatable.PathHead } };
			if (currentPage != null) backStack.Push(currentPage);
			currentPage = step;
			forwardStack.Clear();
		}
	}
}