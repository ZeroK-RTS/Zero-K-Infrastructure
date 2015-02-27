﻿using System;
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
                if (Program.NotifySection.Contains(this)) timerLabel.Text = ZkData.Utils.PrintTimeRemaining((int)deadline.Subtract(DateTime.Now).TotalSeconds);
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
            if (pw != null && !string.IsNullOrEmpty(tas.MyUser.Faction) && tas.MyUser.Level >= GlobalConst.MinPlanetWarsLevel && tas.MyUser.EffectiveElo >= GlobalConst.MinPlanetWarsElo)
            {
                if (pw.Mode == PwMatchCommand.ModeType.Clear) Program.NotifySection.RemoveBar(this);
                else
                {
                    deadline = DateTime.Now.AddSeconds(pw.DeadlineSeconds);
                    timerLabel.Text = ZkData.Utils.PrintTimeRemaining(pw.DeadlineSeconds);

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
                if (Program.SpringScanner.HasResource(opt1.Map)) tas.Say(SayPlace.User, GlobalConst.NightwatchName, "!" + opt1.PlanetID, true);
                else tas.Say(SayPlace.Channel, tas.MyUser.Faction, string.Format("wants to play {0}, but lacks the map..", opt1.PlanetID), true);
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
            container.Title = "PlanetWars match maker";
            container.TitleTooltip = "You need at least two people to attack a planet and at least one to defend a planet";

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