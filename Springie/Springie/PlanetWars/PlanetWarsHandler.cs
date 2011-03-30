#region using

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Timers;
using LobbyClient;
using PlanetWarsShared;
using PlanetWarsShared.Springie;
using PlasmaShared.UnitSyncLib;
using Springie.autohost;

#endregion

namespace Springie.PlanetWars
{
    public class PlanetWarsHandler: IDisposable
    {

        AutoHost autoHost;
        List<string> channelAllowedExceptions = new List<string>();
        string host;
        List<String> mecenaries = new List<string>();
        List<string> planetWarsChannels = new List<string>();
        Dictionary<string, string> planetWarsPlayerSide = new Dictionary<string, string>();


        TasClient tas;
        Timer timer = new Timer();

        public AuthInfo account;
        public ISpringieServer server;

        public PlanetWarsHandler(string host, int port, AutoHost autoHost, TasClient tas, AutoHostConfig config)
        {
            this.autoHost = autoHost;
            this.tas = tas;
            this.host = host;
            account = new AuthInfo(autoHost.GetAccountName(), config.PlanetWarsServerPassword);

            server = (ISpringieServer)Activator.GetObject(typeof(ISpringieServer), String.Format("tcp://{0}:{1}/IServer", host, port));
            // fill factions for channel monitoring and join channels
            planetWarsChannels = new List<string>();
            ICollection<IFaction> factions = server.GetFactions(account);
            foreach (IFaction fact in factions)
            {
                string name = fact.Name.ToLower();
                planetWarsChannels.Add(name);
                if (!config.JoinChannels.Contains(name))
                {
                    config.JoinChannels.Add(name);
                    if (tas != null && tas.IsConnected && tas.IsLoggedIn) tas.JoinChannel(name);
                }
            }
            timer.Interval = 2000;
            timer.Elapsed += timer_Elapsed;
            timer.AutoReset = true;
            timer.Start();
        }

        public void Dispose()
        {
            timer.Stop();
            timer.Elapsed -= timer_Elapsed;
        }

        public void ComListPlanets(TasSayEventArgs e, string[] words)
        {
            string[] vals;
            try
            {
                int[] indexes;
                if (FilterPlanets(words, out vals, out indexes) > 0)
                {
                    autoHost.Respond(e, string.Format("{0} can attack:", server.GetOffensiveFaction(account).Name));
                    for (int i = 0; i < vals.Length; ++i) autoHost.Respond(e, string.Format("{0}: {1}", indexes[i], vals[i]));
                }
                else autoHost.Respond(e, "no such planet found");
            }
            catch (Exception ex)
            {
                autoHost.Respond(e, string.Format("Error getting planets: {0}", ex));
            }
        }


        public void ComMerc(TasSayEventArgs e, string[] words)
        {
            bool mode;
            if (words.Length > 0)
            {
                if (words[0] == "1") mode = true;
                else mode = false;
            }
            else mode = !IsMercenary(e.UserName);

            SetMercenary(e.UserName, mode);
            autoHost.Respond(e, string.Format("Mercenary mode {0}", mode ? "enabled" : "disabled"));
        }

        public void ComPlanet(TasSayEventArgs e, string[] words)
        {
            if (words.Length == 0)
            {
                autoHost.Respond(e, "You must specify planet name");
                return;
            }
            string[] vals;
            int[] indexes;

            IPlayer info = server.GetPlayerInfo(account, e.UserName);
            IFaction fact = server.GetOffensiveFaction(account);
            if (info == null || info.FactionName != fact.Name)
            {
                autoHost.Respond(e, string.Format("It's currently {0} turn", fact.Name));
                return;
            }

            if (FilterPlanets(words, out vals, out indexes) > 0)
            {
                bool canset;
                string bestPlayer = "";

                if (info.IsCommanderInChief) canset = true;
                else
                {
                    canset = true;
                    int bestRank = info.RankOrder;
                    Mod mod = Program.main.UnitSyncWrapper.GetModInfo(tas.MyBattle.ModName);
                    foreach (UserBattleStatus u in tas.MyBattle.Users)
                    {
                        if (!u.IsSpectator && u.Name != e.UserName &&
                            mod.Sides[u.Side].ToLower() == info.FactionName.ToLower())
                        {
                            IPlayer pi = server.GetPlayerInfo(account, u.Name);
                            if (pi != null && pi.RankOrder < bestRank)
                            {
                                // someone sle with better rank exists
                                bestPlayer = pi.Name;
                                bestRank = pi.RankOrder;
                                canset = false;
                            }
                        }
                    }
                }

                if (canset)
                {
                    string pname = vals[0].Split('|')[0];
                    autoHost.SayBattle(string.Format("changing planet to {0} by {1}", pname, e.UserName));
                    IPlanet planet = server.GetAttackOptions(account).Where(m => m.Name == pname).Single();
                    var mapi = Program.main.UnitSyncWrapper.GetMapInfo(planet.MapName);
                    tas.ChangeMap(mapi.Name, mapi.Checksum);
                }
                else autoHost.Respond(e, string.Format("You are not first in command here, {0} must do it!", bestPlayer));
            }
            else autoHost.Respond(e, "Cannot find such planet.");
        }

        public void ComRegister(TasSayEventArgs e, string[] words)
        {
            if (words.Length < 2)
            {
                autoHost.Respond(e,
                                 "This command needs 2-3 parameters - side(core or arm) and password and optional planet name (you can PM it to me). For example say: !register arm/core password");
                return;
            }

            try
            {
                string response = server.Register(account,
                                                  new AuthInfo { Login = e.UserName, Password = words[1] },
                                                  words[0],
                                                  words.Length > 2 ? Utils.Glue(words, 2) : null);
                autoHost.Respond(e, string.Format(response));
                if (response.StartsWith("Welcome to PlanetWars")) SetMercenary(e.UserName, false);
                //	else autoHost.Respond(e, "To disable mercenary mode say !merc");
            }
            catch (Exception ex)
            {
                autoHost.Respond(e, string.Format("Error when registering: {0}", ex));
            }
        }

        public void ComResetPassword(TasSayEventArgs e)
        {
            try
            {
                autoHost.Respond(e, server.ResetPassword(account, e.UserName));
            }
            catch (Exception ex)
            {
                autoHost.SayBattle("Error reseting password: " + ex);
            }
        }

        public int FilterPlanets(string[] words, out string[] vals, out int[] indexes)
        {
            ICollection<IPlanet> options = server.GetAttackOptions(account);

            if (options != null)
            {
                var temp = new string[options.Count];
                int cnt = 0;

                foreach (IPlanet planet in options) temp[cnt++] = string.Format("{0}|  {1}", planet.Name, Path.GetFileNameWithoutExtension(planet.MapName));
                return AutoHost.Filter(temp, words, out vals, out indexes);
            }
            else
            {
                vals = null;
                indexes = null;
                return 0;
            }
        }

        public void MapChanged()
        {
            try
            {
                string name = tas.MyBattle.MapName;
                IPlanet mapInfo = server.GetAttackOptions(account).Where(m => m.MapName == name).Single();
                if (mapInfo.StartBoxes != null && mapInfo.StartBoxes.Count > 0)
                {
                    int rectangles = tas.MyBattle.Rectangles.Count;
                    for (int i = 0; i < rectangles; ++i) tas.RemoveBattleRectangle(i);
                    for (int i = 0; i < mapInfo.StartBoxes.Count; ++i)
                    {
                        Rectangle mi = mapInfo.StartBoxes[i];
                        tas.AddBattleRectangle(i, new BattleRect(mi.Left, mi.Top, mi.Right, mi.Bottom));
                    }
                }

                foreach (string command in mapInfo.AutohostCommands) tas.Say(TasClient.SayPlace.Channel, tas.UserName, command, false);

                autoHost.SayBattle(String.Format("Welcome to {0}!  (http://{2}/planet.aspx?name={1})",
                                                 mapInfo.Name,
                                                 Uri.EscapeUriString(mapInfo.Name),
                                                 host));

                ICollection<string> notifyList = server.GetPlayersToNotify(account, name, ReminderEvent.OnBattlePreparing);
                foreach (string userName in notifyList) tas.Say(TasClient.SayPlace.User, userName, string.Format("Planet {0} is under attack! Join the fight!", mapInfo.Name), false);
            }
            catch (Exception ex)
            {
                autoHost.SayBattle(string.Format("Error setting planet starting boxes: {0}", ex));
            }
        }

        public void SendBattleResult(Battle battle, Dictionary<string, EndGamePlayerInfo> players)
        {
            try
            {
                SendBattleResultOutput response = server.SendBattleResult(account,
                                                                          battle.MapName,
                                                                          players.Values.Where(x => !IsMercenary(x.Name)).ToList());

                autoHost.SayBattle(response.MessageToDisplay);
                List<UserBattleStatus> users = tas.MyBattle.Users;
                foreach (EndGamePlayerInfo p in players.Values) if (p.Name != tas.UserName && users.Find(x => x.Name == p.Name) == null) tas.Say(TasClient.SayPlace.User, p.Name, response.MessageToDisplay, false);

                foreach (RankNotification kvp in response.RankNotifications) tas.Say(TasClient.SayPlace.User, kvp.Name, kvp.Text, false);

                ComListPlanets(TasSayEventArgs.Default, new string[] { });
            }
            catch (Exception ex)
            {
                autoHost.SayBattle(string.Format("Error sending planet battle result :(( {0}", ex), true);
            }
        }

        public void SpringExited()
        {
            try
            {
                ICollection<string> toNotify = server.GetPlayersToNotify(account, tas.MyBattle.MapName, ReminderEvent.OnBattleEnded);
                foreach (string s in toNotify) tas.Say(TasClient.SayPlace.User, s, "PlanetWars battle has just ended.", false);
            }
            catch (Exception ex)
            {
                autoHost.SayBattle("Error notifying game end:" + ex);
            }
        }


        public static void SpringMessage(string text)
        {
        }

        public bool StartGame(TasSayEventArgs e)
        {
            try
            {
                if (!autoHost.ComFix(e, "silent"))
                {
                    autoHost.Respond(e, "Teams were not fixed, fixing");
                    return false;
                }

                Battle bat = tas.MyBattle;
                Mod mod = Program.main.UnitSyncWrapper.GetModInfo(bat.ModName);
                foreach (var grouping in bat.Users.Where(x => !x.IsSpectator).GroupBy(x => x.AllyNumber))
                {
                    if (!grouping.Any(y => !IsMercenary(y.Name)))
                    {
                        autoHost.Respond(e,
                                         string.Format("Alliance {0} contains only mercenaries. Register/disable mercenary mode with !merc please",
                                                       grouping.Key));
                        return false;
                    }
                }

                string currentMapName = bat.MapName;
                IPlanet planet = server.GetAttackOptions(account).Where(p => p.MapName == currentMapName).SingleOrDefault();

                if (planet == null)
                {
                    autoHost.SayBattle("This planet is not currently allowed, select another one");
                    return false;
                }

                ICollection<IFaction> factions = server.GetFactions(account);
                var actual = new List<IPlayer>();
                foreach (UserBattleStatus user in bat.Users)
                {
                    if (!user.IsSpectator && !IsMercenary(user.Name))
                    {
                        IPlayer info = server.GetPlayerInfo(account, user.Name);
                        actual.Add(info);
                        string side = mod.Sides[user.Side];
                        string hisSide = factions.Where(f => f.Name == info.FactionName).Single().SpringSide;

                        if (!string.Equals(side, hisSide, StringComparison.InvariantCultureIgnoreCase))
                        {
                            autoHost.SayBattle(string.Format("{0} must switch to {1}", user.Name, hisSide), false);
                            return false;
                        }
                    }
                }

                string options = server.GetStartupModOptions(account, bat.MapName, actual);
                //SayBattle(Encoding.ASCII.GetString(Convert.FromBase64String(options.Replace("+", "="))));
                Battle b = tas.MyBattle;
                foreach (Option o in mod.Options)
                {
                    if (o.Key == "planetwars")
                    {
                        string res;
                        if (o.GetPair(options, out res))
                        {
                            tas.SetScriptTag(res);

                            ICollection<string> startEvent = server.GetPlayersToNotify(account, currentMapName, ReminderEvent.OnBattleStarted);
                            foreach (string s in startEvent)
                                tas.Say(TasClient.SayPlace.User,
                                        s,
                                        string.Format("PlanetWars battle for planet {0} owned by {1} is starting.", planet.Name, planet.OwnerName),
                                        false);

                            return true;
                        }
                        else
                        {
                            autoHost.Respond(e, "Eror setting script tag");
                            return false;
                        }
                    }
                }
                autoHost.Respond(e, "This mod does not support PlanetWars");
                return false;
            }
            catch (Exception ex)
            {
                autoHost.SayBattle(string.Format("Error when checking PlanetWars teams: {0}", ex), false);
                return false;
            }
        }

        public void UserJoined(string name)
        {
            try
            {
                IFaction current = server.GetOffensiveFaction(account);

                if (IsMercenary(name)) autoHost.SayBattle(string.Format("Mercenary {0} is here.", name));
                else
                {
                    IPlayer info = server.GetPlayerInfo(account, name);
                    if (info != null)
                    {
                        autoHost.SayBattle(
                            string.Format("{0} {1} {2}. Attacking faction is {3}. http://{5}/player.aspx?name={4}",
                                          info.IsCommanderInChief ? "All hail to" : "Greetings, ",
                                          info.RankText,
                                          name,
                                          current.Name,
                                          Uri.EscapeDataString(info.Name),
                                          host),
                            false);
                    }
                }
            }
            catch (Exception ex)
            {
                autoHost.SayBattle("PlanetWars error: " + ex);
            }
        }

        public void UserJoinedChannel(string channel, string name)
        {
            if (planetWarsChannels.Contains(channel) && name != tas.UserName)
            {
                bool? isOk = null;
                string side;
                if (planetWarsPlayerSide.TryGetValue(name, out side))
                {
                    // first check cached value
                    isOk = side == channel;
                }
                if (!isOk.HasValue)
                {
                    IPlayer usinfo = null;
                    try
                    {
                        usinfo = server.GetPlayerInfo(account, name);
                    }
                    catch (Exception ex)
                    {
                        autoHost.SayBattle("Error fetching user info:" + ex);
                    }
                    if (usinfo == null)
                    {
                        if (channelAllowedExceptions.Contains(name)) isOk = true;
                        else
                        {
                            channelAllowedExceptions = new List<string>(server.GetFactionChannelAllowedExceptions());
                            isOk = channelAllowedExceptions.Contains(name);
                        }
                    }
                    else
                    {
                        string realFact = usinfo.FactionName.ToLower();
                        planetWarsPlayerSide[name] = realFact;
                        isOk = realFact == channel;
                    }
                }

                // he is not from this faction, mute him
                if (!isOk.Value)
                {
                    tas.Say(TasClient.SayPlace.User,
                            "ChanServ",
                            string.Format("!kick #{0} {1} This is PlanetWars faction channel (http://{2}/) Register using !register command.",
                                          channel,
                                          name,
                                          host),
                            false);
                }
            }
        }

        public void UserSaid(TasSayEventArgs e)
        {
            if (e.Origin == TasSayEventArgs.Origins.Player && e.Place == TasSayEventArgs.Places.Channel && planetWarsChannels.Contains(e.Channel))
            {
                //  lets log this
                try
                {
                    server.SendChatLine(account, e.Channel, e.UserName, e.Text);
                }
                catch (Exception ex)
                {
                    autoHost.SayBattle("Error sending chat:" + ex);
                }
            }
        }

        bool IsMercenary(string name)
        {
            return mecenaries.Contains(name);
        }

        void SetMercenary(string name, bool enabled)
        {
            if (enabled)
            {
                if (!mecenaries.Contains(name)) mecenaries.Add(name);
            }
            else mecenaries.Remove(name);
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Stop();
            try
            {
                Battle b = tas.MyBattle;
                if (!autoHost.ComFix(TasSayEventArgs.Default, "silent")) return;

                var factions = new List<IFaction>(server.GetFactions(account));

                Mod mod = Program.main.UnitSyncWrapper.GetModInfo(b.ModName);

                List<string> sides = mod.Sides.ToList();
                bool teamsOk = true;
                foreach (
                    UserBattleStatus user in b.Users.Where(x => !x.IsSpectator && x.SyncStatus != SyncStatuses.Unknown))
                {
                    IPlayer info = null;
                    if (!IsMercenary(user.Name)) info = server.GetPlayerInfo(account, user.Name);
                    if (info == null)
                    {
                        if (!IsMercenary(user.Name))
                        {
                            // player not mercenary and not registered -> force him merc
                            SetMercenary(user.Name, true);
                            tas.Say(TasClient.SayPlace.User,
                                    user.Name,
                                    string.Format(
                                        "This is online campaign server - http://{0}/ - you must register first /to play here. \n To register say: !register side newpassword (optional planetnname) \n Example: !register core secretpw \n Or: !register arm mynewpassword Alpha Centauri",
                                        host),
                                    false);
                            autoHost.SayBattle(
                                string.Format("{0} is not a registered player. Forcing him to be a mercenary. Register using !register ", user.Name));
                        }

                        if (user.AllyNumber >= factions.Count)
                        {
                            // player alliance invalid, fix it

                            List<IGrouping<int, UserBattleStatus>> grp =
                                b.Users.Where(
                                    u =>
                                    !u.IsSpectator && u.SyncStatus != SyncStatuses.Unknown && u.AllyNumber < factions.Count)
                                    .GroupBy(u => u.AllyNumber).OrderBy(g => g.Count()).ToList(); // get alliances and find smallest to put merc in
                            if (grp.Count > 0) tas.ForceAlly(user.Name, grp[0].Key); // put merc to smallest valid team
                            else tas.ForceAlly(user.Name, 0); // or to ally 0 if none found
                            teamsOk = false;
                        }
                        else
                        {
                            // set proper side for him if his alliance is ok
                            string springSideName = sides.SingleOrDefault(s => s.ToUpper() == factions[user.AllyNumber].Name.ToUpper());
                            int springSideIndex = sides.IndexOf(springSideName);
                            if (user.Side != springSideIndex) tas.ForceSide(user.Name, springSideIndex);
                        }
                    }
                    else
                    {
                        int hisFaction = factions.IndexOf(factions.Find(f => f.Name == info.FactionName));
                        string springSideName = sides.SingleOrDefault(s => s.ToUpper() == info.FactionName.ToUpper());
                        int springSideIndex = sides.IndexOf(springSideName);

                        // he is in wrong team
                        if (user.AllyNumber != hisFaction)
                        {
                            tas.ForceAlly(user.Name, hisFaction);
                            teamsOk = false;
                        }
                        else if (user.Side != springSideIndex) tas.ForceSide(user.Name, springSideIndex);
                    }
                }
                if (!teamsOk) return; // dont proceed to balancing if teams are bing changed

                List<IGrouping<int, UserBattleStatus>> grouping =
                    b.Users.Where(u => !u.IsSpectator && u.SyncStatus != SyncStatuses.Unknown).GroupBy(u => u.AllyNumber).
                        OrderBy(g => g.Count()).ToList();
                if (grouping.Count() == 2)
                {
                    if (grouping[1].Count() - grouping[0].Count() > 1)
                    {
                        // if one team is bigger than other by 2+

                        // find latest joined merc
                        UserBattleStatus newest =
                            b.Users.Where(u => u.AllyNumber == grouping[1].Key && IsMercenary(u.Name) && !u.IsSpectator).
                                OrderByDescending(u => u.JoinTime).FirstOrDefault();
                        if (newest != null) tas.ForceAlly(newest.Name, grouping[0].Key); // and move to smaller team
                    }
                }
            }
            catch (Exception ex)
            {
                autoHost.SayBattle("Problem with PlanetWars:" + ex);
            }
            finally
            {
                timer.Start();
            }
        }
    }
}