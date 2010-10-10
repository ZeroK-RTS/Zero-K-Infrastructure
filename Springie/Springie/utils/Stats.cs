#region using

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using LobbyClient;
using PlanetWarsShared.Springie;
using PlasmaShared.UnitSyncLib;
using Springie.autohost;
using Springie.PlanetWars;

#endregion

namespace Springie
{
    public class Stats
    {
        public const string accountsFileName = "do_not_delete_me.xml";
        public const string gatherScript = "statsGather.php";
        public const string smurfScript = "smurfs.php";
        public const string statsScript = "stats.php";

        List<Account> accounts = new List<Account>();
        Battle battle;
        string password = "";
        Dictionary<string, EndGamePlayerInfo> players = new Dictionary<string, EndGamePlayerInfo>();
        Spring spring;
        DateTime startTime;

        static object locker = new object();

        protected string StatsUrl { get { return MainConfig.StatsUrlAddressReal; } }

        TasClient tas;

        protected string UserName
        {
            get
            {
                if (tas != null && !string.IsNullOrEmpty(tas.UserName)) return tas.UserName;
                else return ah.GetAccountName();
            }
        }

        AutoHost ah;
        public Stats(TasClient tas, Spring spring, AutoHost autoHost)
        {
            this.tas = tas;
            this.spring = spring;
            this.ah = autoHost;

            LoadAccounts();

            tas.LoginAccepted += tas_LoginAccepted;
            if (Program.main.Config.GargamelMode)
            {
                tas.UserRemoved += tas_UserRemoved;
                tas.BattleUserIpRecieved += tas_BattleUserIpRecieved;
                tas.UserStatusChanged += tas_UserStatusChanged;
            }
            spring.SpringStarted += spring_SpringStarted;
            spring.PlayerJoined += spring_PlayerJoined;
            spring.PlayerLeft += spring_PlayerLeft;
            spring.PlayerLost += spring_PlayerLost;
            spring.PlayerDisconnected += spring_PlayerDisconnected;
            spring.GameOver += spring_GameOver;
        }

        public static string CalculateHexMD5Hash(string input)
        {
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            var sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }

        public string SendCommand(string script, string query, bool async, bool hash)
        {
            try
            {
                string uri;
                if (hash)
                {
                    query += "&login=" + UserName;
                    uri = string.Format("{0}{1}?{2}&hash={3}",
                                        StatsUrl,
                                        script,
                                        query.Replace("#", "%23"),
                                        CalculateHexMD5Hash(query + password.Trim()));

                    //Console.WriteLine(query);
                    //Console.WriteLine(CalculateHexMD5Hash(query + password));
                }
                else uri = string.Format("{0}{1}?{2}", StatsUrl, script, query);

                if (async)
                {
                    PlasmaShared.Utils.SafeThread(() =>
                        {
                            try
                            {
                                var wc = new WebClient();
                                wc.DownloadString(uri);
                            }
                            catch {}
                        }).Start();
                    return "";
                }
                else
                {
                    var wc = new WebClient();
                    return wc.DownloadString(uri);
                }
            }
            catch
            {
                return "";
            }
        }

        void LoadAccounts()
        {
            lock (locker)
            {
                string fname = ah.springPaths.Cache + '/' + accountsFileName;
                if (File.Exists(fname))
                {
                    var s = new XmlSerializer(accounts.GetType());
                    StreamReader r = File.OpenText(fname);
                    accounts = (List<Account>)s.Deserialize(r);
                    r.Close();
                }
            }
        }

        bool RegisterPlayerInCombat(string name)
        {
					try
					{
						if (players.ContainsKey(name)) return true;
						var p = new EndGamePlayerInfo();
						p.Name = name;
						Mod mod = Program.main.UnitSyncWrapper.GetModInfo(battle.ModName);
						int idx = battle.GetUserIndex(name);
						if (idx != -1)
						{
							p.Side = mod.Sides[battle.Users[idx].Side];
							p.Spectator = battle.Users[idx].IsSpectator || tas.IsTeamSpec(battle.Users[idx].Side);
							p.AllyNumber = battle.Users[idx].AllyNumber;
							p.Ip = battle.Users[idx].ip.ToString();

							User u;
							if (tas.GetExistingUser(name, out u)) p.Rank = u.Rank + 1;
						}
						else return false;
						players.Add(name, p);
						return true;
					} catch(Exception ex)
					{
						ErrorHandling.HandleException(ex, "Cannot register in combat");
						return false;
					}
        }

        void SaveAccounts()
        {
            lock (locker)
            {
                string fname = ah.springPaths.Cache + '/' + accountsFileName;
                var s = new XmlSerializer(accounts.GetType());
                FileStream f = File.OpenWrite(fname);
                f.SetLength(0);
                s.Serialize(f, accounts);
                f.Close();
            }
        }

        void spring_GameOver(object sender, SpringLogEventArgs e)
        {
            string query = String.Format("a=battle&map={0}&mod={1}&title={2}&start={3}&duration={4}",
                                         battle.MapName,
                                         battle.ModName,
                                         battle.Title,
                                         Utils.ToUnix(startTime),
                                         Utils.ToUnix(DateTime.Now.Subtract(startTime)));

            foreach (EndGamePlayerInfo p in players.Values) if (!p.Spectator && p.AliveTillEnd) foreach (EndGamePlayerInfo pset in players.Values) if (pset.AllyNumber == p.AllyNumber && !pset.Spectator) pset.OnVictoryTeam = true;

            foreach (EndGamePlayerInfo p in players.Values) query += "&player[]=" + p;

            if (ah.PlanetWars != null) ah.PlanetWars.SendBattleResult(battle, players);

            // send only if there were at least 2 players in game
            if (players.Count > 1) SendCommand(gatherScript, query, true, true);
        }

        void spring_PlayerDisconnected(object sender, SpringLogEventArgs e)
        {
            if (RegisterPlayerInCombat(e.Username))
            {
                players[e.Username].DisconnectTime = (int)DateTime.Now.Subtract(startTime).TotalSeconds;
                players[e.Username].AliveTillEnd = false;
            }
        }

        void spring_PlayerJoined(object sender, SpringLogEventArgs e)
        {
            if (e.Username == UserName) return; // do not add autohost itself
            RegisterPlayerInCombat(e.Username);
        }

        void spring_PlayerLeft(object sender, SpringLogEventArgs e)
        {
            if (RegisterPlayerInCombat(e.Username))
            {
                players[e.Username].LeaveTime = (int)DateTime.Now.Subtract(startTime).TotalSeconds;
                players[e.Username].AliveTillEnd = false;
            }
        }

        void spring_PlayerLost(object sender, SpringLogEventArgs e)
        {
            if (RegisterPlayerInCombat(e.Username))
            {
                players[e.Username].LoseTime = (int)DateTime.Now.Subtract(startTime).TotalSeconds;
                players[e.Username].AliveTillEnd = false;
            }
        }

        void spring_SpringStarted(object sender, EventArgs e)
        {
            battle = tas.MyBattle;
            players = new Dictionary<string, EndGamePlayerInfo>();
            startTime = DateTime.Now;
        }

        void tas_BattleUserIpRecieved(object sender, TasEventArgs e)
        {
            User u;
            if (tas.GetExistingUser(e.ServerParams[0], out u)) SendCommand(gatherScript, "a=joinplayer&name=" + u.Name + "&rank=" + u.Rank + "&ip=" + e.ServerParams[1], true, true);
        }


        void tas_LoginAccepted(object sender, TasEventArgs e)
        {
            Account a = accounts.Find(delegate(Account acc) { return acc.UserName == UserName; });
            if (a != null) password = a.Password;

            if (password == "")
            {
                password = SendCommand(gatherScript, "a=register&name=" + UserName, false, false);
                if (password != "")
                {
                    if (password.StartsWith("FAILED"))
                    {
                        string mes = "You need correct password to submit stats with account " + UserName + ", stats won't work - " + password;
                        ErrorHandling.HandleException(null, mes);
                    }
                    else
                    {
                        accounts.Add(new Account(UserName, password));
                        SaveAccounts();
                    }
                }
                else
                {
                    string mes = "Error registering to stats server - stats server probably down. Statistics wont work until next Springie start";
                    ErrorHandling.HandleException(null, mes);
                }
            }
        }

        void tas_UserRemoved(object sender, TasEventArgs e)
        {
            SendCommand(gatherScript, "a=removeplayer&name=" + e.ServerParams[0], true, true);
        }

        void tas_UserStatusChanged(object sender, TasEventArgs e)
        {
            User u;
            if (tas.GetExistingUser(e.ServerParams[0], out u)) SendCommand(gatherScript, "a=addplayer&name=" + u.Name + "&rank=" + (u.Rank + 1), true, true);
        }

        public class Account
        {
            public string Password;
            public string UserName;

            public Account() {}

            public Account(string userName, string password)
            {
                UserName = userName;
                Password = password;
            }
        } ;
    }
}