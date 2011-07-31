using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using ZkData;

namespace ZeroKLobby.MicroLobby
{
  /// <summary>
  /// Interaction logic for MissionControl.xaml
  /// </summary>
  public partial class BrowserControl: UserControl, INavigatable
  {
    int navigatedIndex = 0;
    readonly List<string> navigatedPlaces = new List<string>();
    bool navigating = false;
    string navigatingTo = null;

    public BrowserControl()
    {
      try
      {
        WindowsApi.InternetSetCookie(Config.BaseUrl, GlobalConst.LobbyAccessCookieName, "1");
      }
      catch (Exception ex)
      {
        Trace.TraceWarning("Unable to set ZK cookie:{0}", ex);
      }
      if (Process.GetCurrentProcess().ProcessName == "devenv") return;
      InitializeComponent();
    }


    public string PathHead { get { return "http://"; } }

    public bool TryNavigate(params string[] path)
    {
      var pathString = String.Join("/", path);
      if (!pathString.StartsWith(PathHead)) return false;
      //if (WebBrowser.Source != null && pathString == WebBrowser.Source.OriginalString) return true;

      if (navigatingTo == pathString) return true; // already navigating there

			/*
      if (navigatedIndex > 1 && navigatedPlaces[navigatedIndex - 2] == pathString)
      {
        navigatedIndex -= 2;
        WebBrowser.GoBack();
        return true;
      }*/

      //navigatingTo = pathString;
			try {
				WindowsApi.DeleteUrlCacheEntry(pathString);
			} catch (Exception ex)
			{
				Trace.TraceError("Error deleting cache entry: {0}",ex);
			}
			
    	WebBrowser.Navigate(pathString);

      return true;
    }

    public bool Hilite(HiliteLevel level, params string[] path)
    {
      return false;
    }

    public string GetTooltip(params string[] path)
    {
      throw new NotImplementedException();
    }

    void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      if (Process.GetCurrentProcess().ProcessName == "devenv") return;
    }

    void WebBrowser_Navigated(object sender, NavigationEventArgs e)
    {
      navigating = false;
      navigatingTo = null;
      if (navigatedIndex == navigatedPlaces.Count) navigatedPlaces.Add(e.Uri.ToString());
      else navigatedPlaces[navigatedIndex] = e.Uri.ToString();
      navigatedIndex++;
			Program.MainWindow.navigationControl.BusyLoading = false;
    }

    void WebBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
    {
      var alreadyNavigating = navigating == true; // if already navigating, its probably redirect from website
      navigating = true;
      if (navigatingTo != e.Uri.ToString())
      {
        navigatingTo = e.Uri.ToString();
        if (navigatingTo.Contains("@"))
        {
          e.Cancel = true;
          navigatingTo = null;
          navigating = false;
        }
        if (!alreadyNavigating || !navigating) Program.MainWindow.navigationControl.Path = Uri.UnescapeDataString(e.Uri.ToString());
      }
			if (navigating) Program.MainWindow.navigationControl.BusyLoading = true;
    }

    void webBrowser_Loaded(object sender, RoutedEventArgs e)
    {
      if (Process.GetCurrentProcess().ProcessName == "devenv") return;
      UrlSecurityZone.InternetSetFeatureEnabled(UrlSecurityZone.InternetFeaturelist.DISABLE_NAVIGATION_SOUNDS,
                                                UrlSecurityZone.SetFeatureOn.PROCESS,
                                                true);
      //UrlSecurityZone.InternetSetFeatureEnabled(UrlSecurityZone.InternetFeaturelist.OBJECT_CACHING, UrlSecurityZone.SetFeatureOn.PROCESS, false);
    }
  }
}