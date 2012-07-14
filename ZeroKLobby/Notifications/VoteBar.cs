using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using LobbyClient;

namespace ZeroKLobby.Notifications
{
    public partial class VoteBar : UserControl, INotifyBar
    {
        NotifyBarContainer container;
        private TasClient tas;

        public VoteBar()
        {
            InitializeComponent();
            tas = Program.TasClient;
            tas.PreviewSaid += (sender, e) =>
                {
                var args = e.Data;
                if (tas.MyBattle != null && args.Place == TasSayEventArgs.Places.Battle && args.UserName == tas.MyBattle.Founder.Name && args.Text.StartsWith("Poll:")) {
                    var lid = args.Text.LastIndexOf("[");
                    
                    if (lid != -1) {
                        var question = args.Text.Substring(6, lid - 6);
                        var data = args.Text.Substring(lid + 1);
                        if (data.Contains("END:")) {
                            Program.NotifySection.RemoveBar(this);
                        } else
                        {
                            if (!Program.NotifySection.Bars.Contains(this))
                            {
                                Program.NotifySection.AddBar(this);
                                if (!tas.MyUser.IsInGame && !tas.MyBattleStatus.IsSpectator) Program.MainWindow.NotifyUser("chat/battle", string.Format("Poll: {0}", question), true, true);
                            } else {
                                e.Cancel = true; // vote bar already visible, dont spam vote text again
                            }

                            lbQuestion.Text = question;
                            var m2 = Regex.Match(data, "!y=([0-9]+)/([0-9]+), !n=([0-9]+)/([0-9]+)");
                            if (m2.Success) {
                                var yes = int.Parse(m2.Groups[1].Value);
                                var yesMax = int.Parse(m2.Groups[2].Value);
                                var no = int.Parse(m2.Groups[3].Value);
                                var noMax = int.Parse(m2.Groups[4].Value);
                                lbYes.Text = string.Format("{0}/{1}",  yes, yesMax);
                                lbNo.Text = string.Format("{0}/{1}", no, noMax);
                                pbYes.Maximum = yesMax;
                                pbYes.Value = yes;
                                pbNo.Maximum = noMax;
                                pbNo.Value = no;
                            }
                        }
                    }
                }
            };
            tas.BattleClosed += (sender, args) =>
            {
                Program.NotifySection.RemoveBar(this);
            };
        }

        public void AddedToContainer(NotifyBarContainer container) {
            
            this.container = container;
            container.btnDetail.Enabled = false;
            container.btnDetail.Text = "Vote";
        }

        public void CloseClicked(NotifyBarContainer container) {
            Program.NotifySection.RemoveBar(this);
        }

        public void DetailClicked(NotifyBarContainer container) {
            
            
        }

        public Control GetControl() {
            return this;
        }

        private void btnYes_Click(object sender, EventArgs e)
        {
            tas.Say(TasClient.SayPlace.Battle, "","!y", false);
        }

        private void btnNo_Click(object sender, EventArgs e)
        {
            tas.Say(TasClient.SayPlace.Battle, "", "!n", false);
        }
    }
}
