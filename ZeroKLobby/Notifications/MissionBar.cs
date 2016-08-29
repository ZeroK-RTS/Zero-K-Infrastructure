using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LobbyClient;
using PlasmaDownloader;
using ZkData;
using ZkData.UnitSyncLib;

namespace ZeroKLobby.Notifications
{
    public partial class MissionBar: ZklNotifyBar
    {
        private readonly string missionName;

        public MissionBar(string missionName)
        {
            this.missionName = missionName;
            InitializeComponent();
        }

        private void MissionBar_Load(object sender, EventArgs e)
        {
            label1.Text = string.Format("Starting mission {0} - please wait", missionName);

            var down = Program.Downloader.GetResource(DownloadType.MISSION, missionName);
            if (down == null)
            {
                //okay Mission exist, but lets check for dependency!
                down = Program.Downloader.GetDependenciesOnly(missionName);
            }

            var engine = Program.Downloader.GetEngine(GlobalConst.DefaultEngineOverride);

            ZkData.Utils.StartAsync(() =>
            {
                var metaWait = new EventWaitHandle(false, EventResetMode.ManualReset);
                Mod modInfo = null;
                Program.SpringScanner.MetaData.GetModAsync(missionName,
                    mod =>
                    {
                        if (!mod.IsMission)
                        {
                            Program.MainWindow.InvokeFunc(() =>
                            {
                                label1.Text = string.Format("{0} is not a valid mission", missionName);
                                //container.btnStop.Enabled = true;
                            });
                        }

                        else modInfo = mod;

                        metaWait.Set();
                    },
                    error =>
                    {
                        Program.MainWindow.InvokeFunc(() =>
                        {
                            label1.Text = string.Format("Download of metadata failed: {0}", error.Message);
                            //container.btnStop.Enabled = true;
                        });
                        metaWait.Set();
                    });
                //if (down != null) WaitHandle.WaitAll(new WaitHandle[] { down.WaitHandle, metaWait });
                //else metaWait.WaitOne();

                var waitHandles = new List<EventWaitHandle>();

                waitHandles.Add(metaWait);
                if (down != null) waitHandles.Add(down.WaitHandle);
                if (engine != null) waitHandles.Add(engine.WaitHandle);

                if (waitHandles.Any()) WaitHandle.WaitAll(waitHandles.ToArray());

                if ((down != null && down.IsComplete == false) || (engine != null && engine.IsComplete == false) || modInfo == null)
                {
                    Program.MainWindow.InvokeFunc(() =>
                    {
                        label1.Text = string.Format("Download of {0} failed", missionName);

                        //container.btnStop.Enabled = true;
                    });
                }

                if (modInfo != null && (down == null || down.IsComplete == true) && (engine == null || engine.IsComplete == true))
                {
                    if (Utils.VerifySpringInstalled())
                    {
                        var spring = new Spring(Program.SpringPaths);
                        spring.RunLocalScriptGame(modInfo.MissionScript, GlobalConst.DefaultEngineOverride);

                        var cs = GlobalConst.GetContentService();
                        cs.NotifyMissionRun(Program.Conf.LobbyPlayerName, missionName);

                        spring.SpringExited += (o, args) => RecordMissionResult(spring, modInfo);
                    }
                    Program.MainWindow.InvokeFunc(() => Program.NotifySection.RemoveBar(this));
                }
            });
        }

        private static void RecordMissionResult(Spring spring, Mod modInfo)
        {
            if (spring.Context.GameEndedOk && !spring.Context.IsCheating)
            {
                Trace.TraceInformation("Submitting score for mission " + modInfo.Name);
                try
                {
                    var service = GlobalConst.GetContentService();
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            service.SubmitMissionScore(Program.Conf.LobbyPlayerName,
                                ZkData.Utils.HashLobbyPassword(Program.Conf.LobbyPlayerPassword),
                                modInfo.Name,
                                spring.Context.MissionScore ?? 0,
                                spring.Context.MissionFrame/30,
                                spring.Context.MissionVars);
                        }
                        catch (Exception ex)
                        {
                            Trace.TraceError("Error sending score: {0}", ex);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Error sending mission score: {ex}");
                }
            }
        }
    }
}