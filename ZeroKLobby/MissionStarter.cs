using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LobbyClient;
using PlasmaDownloader;
using PlasmaShared;
using ZeroKLobby.MicroLobby;
using ZeroKLobby.Notifications;
using ZkData;
using ZkData.UnitSyncLib;

namespace ZeroKLobby
{
    public class MissionStarter
    {
        public void StartMission(string missionName)
        {
            var down = Program.Downloader.GetResource(DownloadType.MISSION, missionName);
            if (down == null)
            {
                //okay Mission exist, but lets check for dependency!
                down = Program.Downloader.GetDependenciesOnly(missionName);
            }

            var engine = Program.Downloader.GetResource(DownloadType.ENGINE, Program.TasClient.ServerWelcome.Engine ?? GlobalConst.DefaultEngineOverride);

            var metaWait = new EventWaitHandle(false, EventResetMode.ManualReset);
            Mod modInfo = null;
            Program.MetaData.GetModAsync(missionName,
                mod =>
                {
                    if (!mod.IsMission)
                    {
                        Program.MainWindow.InvokeFunc(() => { WarningBar.DisplayWarning(string.Format("{0} is not a valid mission", missionName)); });
                    }

                    else modInfo = mod;

                    metaWait.Set();
                },
                error =>
                {
                    Program.MainWindow.InvokeFunc(() =>
                    {
                        WarningBar.DisplayWarning(string.Format("Download of metadata failed: {0}", error.Message));
                        //container.btnStop.Enabled = true;
                    });
                    metaWait.Set();
                });

            var downloads = new List<Download>() { down, engine }.Where(x => x != null).ToList();
            if (downloads.Count > 0)
            {
                var dd = new WaitDownloadDialog(downloads);
                if (dd.ShowDialog(Program.MainWindow) == DialogResult.Cancel)
                {
                    Program.MainWindow.InvokeFunc(() => Program.NotifySection.RemoveBar(this));
                    return;
                }
            }
            metaWait.WaitOne();

            var spring = new Spring(Program.SpringPaths);
            spring.RunLocalScriptGame(modInfo.MissionScript, Program.TasClient.ServerWelcome.Engine ?? GlobalConst.DefaultEngineOverride);
            var cs = GlobalConst.GetContentService();
            cs.Query(new NotifyMissionRun() {Login = Program.Conf.LobbyPlayerName, MissionName = missionName});
            spring.SpringExited += (o, args) => RecordMissionResult(spring, modInfo);
            Program.MainWindow.InvokeFunc(() => Program.NotifySection.RemoveBar(this));
        }

        public void StartScriptMission(string missionName)
        {
            var serv = GlobalConst.GetContentService();
            var profile = serv.Query(new GetScriptMissionDataRequest() { MissionName = missionName });
            var downloads = new List<Download>();
            downloads.Add(Program.Downloader.GetResource(DownloadType.RAPID, profile.ModName));
            downloads.Add(Program.Downloader.GetResource(DownloadType.MAP, profile.MapName));
            downloads.Add(Program.Downloader.GetResource(DownloadType.ENGINE, Program.TasClient.ServerWelcome.Engine ?? GlobalConst.DefaultEngineOverride));
            if (profile.ManualDependencies != null) foreach (var entry in profile.ManualDependencies) if (!string.IsNullOrEmpty(entry)) downloads.Add(Program.Downloader.GetResource(DownloadType.NOTKNOWN, entry));

            downloads = downloads.Where(x => x != null).ToList();

            if (downloads.Count > 0)
            {
                var dd = new WaitDownloadDialog(downloads);
                if (dd.ShowDialog(Program.MainWindow) == DialogResult.Cancel) return;
            }

            var spring = new Spring(Program.SpringPaths);
            var name = Program.Conf.LobbyPlayerName;
            if (string.IsNullOrEmpty(name)) name = "Player";

            spring.RunLocalScriptGame(
                profile.StartScript.Replace("%MOD%", profile.ModName).Replace("%MAP%", profile.MapName).Replace("%NAME%", name),
                Program.TasClient.ServerWelcome.Engine ?? GlobalConst.DefaultEngineOverride);
            serv.Query(new NotifyMissionRun() {Login = name, MissionName = profile.Name});
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
                            service.Query(new SubmitMissionScoreRequest()
                            {
                                Login = Program.Conf.LobbyPlayerName,
                                PasswordHash = ZkData.Utils.HashLobbyPassword(Program.Conf.LobbyPlayerPassword),
                                MissionName = modInfo.Name,
                                Score = spring.Context.MissionScore ?? 0,
                                GameSeconds = spring.Context.MissionFrame/30,
                                MissionVars = spring.Context.MissionVars
                            });
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