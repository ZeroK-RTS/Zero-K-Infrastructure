using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using LobbyClient;
using PlasmaDownloader;
using ZkData;
using ZkData.UnitSyncLib;

namespace ZeroKLobby.Notifications
{
    public partial class MissionBar : UserControl, INotifyBar
    {
        readonly string missionName;

        public MissionBar(string missionName)
        {
            this.missionName = missionName;
            InitializeComponent();
        }


        NotifyBarContainer container;

        public void AddedToContainer(NotifyBarContainer container) {
            this.container = container;
            container.Title = "Loading a singleplayer mission";
            container.TitleTooltip = "Please await resource download";
        }

        public void CloseClicked(NotifyBarContainer container) {
            Program.NotifySection.RemoveBar(this);
        }

        public void DetailClicked(NotifyBarContainer container) {
        }

        public Control GetControl() {
            return this;
        }

        private void MissionBar_Load(object sender, EventArgs e)
        {
            label1.Text = string.Format("Starting mission {0} - please wait", missionName);

            var down = Program.Downloader.GetResource(DownloadType.MOD, missionName);
            if (down==null)
            {   //okay Mission exist, but lets check for dependency!
                down = Program.Downloader.GetDependenciesOnly(missionName);
            }

            var engine = Program.Downloader.GetAndSwitchEngine(Program.SpringPaths.SpringVersion);

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
                                                                       container.btnStop.Enabled = true;
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
                                                                   container.btnStop.Enabled = true;
                                                               });
                                                               metaWait.Set();
                                                           });
                //if (down != null) WaitHandle.WaitAll(new WaitHandle[] { down.WaitHandle, metaWait });
                //else metaWait.WaitOne();
                
                var waitHandles = new List<EventWaitHandle>();
                
                waitHandles.Add(metaWait);
                if (down != null)  waitHandles.Add(down.WaitHandle);
                if (engine != null) waitHandles.Add(engine.WaitHandle);
                
                if (waitHandles.Any()) WaitHandle.WaitAll(waitHandles.ToArray());

                if ((down != null && down.IsComplete == false) || (engine != null && engine.IsComplete == false) || modInfo==null)
                {
                    Program.MainWindow.InvokeFunc(() =>
                    {
                        label1.Text = string.Format("Download of {0} failed", missionName);

                        container.btnStop.Enabled = true;
                    });
                }

                if (modInfo != null && (down == null || down.IsComplete == true) && (engine == null || engine.IsComplete == true))
                {
                    if (Utils.VerifySpringInstalled())
                    {
                        var spring = new Spring(Program.SpringPaths);
                        spring.RunLocalScriptGame(modInfo.MissionScript);

                        var cs = GlobalConst.GetContentService();
                        cs.NotifyMissionRun(Program.Conf.LobbyPlayerName, missionName);
                    }
                    Program.MainWindow.InvokeFunc(() => Program.NotifySection.RemoveBar(this));
                }
            });
        }
    }
}
