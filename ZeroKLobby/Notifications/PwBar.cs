using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using LobbyClient;
using PlasmaDownloader;

namespace ZeroKLobby.Notifications
{
    public partial class PwBar: UserControl, INotifyBar
    {
        NotifyBarContainer container;
        Label headerLabel = new Label();
        readonly TasClient tas;
        readonly Label timerLabel = new Label();
        Timer timer;
        int deadline;
        public PwBar()
        {
            InitializeComponent();
            tas = Program.TasClient;
            timerLabel.AutoSize = true;
            headerLabel.AutoSize = true;

            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += (sender, args) =>
            {
                deadline--;
                if (Program.NotifySection.Contains(this)) timerLabel.Text = PlasmaShared.Utils.PrintTimeRemaining(deadline);
            };
            timer.Start();


            pnl.Controls.Add(timerLabel);
            pnl.Controls.Add(headerLabel);

            tas.Extensions.JsonDataReceived += (sender, e) =>
            {
                var pw = e as PwMatchCommand;
                if (pw != null)
                {
                    if (pw.Mode == PwMatchCommand.ModeType.Clear) Program.NotifySection.RemoveBar(this);
                    else
                    {
                        deadline = pw.DeadlineSeconds;
                        timerLabel.Text = PlasmaShared.Utils.PrintTimeRemaining(pw.DeadlineSeconds);
                        if (pw.Mode == PwMatchCommand.ModeType.Attack)
                        {
                            foreach (var c in pnl.Controls.OfType<Button>().ToList()) pnl.Controls.Remove(c);
                            if (pw.Options == null || pw.Options.Count == 0)
                            {
                                headerLabel.Text = "Your turn - choose a planet on galaxy map and select attack";
                            }
                            else
                            {
                                headerLabel.Text = "Join planet attack";
                                
                                foreach (var opt in pw.Options)
                                {
                                    Program.Downloader.GetResource(DownloadType.MAP, opt.Map);

                                    var but = new Button() { Text = string.Format("{0} [{1}/{2}]", opt.PlanetName, opt.Count, opt.Needed), AutoSize = true };
                                    PwMatchCommand.VoteOption opt1 = opt;
                                    but.Click += (s2, ev) => tas.Say(TasClient.SayPlace.Channel, tas.MyUser.Faction, "!" + opt1.PlanetID, true);
                                    pnl.Controls.Add(but);
                                }
                            }
                        }
                        else if (pw.Mode == PwMatchCommand.ModeType.Defend)
                        {
                            foreach (var c in pnl.Controls.OfType<Button>().ToList()) pnl.Controls.Remove(c);
                            var opt = pw.Options.First();
                            headerLabel.Text = "Join planet defense";
                            
                            var but = new Button() { Text = string.Format("{0} [{1}/{2}]", opt.PlanetName, opt.Count, opt.Needed), AutoSize = true};
                            PwMatchCommand.VoteOption opt1 = opt;
                            but.Click += (s2, ev) => tas.Say(TasClient.SayPlace.Channel, tas.MyUser.Faction, "!" + opt1.PlanetID, true);
                            pnl.Controls.Add(but);
                        }

                        Program.NotifySection.AddBar(this);
                    }
                }
            };
        }

        public void AddedToContainer(NotifyBarContainer container)
        {
            this.container = container;
            container.btnDetail.Enabled = false;
            container.btnDetail.Text = "PlanetWars";
            //container.btnDetail.Visible = false;
        }

        public void CloseClicked(NotifyBarContainer container)
        {
            Program.NotifySection.RemoveBar(this);
        }

        public void DetailClicked(NotifyBarContainer container) {}

        public Control GetControl()
        {
            return this;
        }
    }
}