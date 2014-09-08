using System;
using System.Linq;
using System.Windows.Forms;
using LobbyClient;
using PlasmaDownloader;
using ZkData;

namespace ZeroKLobby.Notifications
{
    public partial class PwBar: UserControl, INotifyBar
    {
        NotifyBarContainer container;
        DateTime deadline;
        readonly Label headerLabel = new Label();
        readonly TasClient tas;
        Timer timer;
        readonly Label timerLabel = new Label();
        PwMatchCommand pw;

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
                if (Program.NotifySection.Contains(this)) timerLabel.Text = PlasmaShared.Utils.PrintTimeRemaining((int)DateTime.Now.Subtract(deadline).TotalSeconds);
            };
            timer.Start();

            pnl.Controls.Add(timerLabel);
            pnl.Controls.Add(headerLabel);

            tas.Extensions.JsonDataReceived += (sender, e) =>
            {
                var newPw = e as PwMatchCommand;
                if (newPw != null)
                {
                    pw = newPw;
                    UpdateGui();
                }
            };

            tas.MyExtensionsChanged += (sender, args) => UpdateGui();
        }

        void UpdateGui()
        {
            if (pw != null && tas.MyUser.Faction != null && tas.MyUser.Level >= GlobalConst.MinPlanetWarsLevel)
            {
                if (pw.Mode == PwMatchCommand.ModeType.Clear) Program.NotifySection.RemoveBar(this);
                else
                {
                    deadline = DateTime.Now.AddSeconds(pw.DeadlineSeconds);
                    timerLabel.Text = PlasmaShared.Utils.PrintTimeRemaining(pw.DeadlineSeconds);

                    if (pw.Mode == PwMatchCommand.ModeType.Attack)
                    {
                        headerLabel.Text = string.Format("{0} picks a planet to attack", pw.AttackerFaction);

                        foreach (Button c in pnl.Controls.OfType<Button>().ToList()) pnl.Controls.Remove(c);

                        foreach (PwMatchCommand.VoteOption opt in pw.Options)
                        {
                            Program.Downloader.GetResource(DownloadType.MAP, opt.Map);

                            var but = new Button { Text = string.Format("{0} [{1}/{2}]", opt.PlanetName, opt.Count, opt.Needed), AutoSize = true };
                            Program.ToolTip.SetMap(but, opt.Map);

                            if (pw.AttackerFaction == tas.MyUser.Faction) // NOTE this is for cases where nightwatch self faction info is delayed
                            {
                                AddButtonClick(opt, but);
                            }
                            else but.Enabled = false;
                            pnl.Controls.Add(but);
                        }
                    }
                    else if (pw.Mode == PwMatchCommand.ModeType.Defend)
                    {
                        PwMatchCommand.VoteOption opt = pw.Options.First();
                        headerLabel.Text = string.Format("{0} attacks planet {2}, {1} defends",
                            pw.AttackerFaction,
                            string.Join(",", pw.DefenderFactions),
                            opt.PlanetName);

                        foreach (Button c in pnl.Controls.OfType<Button>().ToList()) pnl.Controls.Remove(c);

                        var but = new Button { Text = string.Format("{0} [{1}/{2}]", opt.PlanetName, opt.Count, opt.Needed), AutoSize = true };
                        Program.ToolTip.SetMap(but, opt.Map);

                        if (pw.DefenderFactions.Contains(tas.MyUser.Faction)) // NOTE this is for cases where nightwatch self faction info is delayed
                        {
                            AddButtonClick(opt, but);
                        }
                        else but.Enabled = false;
                        pnl.Controls.Add(but);
                    }

                    Program.NotifySection.AddBar(this);
                }
            } else Program.NotifySection.RemoveBar(this);
        }

        void AddButtonClick(PwMatchCommand.VoteOption opt, Button but)
        {
            PwMatchCommand.VoteOption opt1 = opt;
            but.Click += (s2, ev) =>
            {
                if (Program.SpringScanner.HasResource(opt1.Map)) tas.Say(TasClient.SayPlace.Channel, tas.MyUser.Faction, "!" + opt1.PlanetID, true);
                else tas.Say(TasClient.SayPlace.Channel, tas.MyUser.Faction, string.Format("wants to play {0}, but lacks the map..", opt1.PlanetID), true);
            };
        }

        public void AddedToContainer(NotifyBarContainer container)
        {
            this.container = container;
            container.btnDetail.Enabled = false;
            container.btnDetail.Text = "PlanetWars";
            container.btnStop.Enabled = false;
            container.btnStop.Visible = false;
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