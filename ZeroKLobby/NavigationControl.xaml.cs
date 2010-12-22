using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Deployment.Application;
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
    INavigatable CurrentINavigatable { get { return GetINavigatableFromControl(tabControl.SelectedContent); } }

    NavigationStep CurrentPage
    {
      get { return _currentPage; }
      set
      {
        _currentPage = value;
        PropertyChanged(this, new PropertyChangedEventArgs("CurrentPage"));
        PropertyChanged(this, new PropertyChangedEventArgs("Path"));

        var steps = Path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries); // todo cleanup
        var navigable = tabControl.Items.OfType<Object>().Select(GetINavigatableFromControl).FirstOrDefault(x => x != null && x.PathHead == steps[0]);
        if (navigable != null) navigable.Hilite(HiliteLevel.None, steps);
      }
    }

    NavigationStep _currentPage;
    readonly Stack<NavigationStep> backStack = new Stack<NavigationStep>();
    readonly Stack<NavigationStep> forwardStack = new Stack<NavigationStep>();
    readonly List<string> lastPaths = new List<string>();
    public WebBrowser Browser { get { return browserControl.WebBrowser; } }
    public ChatTab2 ChatTab { get { return chatTab; } }
    public static NavigationControl Instance { get; private set; }
    public bool IsBrowserTabSelected { get { return tabControl.SelectedContent is BrowserControl; } }

    public string Path
    {
      get { return CurrentPage != null ? CurrentPage.ToString() : string.Empty; }
      set
      {
        if (value == "http://zero-k.info/lobby/Zero-K.application")
        {
          return; // this URL happens if you start installer while another copy is running. In this case dont change to this path
        }

        if (value.ToLower().StartsWith("spring://")) value = value.Substring(9);
        var parts = value.Split('@');
        for (var i = 1; i < parts.Length; i++)
        {
          var action = parts[i];
          ActionHandler.PerformAction(action);
        }
        value = parts[0];

        if (CurrentPage != null && CurrentPage.ToString() == value) return; // we are already there, no navigation needed
        lastPaths.Add(value);

        var step = GoToPage(value.Split('/'));
        if (step != null)
        {
          if (CurrentPage != null && CurrentPage.ToString() != value) backStack.Push(CurrentPage);
          CurrentPage = step;
          //forwardStack.Clear();
        }
      }
    }

    public NavigationControl()
    {
      Instance = this;
      InitializeComponent();
      if (Program.Conf.LimitedMode)
      {
        btnWidgets.Visibility= Visibility.Collapsed;
        btnRapid.Visibility = Visibility.Collapsed;
      }
    }


    public WindowsFormsHost GetWindowsFormsHostOfCurrentTab()
    {
      if (tabControl.SelectedContent is ChatTab2)
      {
        var ct = (ChatTab2)tabControl.SelectedContent;
        return ct.winformsHost;
      }

      return tabControl.SelectedContent as WindowsFormsHost;
    }

    public bool HilitePath(string navigationPath, HiliteLevel hiliteLevel)
    {
      if (string.IsNullOrEmpty(navigationPath)) return false;
      var steps = navigationPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
      var navigable = tabControl.Items.OfType<Object>().Select(GetINavigatableFromControl).FirstOrDefault(x => x != null && x.PathHead == steps[0]);
      if (navigable != null) return navigable.Hilite(hiliteLevel, steps);
      else return false;
    }

    public void NavigateBack()
    {
      if (CanGoBack) GoBack();
    }

    public void NavigateForward()
    {
      if (CanGoForward) GoForward();
    }

    INavigatable GetINavigatableFromControl(object obj)
    {
      if (obj is TabItem) obj = ((TabItem)obj).Content;
      if (obj is WindowsFormsHost) obj = ((WindowsFormsHost)obj).Child;
      return obj as INavigatable;
    }

    string GetLastPathStartingWith(string startString)
    {
      string path;
      for (var i = lastPaths.Count - 1; i >= 0; i--) if (lastPaths[i].StartsWith(startString)) return lastPaths[i];
      return startString;
    }

    void GoBack()
    {
      if (forwardStack.Count == 0|| forwardStack.Peek().ToString() != CurrentPage.ToString()) forwardStack.Push(CurrentPage);
      CurrentPage = backStack.Pop();
      GoToPage(CurrentPage.Path);
    }

    void GoForward()
    {
      if (backStack.Count == 0 || backStack.Peek().ToString() != CurrentPage.ToString()) backStack.Push(CurrentPage);
      CurrentPage = forwardStack.Pop();
      GoToPage(CurrentPage.Path);
    }

    NavigationStep GoToPage(string[] path) // todo cleanup
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

    void BattleListPage_Click(object sender, RoutedEventArgs e)
    {
      Path = GetLastPathStartingWith("battles");
    }

    void ChatPage_Click(object sender, RoutedEventArgs e)
    {
      Path = GetLastPathStartingWith("chat");
    }

    void DownloaderPage_Click(object sender, RoutedEventArgs e)
    {
      Path = GetLastPathStartingWith("rapid");
    }


    void MapPage_Click(object sender, RoutedEventArgs e)
    {
      Path = GetLastPathStartingWith("http://zero-k.info/Maps.mvc");
    }


    void MissionsPage_Click(object sender, RoutedEventArgs e)
    {
      Path = GetLastPathStartingWith("http://zero-k.info/Missions.mvc");
    }

    void SettingsPage_Click(object sender, RoutedEventArgs e)
    {
      Path = GetLastPathStartingWith("settings");
    }

    void TabItem_MouseUp(object sender, RoutedEventArgs e)
    {
      var navigatable = GetINavigatableFromControl(e.Source);
      if (navigatable == null) return;
      Path = navigatable.PathHead;
      /*var step = new NavigationStep { Path = new[] { navigatable.PathHead } };
      if (CurrentPage != null) backStack.Push(CurrentPage);
      CurrentPage = step;
      forwardStack.Clear();*/
    }

    void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      if (string.IsNullOrEmpty(Path)) Path = "http://zero-k.info/Missions.mvc";
    }

    void WidgetsPage_Click(object sender, RoutedEventArgs e)
    {
      Path = GetLastPathStartingWith("widgets");
    }

    void backButton_Click(object sender, RoutedEventArgs e)
    {
      NavigateBack();
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
  }
}