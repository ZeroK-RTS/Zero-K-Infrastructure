using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using PlasmaDownloader;

namespace ZeroKLobby.Notifications
{
  /// <summary>
  /// Interaction logic for MissionBar.xaml
  /// </summary>
  public partial class ReplayBar: UserControl
  {
    readonly string demoUrl;
    readonly string engineVersion;
    readonly string mapName;
    readonly string modName;


    public ReplayBar(string demoUrl, string modName, string mapName, string engineVersion)
    {
      this.demoUrl = demoUrl;
      this.modName = modName;
      this.mapName = mapName;
      this.engineVersion = engineVersion;
      InitializeComponent();
    }


    void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
      label1.Content = string.Format("Starting replay {0} - please wait", demoUrl);

      var waitHandles = new List<EventWaitHandle>();

      var downMod = Program.Downloader.GetResource(DownloadType.MOD, modName);
      if (downMod != null) waitHandles.Add(downMod.WaitHandle);

      var downMap = Program.Downloader.GetResource(DownloadType.MAP, mapName);
      if (downMap != null) waitHandles.Add(downMap.WaitHandle);

      var downDemo = Program.Downloader.GetResource(DownloadType.DEMO, demoUrl);
      if (downDemo != null) waitHandles.Add(downDemo.WaitHandle);

      var downEngine = Program.Downloader.GetAndSwitchEngine(engineVersion);
      if (downEngine != null) waitHandles.Add(downEngine.WaitHandle);

      PlasmaShared.Utils.StartAsync(() =>
        {
          if (waitHandles.Any()) WaitHandle.WaitAll(waitHandles.ToArray());

          if ((downMod != null && downMod.IsComplete == false) || (downMap != null && downMap.IsComplete == false) ||
              (downDemo != null && downDemo.IsComplete == false) || (downEngine != null && downEngine.IsComplete == false))
          {
            Program.MainWindow.InvokeFunc(() =>
              {
                label1.Content = string.Format("Download of {0} failed", demoUrl);
                btnCancel.IsEnabled = true;
              });
            return;
          }

          var path = Utils.MakePath(Program.SpringPaths.WritableDirectory, "demos", new Uri(demoUrl).Segments.Last());
          try
          {
            Process.Start(Program.SpringPaths.Executable, string.Format("\"{0}\"", path));
            Program.MainWindow.InvokeFunc(() => Program.NotifySection.RemoveBar(this));
          }
          catch (Exception ex)
          {
            Trace.TraceError("Error starting replay: {0}", ex);
            Program.MainWindow.InvokeFunc(() =>
              {
                label1.Content = string.Format("Error starting replay {0}", demoUrl);
                btnCancel.IsEnabled = true;
              });
          }
        });
    }

    void btnCancel_Click(object sender, RoutedEventArgs e)
    {
      Program.NotifySection.RemoveBar(this);
    }
  }
}