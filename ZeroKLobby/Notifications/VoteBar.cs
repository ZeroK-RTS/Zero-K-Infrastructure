using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using LobbyClient;
using ZkData;

namespace ZeroKLobby.Notifications
{
    public partial class VoteBar: UserControl, INotifyBar
    {
        NotifyBarContainer container;
        readonly TasClient tas;
        bool isSpad = false;

        public VoteBar() {
            InitializeComponent();
            tas = Program.TasClient;
            tas.PreviewSaid += (sender, e) =>
                {
                    var args = e.Data;
                    if ((tas.MyBattle != null && args.Place == TasSayEventArgs.Places.Battle && args.UserName == tas.MyBattle.Founder.Name) ||
                         (args.Place == TasSayEventArgs.Places.Channel && args.UserName == GlobalConst.NightwatchName))
                    {
                        //SPRINGIE's
                        if(args.Text.StartsWith("Poll:"))  //SPRINGIE start & end & mid of vote (OFFICIAL, COMPLETE)
                        {
                            var lid = args.Text.LastIndexOf("[");

                            if (lid != -1) {
                                var question = args.Text.Substring(6, lid - 6);
                                var data = args.Text.Substring(lid + 1);
                                if (data.Contains("END:")) Program.NotifySection.RemoveBar(this);
                                else {
                                    if (!Program.NotifySection.Bars.Contains(this)) {
                                        Program.NotifySection.AddBar(this);
                                        if (tas.MyBattleStatus == null || (!tas.MyUser.IsInGame && !tas.MyBattleStatus.IsSpectator)) Program.MainWindow.NotifyUser("chat/battle", string.Format("Poll: {0}", question), true, true);
                                    }
                                    else e.Cancel = true; // vote bar already visible, dont spam vote text again

                                    lbQuestion.Text = question; //add vote text into linklabel
                                    lbQuestion.Links.Clear(); //remove all link (convert to normal text?)
                                    foreach (Match match in Regex.Matches(question, @"((mailto|spring|http|https|ftp|ftps)\://(\S+))")) //find map link
                                        lbQuestion.Links.Add(match.Groups[1].Index, match.Groups[1].Length); //activate link for map link

                                    var m2 = Regex.Match(data, "!y=([0-9]+)/([0-9]+), !n=([0-9]+)/([0-9]+)");
                                    if (m2.Success) {
                                        var yes = int.Parse(m2.Groups[1].Value);
                                        var yesMax = int.Parse(m2.Groups[2].Value);
                                        var no = int.Parse(m2.Groups[3].Value);
                                        var noMax = int.Parse(m2.Groups[4].Value);
                                        lbYes.Text = string.Format("{0}/{1}", yes, yesMax);
                                        lbNo.Text = string.Format("{0}/{1}", no, noMax);
                                        pbYes.Maximum = yesMax;
                                        pbYes.Value = yes;
                                        pbNo.Maximum = noMax;
                                        pbNo.Value = no;
                                    }
                                }
                            }
                        }
                        //SPAD's
                        else if (args.Text.Contains("vote for command") || args.Text.StartsWith("Vote for command"))  //SPAD, at start & end of vote
                        {
                            var lid = args.Text.LastIndexOf('"');
                        
                            if (lid != -1)
                            {
                                var openingVoteText = args.Text.IndexOf('"');
                                var question = args.Text.Substring(openingVoteText + 1, lid - openingVoteText-1);
                                var data = args.Text.Substring(openingVoteText + 2);
                                if (data.Contains("passed") || data.Contains("failed")) { Program.NotifySection.RemoveBar(this); isSpad = false; }
                                else
                                {
                                    if (!Program.NotifySection.Bars.Contains(this))
                                    {
                                        Program.NotifySection.AddBar(this); isSpad = true;
                                        if (tas.MyBattleStatus == null || (!tas.MyUser.IsInGame && !tas.MyBattleStatus.IsSpectator)) Program.MainWindow.NotifyUser("chat/battle", string.Format("Poll: {0}", question), true, true);
                                    }

                                    lbQuestion.Text = "Poll: "+ question; //add vote text into linklabel
                                    lbQuestion.Links.Clear(); //remove all link (convert to normal text?)
                                    foreach (Match match in Regex.Matches(question, @"((mailto|spring|http|https|ftp|ftps)\://(\S+))")) //find map link
                                        lbQuestion.Links.Add(match.Groups[1].Index, match.Groups[1].Length); //activate link for map link

                                    lbYes.Text = string.Format("{0}/{1}", 1, 1);
                                    lbNo.Text = string.Format("{0}/{1}", 0, 1);
                                    pbYes.Maximum = 1;
                                    pbYes.Value = 1;
                                    pbNo.Maximum = 1;
                                    pbNo.Value = 0;
                                }
                            }
                        }
                        else if (isSpad == true) //SPAD at mid of vote event
                        {
                            if (args.Text.StartsWith("Vote in progress: ")) //SPAD, update vote count
                            {
                                var lid = args.Text.LastIndexOf("[");

                                if (lid != -1)
                                {
                                    var data = args.Text.Substring(lid + 1);
                                    var m1 = Regex.Match(data, "y:([0-9]+)/([0-9]+)");
                                    var m2 = Regex.Match(data, "n:([0-9]+)/([0-9]+)");
                                    if (m1.Success)
                                    {
                                        if (!Program.NotifySection.Bars.Contains(this)) //readd SPAD vote bar if was closed manually before vote finished
                                        {
                                            Program.NotifySection.AddBar(this);
                                            if (tas.MyBattleStatus == null || (!tas.MyUser.IsInGame && !tas.MyBattleStatus.IsSpectator)) Program.MainWindow.NotifyUser("chat/battle", string.Format("Poll: {0}", "?"), true, true);
                                        }
                                        var yes = int.Parse(m1.Groups[1].Value);
                                        var yesMax = int.Parse(m1.Groups[2].Value);
                                        var no = int.Parse(m2.Groups[1].Value);
                                        var noMax = int.Parse(m2.Groups[2].Value);
                                        lbYes.Text = string.Format("{0}/{1}", yes, yesMax);
                                        lbNo.Text = string.Format("{0}/{1}", no, noMax);
                                        pbYes.Maximum = yesMax;
                                        pbYes.Value = yes;
                                        pbNo.Maximum = noMax;
                                        pbNo.Value = no;
                                    }
                                }
                            }
                            //SPAD, sudden vote cancellation
                            else if (args.Text.StartsWith("Vote cancelled, launching game...")  //vote cancelled when game launch
                                || (args.Text.StartsWith("Game starting, cancelling \"") && args.Text.Contains("\" vote")) //vote cancelled when game starting
                                || (args.Text.StartsWith("Cancelling \"") && args.Text.Contains("\" vote")))  //vote cancelled by veto power
                            { Program.NotifySection.RemoveBar(this); isSpad = false; }
                        }
                    }
                };
            tas.BattleClosed += (sender, args) => { Program.NotifySection.RemoveBar(this); isSpad = false; };
        }

        public void AddedToContainer(NotifyBarContainer container) {
            this.container = container;
            container.btnDetail.Enabled = false;
            container.btnDetail.Text = "Vote";
        }

        public void CloseClicked(NotifyBarContainer container) {
            Program.NotifySection.RemoveBar(this);
        }

        public void DetailClicked(NotifyBarContainer container) {}

        public Control GetControl() {
            return this;
        }

        void btnNo_Click(object sender, EventArgs e) {
            if (isSpad) tas.Say(TasClient.SayPlace.Battle, "", "!vote n", false);
            else tas.Say(TasClient.SayPlace.Battle, "", "!n", false);
        }

        void btnYes_Click(object sender, EventArgs e) {
            if (isSpad) tas.Say(TasClient.SayPlace.Battle, "", "!vote y", false);
            else tas.Say(TasClient.SayPlace.Battle, "", "!y", false);
        }

        void lbQuestion_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            Utils.OpenWeb(lbQuestion.Text.Substring(e.Link.Start, e.Link.Length), true); //open map's webpage
        }
    }
}