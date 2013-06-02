using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using PlasmaDownloader;

namespace ZeroKLobby.MapDownloader
{
  public partial class DownloaderTab: UserControl, INavigatable
  {
    public DownloaderTab()
    {
      InitializeComponent();
      if (Process.GetCurrentProcess().ProcessName == "devenv") return; // detect design mode, workaround for non-working this.DesignMode 

      Load += Rapid_Load;
      Disposed += DownloaderTab_Disposed;
    }

    void DownloaderTab_Disposed(object sender, EventArgs e)
    {
      Program.Downloader.PackagesChanged -= Downloader_PackagesChanged;
      Program.Downloader.SelectedPackagesChanged -= Downloader_SelectedPackagesChanged;
    }

    void AddNode(string name)
    {
      if (string.IsNullOrEmpty(name)) return;

      var curNodes = tvAvailable.Nodes;
      var key = "";
      foreach (var part in name.Split(':'))
      {
        key += part;
        var ret = curNodes.Find(key, false);
        if (ret.Length == 0)
        {
          var newNode = curNodes.Add(key, part);
          newNode.Tag = key;
          curNodes = newNode.Nodes;
        }
        else curNodes = ret.First().Nodes;

        key += ":";
      }
    }

    void UpdateAvailablePackages()
    {
      tvAvailable.BeginUpdate();
      tvAvailable.Nodes.Clear();
      foreach (var repo in Program.Downloader.PackageDownloader.Repositories) foreach (var key in repo.VersionsByTag.Keys) AddNode(key);
      tvAvailable.EndUpdate();
    }

    void UpdateSelectedPackages()
    {
      lbInstalled.BeginUpdate();
      lbInstalled.Items.Clear();
      foreach (var item in new List<string>(Program.Downloader.PackageDownloader.SelectedPackages)) lbInstalled.Items.Add(item);
      lbInstalled.EndUpdate();
    }

    public string PathHead { get { return "rapid"; } }

    public bool TryNavigate(bool reload, params string[] path)
    {
      return path.Length > 0 && path[0] == PathHead;
    }

    public bool Hilite(HiliteLevel level, params string[] path)
    {
      return false;
    }

    public string GetTooltip(params string[] path)
    {
      return null;
    }

    void Downloader_PackagesChanged(object sender, EventArgs e)
    {
      if (InvokeRequired) Invoke(new EventHandler(Downloader_PackagesChanged));
      else
      {
        Trace.TraceInformation("Rapid packages updated");
        UpdateAvailablePackages();
      }
    }

    void Downloader_SelectedPackagesChanged(object sender, EventArgs e)
    {
      if (InvokeRequired) Invoke(new EventHandler(Downloader_SelectedPackagesChanged));
      else
      {
        Trace.TraceInformation("Selected packages changed");
        UpdateSelectedPackages();
      }
    }

    void Rapid_Load(object sender, EventArgs e)
    {
      Program.Downloader.PackagesChanged += Downloader_PackagesChanged;
      Program.Downloader.SelectedPackagesChanged += Downloader_SelectedPackagesChanged;

      UpdateAvailablePackages();
      UpdateSelectedPackages();
    }

    void btnReload_Click(object sender, EventArgs e)
    {
      PlasmaShared.Utils.StartAsync(Program.Downloader.PackageDownloader.LoadMasterAndVersions);
    }

    void lbInstalled_DoubleClick(object sender, EventArgs e)
    {
      if (lbInstalled.SelectedIndex >= 0) Program.Downloader.PackageDownloader.DeselectPackage(lbInstalled.Items[lbInstalled.SelectedIndex].ToString());
    }

    void tvAvailable_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
    {
      if (e.Node.Nodes.Count == 0) Program.Downloader.GetResource(DownloadType.UNKNOWN, e.Node.Tag.ToString());
    }
  }
}