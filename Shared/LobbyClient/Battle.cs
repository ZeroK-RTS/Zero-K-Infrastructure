using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using PlasmaShared;
using PlasmaShared.LobbyMessages;
using ZkData;
using ZkData.UnitSyncLib;

namespace LobbyClient
{
    public class Battle: ICloneable
    {
        public enum NatMode
        {
            None = 0,
            HolePunching = 1,
            FixedPorts = 2
        };

        /// <summary>
        /// Full map metadata - loaded only for joined battle
        /// </summary>
        readonly Map map;
        /// <summary>
        /// Full mod metadata - loaded only for joined battle
        /// </summary>
        readonly Mod mod;

        public int BattleID { get; set; }
        public List<BotBattleStatus> Bots { get; set; }
        public BattleDetails Details { get; set; }
        public List<string> DisabledUnits { get; set; }

        public User Founder { get; set; }

        public int HostPort { get; set; }
        public string Ip { get; set; }
        public bool IsFull { get { return NonSpectatorCount == MaxPlayers; } }
        public bool IsInGame { get { return Founder.IsInGame; } }
        public bool IsLocked { get; set; }
        public bool IsMission { get { return mod != null && mod.IsMission; } }
        public bool IsPassworded { get { return Password != "*"; } }
        public bool IsReplay { get; set; }

        public int? MapHash { get; set; }
        public string MapName { get; set; }

        public int MaxPlayers { get; set; }

        public int? ModHash { get; set; }
        public string ModName { get; set; }
        public Dictionary<string, string> ModOptions { get; private set; }
        public NatMode Nat { get; set; }

        public int NonSpectatorCount { get { return Users.Count - SpectatorCount; } }

        public string Password = "*";

        public Dictionary<int, BattleRect> Rectangles { get; set; }
        public List<string> ScriptTags = new List<string>();
        public string EngineName = "spring";
        public string EngineVersion { get; set; }
        public int SpectatorCount { get; set; }
        public string Title { get; set; }

        public List<UserBattleStatus> Users { get; set; }


        public bool IsSpringieManaged
        {
            get { return Founder != null && Founder.ClientType == Login.ClientTypes.SpringieManaged;}
        }

        public bool IsQueue
        {
            get { return IsSpringieManaged && Title.StartsWith("Queue"); }
        }

        public string QueueName
        {
            get
            {
                if (IsQueue)
                {
                    return Title.Substring(6);
                }
                else return null;
            }
        }

        internal Battle()
        {
            Bots = new List<BotBattleStatus>();
            Details = new BattleDetails();
            ModOptions = new Dictionary<string, string>();
            Rectangles = new Dictionary<int, BattleRect>();
            DisabledUnits = new List<string>();
            Password = "*";
            Nat = NatMode.None;
            Users = new List<UserBattleStatus>();
        }


        public Battle(string engineVersion, string password, int port, int maxplayers, Map map, string title, Mod mod, BattleDetails details): this()
        {
            if (!String.IsNullOrEmpty(password)) Password = password;
            if (port == 0) HostPort = 8452; else HostPort = port;
            try {
                var ports = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners().OrderBy(x => x.Port).Select(x=>x.Port).ToList();
                if (ports.Contains(HostPort)) {
                    var blockedPort = HostPort;
                    while (ports.Contains(HostPort)) HostPort++;
                    Trace.TraceWarning("Host port {0} was used, using backup port {1}", blockedPort, HostPort);
                }
            } catch {}




            EngineVersion = engineVersion;
            MaxPlayers = maxplayers;
            this.map = map;
            MapName = map.Name;
            MapHash = map.Checksum;
            Title = title;
            this.mod = mod;
            ModName = mod.Name;
            ModHash = mod.Checksum;
            if (details != null) Details = details;
        }


        public bool CanBeJoined(int playerRank)
        {
            return NonSpectatorCount > 0 && !IsLocked && MaxPlayers > NonSpectatorCount && Password == "*";
        }

        public bool ContainsUser(string name, out UserBattleStatus status)
        {
            status = Users.SingleOrDefault(x => x.Name == name);
            return status != null;
        }

        /// <summary>
        /// Generates script
        /// </summary>
        /// <param name="playersExport">list of players</param>
        /// <param name="localUser">myself</param>
        /// <param name="loopbackListenPort">listen port for autohost interface</param>
        /// <param name="zkSearchTag">hackish search tag</param>
        /// <param name="startSetup">structure with custom extra data</param>
        /// <returns></returns>
        public string GenerateScript(out List<UserBattleStatus> playersExport,
                                     User localUser,
                                     int loopbackListenPort,
                                     string zkSearchTag,
                                     SpringBattleStartSetup startSetup)
        {
            var previousCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                playersExport = new List<UserBattleStatus>();
                var isHost = localUser.Name == Founder.Name;
                var myUbs = Users.SingleOrDefault(x => x.Name == localUser.Name);
                if (!isHost)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("[GAME]");
                    sb.AppendLine("{");
                    sb.AppendFormat("HostIP={0};\n", Ip);
                    sb.AppendFormat("HostPort={0};\n", HostPort);
                    sb.AppendLine("IsHost=0;");
                    sb.AppendFormat("MyPlayerName={0};\n", localUser.Name);
                    if (myUbs != null)
                    {
                        if (myUbs.ScriptPassword != null) sb.AppendFormat("MyPasswd={0};\n", myUbs.ScriptPassword);
                    }
                    else
                    {
                        sb.AppendFormat("MyPasswd={0};\n", localUser.Name); // used for mid-game join .. if no userbattlestatus, use own name
                    }
                    sb.AppendLine("}");
                    return sb.ToString();
                }
                else
                {
                    if (mod == null) throw new ApplicationException("Mod not downloaded yet");

                    var script = new StringBuilder();

                    script.AppendLine("[GAME]");
                    script.AppendLine("{");

                    script.AppendFormat("   ZkSearchTag={0};\n", zkSearchTag);
                    script.AppendFormat("  Mapname={0};\n", MapName);

                    if (mod.IsMission) script.AppendFormat("  StartPosType=3;\n");
                    else
                    {
                        if (Details.StartPos == BattleStartPos.Choose) script.AppendFormat("  StartPosType=2;\n");
                        else script.AppendFormat("  StartPosType=3;\n"); // workaround for random/fixed
                        // script.AppendFormat("  StartPosType={0};\n", (int)Details.StartPos);
                    }

                    script.AppendFormat("  GameType={0};\n", ModName);
                    if (ModHash.HasValue)
                        script.AppendFormat("  ModHash={0};\n", ModHash.Value != 0 ? (uint)ModHash.Value : 1);
                            // hack dont set to 1 when dedi srever is patched
                    if (MapHash.HasValue) script.AppendFormat("  MapHash={0};\n", MapHash.Value != 0 ? (uint)MapHash.Value : 1);
                    script.AppendFormat("  AutohostPort={0};\n", loopbackListenPort);
                    script.AppendLine();
                    script.AppendFormat("  HostIP={0};\n", Ip);
                    script.AppendFormat("  HostPort={0};\n", HostPort);
                    script.AppendFormat("  SourcePort={0};\n", 8300);
                    script.AppendFormat("  IsHost=1;\n");
                    script.AppendLine();

                    //script.AppendFormat("  MyPlayerName={0};\n", localUser.Name);

                    var positions = map.Positions != null ? map.Positions.ToList() : new List<StartPos>();
                    if (Details.StartPos == BattleStartPos.Random) positions = positions.Shuffle();

                    List<UserBattleStatus> users;
                    List<BotBattleStatus> bots;

                    if (startSetup != null && startSetup.BalanceTeamsResult != null && startSetup.BalanceTeamsResult.Players != null)
                    {
                        // if there is a balance results as a part of start setup, use values from this (override lobby state)
                        users = new List<UserBattleStatus>(this.Users.Select(x => x.Clone()));
                        bots = new List<BotBattleStatus>(this.Bots.Select(x => (BotBattleStatus)x.Clone()));
                        foreach (var p in startSetup.BalanceTeamsResult.Players)
                        {
                            var us = users.FirstOrDefault(x => x.Name == p.Name);
                            if (us == null)
                            {
                                us = new UserBattleStatus(p.Name, new User() {LobbyID = p.LobbyID}, Password);
                                users.Add(us);
                            }
                            us.TeamNumber = p.TeamID;
                            us.IsSpectator = p.IsSpectator;
                            us.AllyNumber = p.AllyID;

                        }
                        foreach (var p in startSetup.BalanceTeamsResult.Bots)
                        {
                            var bot = bots.FirstOrDefault(x => x.Name == p.BotName);
                            if (bot == null)
                            {
                                bot = new BotBattleStatus(p.BotName, p.Owner, p.BotAI);
                                bots.Add(bot);
                            }
                            bot.AllyNumber = bot.AllyNumber;
                            bot.TeamNumber = bot.TeamNumber;
                        }

                        foreach (var u in users.Where(x => !startSetup.BalanceTeamsResult.Players.Any(y => y.Name == x.Name)))
                        {
                            u.IsSpectator = true;
                        } // spec those not known at the time of balance
                    }
                    else
                    {
                        users = this.Users;
                        bots = this.Bots;
                    }


                    GeneratePlayerSection(playersExport, localUser, startSetup, users, script, positions, bots);

                    return script.ToString();
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previousCulture;
            }
        }

        void GeneratePlayerSection(List<UserBattleStatus> playersExport,
            User localUser,
            SpringBattleStartSetup startSetup,
            List<UserBattleStatus> users,
            StringBuilder script,
            List<StartPos> positions,
            List<BotBattleStatus> bots)
        {
            if (mod != null && mod.IsMission) // mission stuff
            {
                var aiNum = 0;
                var declaredTeams = new HashSet<int>();
                var orderedUsers = users.OrderBy(x => x.TeamNumber).ToList();
                for (var i = 0; i < orderedUsers.Count; i++)
                {
                    var u = orderedUsers[i];
                    ScriptAddUser(script, i, playersExport, startSetup, u.TeamNumber, u);
                    if (!u.IsSpectator && !declaredTeams.Contains(u.TeamNumber))
                    {
                        ScriptAddTeam(script, u.TeamNumber, positions, i, u,mod,Details);
                        declaredTeams.Add(u.TeamNumber);
                    }
                }

                for (var i = 0; i < orderedUsers.Count; i++)
                {
                    var u = orderedUsers[i];
                    foreach (var b in bots.Where(x => x.owner == u.Name))
                    {
                        ScriptAddBot(script, aiNum++, b.TeamNumber, i, b);
                        if (!declaredTeams.Contains(b.TeamNumber))
                        {
                            ScriptAddTeam(script, b.TeamNumber, positions, i, b,mod,Details);
                            declaredTeams.Add(b.TeamNumber);
                        }
                    }
                }
            }
            else
            {
                // ordinary battle stuff

                var userNum = 0;
                var teamNum = 0;
                var aiNum = 0;
                //players is excluding self (so "springie doesn't appear as spec ingame") & excluding bots (bots is added later for each owner)
                foreach (var u in users.Where(u => !bots.Any(b => b.Name == u.Name)).OrderBy(x => x.TeamNumber).Where(x => x.Name != localUser.Name)) 
                {
                    ScriptAddUser(script, userNum, playersExport, startSetup, teamNum, u);

                    if (!u.IsSpectator)
                    {
                        ScriptAddTeam(script, teamNum, positions, userNum, u,mod,Details);
                        teamNum++;
                    }

                    foreach (var b in bots.Where(x => x.owner == u.Name))
                    {
                        ScriptAddBot(script, aiNum, teamNum, userNum, b);
                        aiNum++;
                        ScriptAddTeam(script, teamNum, positions, userNum, b,mod,Details);
                        teamNum++;
                    }
                    userNum++;
                }
            }

            // ALLIANCES
            script.AppendLine();
            foreach (var allyNumber in
                users.Where(x => !x.IsSpectator).Select(x => x.AllyNumber).Union(bots.Select(x => x.AllyNumber)).Union(Rectangles.Keys).Distinct())
            {
                // get allies from each player, bot and rectangles (for koth)
                script.AppendFormat("[ALLYTEAM{0}]\n", allyNumber);
                script.AppendLine("{");
                script.AppendFormat("     NumAllies={0};\n", 0);
                double left = 0, top = 0, right = 1, bottom = 1;
                BattleRect rect;
                if (Rectangles.TryGetValue(allyNumber, out rect)) rect.ToFractions(out left, out top, out right, out bottom);
                script.AppendFormat(CultureInfo.InvariantCulture,"     StartRectLeft={0};\n", left);
                script.AppendFormat(CultureInfo.InvariantCulture,"     StartRectTop={0};\n", top);
                script.AppendFormat(CultureInfo.InvariantCulture,"     StartRectRight={0};\n", right);
                script.AppendFormat(CultureInfo.InvariantCulture,"     StartRectBottom={0};\n", bottom);
                script.AppendLine("}");
            }

            script.AppendLine();
            script.AppendFormat("  NumRestrictions={0};\n", DisabledUnits.Count);
            script.AppendLine();

            if (!mod.IsMission)
            {
                script.AppendLine("  [RESTRICT]");
                script.AppendLine("  {");
                for (var i = 0; i < DisabledUnits.Count; ++i)
                {
                    script.AppendFormat("    Unit{0}={1};\n", i, DisabledUnits[i]);
                    script.AppendFormat("    Limit{0}=0;\n", i);
                }
                script.AppendLine("  }");

                script.AppendLine("  [MODOPTIONS]");
                script.AppendLine("  {");

                var options = new Dictionary<string, string>();

                // put standard modoptions to options dictionary
                foreach (var o in mod.Options.Where(x => x.Type != OptionType.Section))
                {
                    var v = o.Default;
                    if (ModOptions.ContainsKey(o.Key)) v = ModOptions[o.Key];
                    options[o.Key] = v;
                }

                // replace/add custom modoptions from startsetup (if they exist)
                if (startSetup != null && startSetup.ModOptions != null) foreach (var entry in startSetup.ModOptions) options[entry.Key] = entry.Value;

                // write final options to script
                foreach (var kvp in options) script.AppendFormat("    {0}={1};\n", kvp.Key, kvp.Value);

                script.AppendLine("  }");
            }

            script.AppendLine("}");
        }


        public int GetFirstEmptyRectangle()
        {
            for (var i = 0; i < Spring.MaxAllies; ++i) if (!Rectangles.ContainsKey(i)) return i;
            return -1;
        }

        public int GetFreeTeamID(string exceptUser)
        {
            return
                Enumerable.Range(0, TasClient.MaxTeams - 1).FirstOrDefault(
                    teamID =>
                    !Users.Where(u => !u.IsSpectator).Any(user => user.Name != exceptUser && user.TeamNumber == teamID) &&
                    !Bots.Any(x => x.TeamNumber == teamID));
        }

        public int GetState(User founder)
        {
            var battleState = 0;
            if (founder.IsInGame) battleState += 2;
            if (IsFull) battleState++;
            if (IsPassworded) battleState += 3;
            if (IsReplay) battleState += 6;
            return battleState;
        }

        public int GetUserIndex(string name)
        {
            for (var i = 0; i < Users.Count; ++i) if (Users[i].Name == name) return i;
            return -1;
        }


        public void RemoveUser(string name)
        {
            var ret = GetUserIndex(name);
            if (ret != -1) Users.RemoveAt(ret);
        }

        public override string ToString()
        {
            return String.Format("{0} {1} ({2}+{3}/{4})", ModName, MapName, NonSpectatorCount, SpectatorCount, MaxPlayers);
        }

        public static void ScriptAddBot(StringBuilder script, int aiNum, int teamNum, int userNum, BotBattleStatus status)
        {
            // AI
            var split = status.aiLib.Split('|');
            script.AppendFormat("  [AI{0}]\n", aiNum);
            script.AppendLine("  {");
            script.AppendFormat("    Name={0};\n", status.Name);
            script.AppendFormat("    ShortName={0};\n", split[0]);
            script.AppendFormat("    Version={0};\n", split.Length > 1 ? split[1] : ""); //having no value is better. Related file: ResolveSkirmishAIKey() at Spring/ExternalAI/IAILibraryManager.cpp 
            script.AppendFormat("    Team={0};\n", teamNum);
            script.AppendFormat("    Host={0};\n", userNum);
            script.AppendLine("    IsFromDemo=0;");
            script.AppendLine("    [Options]");
            script.AppendLine("    {");
            script.AppendLine("    }");
            script.AppendLine("  }\n");
        }

        public static void ScriptAddTeam(StringBuilder script, int teamNum, List<StartPos> positions, int userNum, UserBattleStatus status, Mod mod, BattleDetails Details)
        {
            // BOT TEAM
            script.AppendFormat("  [TEAM{0}]\n", teamNum);
            script.AppendLine("  {");
            script.AppendFormat("     TeamLeader={0};\n", userNum);
            script.AppendFormat("     AllyTeam={0};\n", status.AllyNumber);
            script.AppendFormat(CultureInfo.InvariantCulture,"     RGBColor={0:F5} {1:F5} {2:F5};\n",
                                (status.TeamColor & 255)/255.0,
                                ((status.TeamColor >> 8) & 255)/255.0,
                                ((status.TeamColor >> 16) & 255)/255.0);
            var side = "mission";
            if (mod.Sides.Length > status.Side) side = mod.Sides[status.Side];
            script.AppendFormat("     Side={0};\n", side);

            script.AppendFormat("     Handicap={0};\n", 0);
            if (mod.IsMission)
            {
                script.AppendFormat("      StartPosX={0};\n", 0);
                script.AppendFormat("      StartPosZ={0};\n", 0);
            }
            else
            {
                if ((Details.StartPos == BattleStartPos.Random || Details.StartPos == BattleStartPos.Fixed) && positions.Count > teamNum)
                {
                    var pos = positions[teamNum];
                    script.AppendFormat("      StartPosX={0};\n", pos.x);
                    script.AppendFormat("      StartPosZ={0};\n", pos.z);
                }
            }
            script.AppendLine("  }");
        }

        public static void ScriptAddUser(StringBuilder script, int userNum, List<UserBattleStatus> playersExport, SpringBattleStartSetup startSetup, int teamNum, UserBattleStatus status)
        {
            var export = status.Clone();
            export.TeamNumber = teamNum;
            playersExport.Add(status);

            // PLAYERS
            script.AppendFormat("  [PLAYER{0}]\n", userNum);
            script.AppendLine("  {");
            script.AppendFormat("     Name={0};\n", status.Name);

            script.AppendFormat("     Spectator={0};\n", status.IsSpectator ? 1 : 0);
            if (!status.IsSpectator) script.AppendFormat("     Team={0};\n", teamNum);

            if (status.LobbyUser != null)
            {
                script.AppendFormat("     CountryCode={0};\n", status.LobbyUser.Country);
                script.AppendFormat("     LobbyID={0};\n", status.LobbyUser.LobbyID);
            }
            if (status.ScriptPassword != null) script.AppendFormat("     Password={0};\n", status.ScriptPassword);

            if (startSetup != null)
            {
                var entry = startSetup.UserParameters.FirstOrDefault(x => x.LobbyID == status.LobbyUser.LobbyID);
                if (entry != null) foreach (var kvp in entry.Parameters) script.AppendFormat("     {0}={1};\n", kvp.Key, kvp.Value);
            }
            script.AppendLine("  }");
        }

        public object Clone()
        {
            var b = (Battle)MemberwiseClone();
            if (Details != null) b.Details = (BattleDetails)Details.Clone();
            if (Users != null) b.Users = new List<UserBattleStatus>(Users);
            if (Rectangles != null)
            {
                // copy the dictionary
                b.Rectangles = new Dictionary<int, BattleRect>();
                foreach (var kvp in Rectangles) b.Rectangles.Add(kvp.Key, kvp.Value);
            }

            if (DisabledUnits != null) b.DisabledUnits = new List<string>(DisabledUnits);
            return b;
        }

        public  BattleContext GetContext()
        {
            var ret = new BattleContext();
            ret.AutohostName = Founder.Name;
            ret.Map = MapName;
            ret.Mod = ModName;
            ret.Players = Users.Where(x=>x.SyncStatus != SyncStatuses.Unknown).Select(x => new PlayerTeam() { AllyID = x.AllyNumber, Name = x.Name, LobbyID = x.LobbyUser.LobbyID, TeamID = x.TeamNumber, IsSpectator = x.IsSpectator }).ToList();

            ret.Bots = Bots.Select(x => new BotTeam() { BotName = x.Name, AllyID = x.AllyNumber, TeamID = x.TeamNumber, Owner = x.owner, BotAI = x.aiLib }).ToList();
            return ret;
        }
    }
}