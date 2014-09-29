using System;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using LobbyClient;

namespace ZeroKLobby
{
    public partial class AdvertiserWindow: UserControl, INavigatable
    {
        int delay;
        DateTime lastSaid = DateTime.MinValue;
        Random random;
        string sayLine;
        TasClient tas;
        System.Timers.Timer timer;

        public AdvertiserWindow()
        {
            Paint += AdvertiserWindow_Enter;
        }

        private void AdvertiserWindow_Enter(object sender, EventArgs e)
        {
            Paint -= AdvertiserWindow_Enter;
            InitializeComponent();
            Start();
        }

        void GetBestBattle(out Battle bestBattle, out int battleSize)
        {
            battleSize = 0;
            bestBattle = null;
            foreach (var v in tas.ExistingBattles)
            {
                var gamers = v.Value.NonSpectatorCount;
                if (!v.Value.ModName.Contains("Zero-K")) continue;

                var ing = 0;
                foreach (var s in v.Value.Users) if (s.LobbyUser.IsInGame) ing++;

                if (!v.Value.Founder.IsInGame && ing < 2 && !v.Value.IsLocked && (v.Value.Password == "0" || v.Value.Password == "*") &&
                    gamers < v.Value.MaxPlayers && gamers > battleSize)
                {
                    battleSize = gamers;
                    bestBattle = v.Value;
                }
            }
        }

        void Spam()
        {
            

            Battle best;
            int bestp;
            GetBestBattle(out best, out bestp);

            sayLine = "";

            if (best != null && bestp > 0 && best.MaxPlayers > 4)
            {
                var gamers = best.NonSpectatorCount;

                if (gamers < 1) return;

                if (gamers + 1 == best.MaxPlayers && best.MaxPlayers >= 4) sayLine = "Last one for ZK. ";
                else if (gamers == 5 || gamers == 7 || gamers == 9) sayLine = "One more for ZK. ";
                else 
                {
                    var prefixes = Program.Conf.AdPreffix.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    sayLine = prefixes[random.Next(prefixes.Length)];
                }

                var au = Program.Conf.AdLines.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (au.Length <= 0) return;
                sayLine += au[random.Next(au.Length)];

                var suffixes = Program.Conf.AdSuffix.Split(new char[] { '\r', '\n' });
                if (suffixes.Length > 0) sayLine += suffixes[random.Next(suffixes.Length)];

                var channels = Program.Conf.AdChannels.Split(new char[] { ',' });

                foreach (var c in channels)
                {
                    if (!tas.JoinedChannels.ContainsKey(c)) tas.JoinChannel(c);
                    Thread.Sleep(random.Next(8000) + 2000);
                    if (DateTime.Now.Subtract(lastSaid).TotalSeconds > 5 && random.Next(3)!=0) tas.Say(TasClient.SayPlace.Channel, c, sayLine, false);
                }
            }
        }

        void Start()
        {
            random = new Random();
            tbAdlines.Text = Program.Conf.AdLines.Replace("\n","\r\n");
            tbChannels.Text = Program.Conf.AdChannels;
            tbSuffix.Text = Program.Conf.AdSuffix.Replace("\n", "\r\n");
            tas = Program.TasClient;
            timer = new System.Timers.Timer();
            tbPrefix.Text = Program.Conf.AdPreffix.Replace("\n", "\r\n");
            tbDelay.Text = Program.Conf.AdDelays.ToString();
            timer.Interval = 52345;
            timer.Elapsed += (sender, args) =>
                {
                    delay++;
                    if (delay < Program.Conf.AdDelays) return;
                    else delay = 0;
                    if (!cbActive.Checked || tas.MyUser == null || tas.MyUser.IsAway || tas.MyUser.IsInGame) return; // if idle for more like 5 mins dont spam
                    if (random.Next(3)==0) new Thread(Spam).Start();
                };
            timer.Start();
            tas.Said += (sender, args) => { if (args.Text != sayLine) lastSaid = DateTime.Now; };
        }

        void UpdateAdLines()
        {
            using (var wc = new WebClient())
            {
                try
                {
                    string l;
                    try
                    {
                        l = wc.DownloadString("http://zero-k.googlecode.com/svn/wiki/AdLines.wiki");
                        var lin = l.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        Program.Conf.AdLines = "";
                        foreach (var s in lin)
                        {
                            if (s.StartsWith(" * "))
                            {
                                var t = s.Substring(3);
                                if (t != "") Program.Conf.AdLines += t + "\r\n";
                            }
                        }
                    }
                    catch {}
                    tbAdlines.Text = Program.Conf.AdLines;
                }
                catch (WebException) {}
            }
        }

        public string PathHead { get { return "advertiser"; } }

        public bool TryNavigate(params string[] path)
        {
            return path.Length > 0 && path[0] == PathHead;
        }

        public bool Hilite(HiliteLevel level, string path)
        {
            return false;
        }

        public string GetTooltip(params string[] path)
        {
            return null;
        }

        public void Reload() {
            
        }

        public bool CanReload { get { return false; } }

        public bool IsBusy { get { return false; } }

        void btnNow_Click(object sender, EventArgs e)
        {
            Spam();
        }

        void button2_Click(object sender, EventArgs e)
        {
            UpdateAdLines();
        }

        void tbAdlines_TextChanged(object sender, EventArgs e)
        {
            Program.Conf.AdLines = tbAdlines.Text;
        }

        void tbChannels_TextChanged(object sender, EventArgs e)
        {
            Program.Conf.AdChannels = tbChannels.Text;
        }

        void tbDelay_TextChanged(object sender, EventArgs e)
        {
            Program.Conf.AdDelays = int.Parse(tbDelay.Text);
        }

        void tbSuffix_TextChanged(object sender, EventArgs e)
        {
            Program.Conf.AdSuffix = tbSuffix.Text;
        }

        private void tbPrefix_TextChanged(object sender, EventArgs e)
        {
            Program.Conf.AdPreffix = tbPrefix.Text;
        }
    }
}