#region using

using System.Collections.Generic;
using System.ComponentModel;
using LobbyClient;
using PlasmaShared.UnitSyncLib;

#endregion

namespace Springie.autohost
{
    public class AutoHostConfig
    {
        BattleDetails battleDetails = new BattleDetails();
        int defaulRightsLevel = 1;
        int defaulRightsLevelForLobbyAdmins = 4;
        string defaultMap = "SmallDivide";
        string defaultMod = "XTA v8";
        UnitInfo[] disabledUnits = new UnitInfo[] { };
        string gameTitle = "AutoHost (%1)";
        string[] mapCycle = new string[] { };
        int maxPlayers = 10;
        string password = "*";
        string welcome = "Hi %1 (rights:%2), welcome to %3, automated host. For help say !help";
        public string AccountName = "login";
        public string AccountPassword = "password";

        public bool AutoSpawnClone = true;
        public string AutoUpdateRapidTag = "";

        [Category("Default battle settings")]
        [Description("Defines battle details to use by default")]
        public BattleDetails BattleDetails { get { return battleDetails; } set { battleDetails = value; } }

        public List<CommandConfig> Commands = new List<CommandConfig>();

        [Category("Rights")]
        [Description("Default rights level for non-privileged users")]
        public int DefaulRightsLevel { get { return defaulRightsLevel; } set { defaulRightsLevel = value; } }

        public int BossRightsLevel = 2;

        [Category("Rights")]
        [Description("Default rights level for lobby admins (mod admins)")]
        public int DefaulRightsLevelForLobbyAdmins { get { return defaulRightsLevelForLobbyAdmins; } set { defaulRightsLevelForLobbyAdmins = value; } }

        [Category("Mod and map")]
        [Description("Default game map")]
        public string DefaultMap { get { return defaultMap; } set { defaultMap = value; } }

        [Category("Mod and map")]
        [Description("Default game mod")]
        public string DefaultMod { get { return defaultMod; } set { defaultMod = value; } }

        public List<BattleRect> DefaultRectangles = new List<BattleRect>();

        [Category("Rights")]
        [Description("Default rights level for non-privileged users when there is a boss in game")]
        public int DefaultRightsLevelWithBoss { get; set; }

        [Category("Default battle settings")]
        [Description("List of units disabled by default")]
        public UnitInfo[] DisabledUnits { get { return disabledUnits; } set { disabledUnits = value; } }


        [Category("Texts")]
        [Description("Game title - appears in open game list, %1 = springie version")]
        public string GameTitle { get { return gameTitle; } set { gameTitle = value; } }

        public List<string> JoinChannels = new List<string>() { "main" };


        [Category("Basic options")]
        [Description("Should autohost kick people below min rank?")]
        public bool KickMinRank { get; set; }

        [Category("Basic options")]
        [Description("Should Springie host ladder map? Pick ladder id")]
        public int LadderId { get; set; }


        [Category("Mod and map")]
        [Description("Limit map selection to this list")]
        public string[] LimitMaps { get; set; }

        [Category("Mod and map")]
        [Description("Limit mod selection to this list")]
        public string[] LimitMods { get; set; }


        [Category("Mod and map")]
        [Description(
            "Optional mapcycle - when game ends, another map is from this list is picked. You don't have to specify exact names here, springie is using filtering capabilities to find entered maps."
            )]
        public string[] MapCycle { get { return mapCycle; } set { mapCycle = value; } }

        [Category("Basic options")]
        [Description("Maximum number of players")]
        public int MaxPlayers { get { return maxPlayers; } set { maxPlayers = value; } }


        [Category("Basic options")]
        [Description("Minimum rank to be allowed to join")]
        public int MinRank { get; set; }

        [Category("Basic options")]
        [Description("Game password")]
        public string Password { get { return password; } set { password = value; } }

        public bool PlanetWarsEnabled;
        public List<PrivilegedUser> PrivilegedUsers = new List<PrivilegedUser>();
        public bool RedirectGameChat = true;

        [Category("Basic options")]
        [Description("Should Springie use hole punching NAT traversal method? - Incompatible with gargamel mode")]
        public bool UseHolePunching { get; set; }


        [Category("Texts")]
        [Description("Welcome message - server says this when users joins. %1 = user name, %2 = user rights level, %3 = springie version")]
        public string Welcome { get { return welcome; } set { welcome = value; } }

        public void AddMissingCommands()
        {
            var addedCommands = new List<CommandConfig>();

            AddMissing(new CommandConfig("help", 0, " - lists all commands available specifically to you", 5), addedCommands);

            AddMissing(
                new CommandConfig("random",
                                  1,
                                  "<allycount> - assigns people to <allycount> random alliances, e.g. !random - makes 2 random alliances",
                                  10),
                addedCommands);

            AddMissing(
                new CommandConfig("balance",
                                  1,
                                  "<allycount> - assigns people to <allycount> rank balanced alliances, e.g. !balance - makes 2 random but balanced alliances",
                                  10),
                addedCommands);

            AddMissing(new CommandConfig("start", 1, " - starts game", 5), addedCommands);

            AddMissing(
                new CommandConfig("ring",
                                  0,
                                  "[<filters>..] - rings all unready or specific player(s), e.g. !ring - rings unready, !ring icho - rings Licho",
                                  5,
                                  new[] { TasSayEventArgs.Places.Normal, TasSayEventArgs.Places.Battle, TasSayEventArgs.Places.Game }),
                addedCommands);

            AddMissing(new CommandConfig("listmaps", 0, "[<filters>..] - lists maps on server, e.g. !listmaps altor div", 10), addedCommands);

            AddMissing(new CommandConfig("listmods", 0, "[<filters>..] - lists mods on server, e.g. !listmods absolute 2.23", 5), addedCommands);
            AddMissing(new CommandConfig("map", 2, "[<filters>..] - changes server map, eg. !map altor div"), addedCommands);

            AddMissing(new CommandConfig("manage", 1, "<minaplayer> [<maxplayers>] [<team count>] - auto manage server for min to max players"),
                       addedCommands);
            AddMissing(
                new CommandConfig("cmanage",
                                  1,
                                  "<minaplayer> [<maxplayers>] [<team count>] - auto manage server for min to max players - respects clans"),
                addedCommands);

            AddMissing(new CommandConfig("forcestart", 2, " - starts game forcibly (ignoring warnings)", 5), addedCommands);

            AddMissing(
                new CommandConfig("say",
                                  0,
                                  "<text> - says something in game",
                                  0,
                                  new[] { TasSayEventArgs.Places.Normal, TasSayEventArgs.Places.Battle, TasSayEventArgs.Places.Game }),
                addedCommands);

            AddMissing(
                new CommandConfig("force",
                                  2,
                                  " - forces game start inside game",
                                  8,
                                  new[] { TasSayEventArgs.Places.Normal, TasSayEventArgs.Places.Battle, TasSayEventArgs.Places.Game }),
                addedCommands);
            AddMissing(
                new CommandConfig("kick",
                                  3,
                                  "[<filters>..] - kicks a player",
                                  0,
                                  new[] { TasSayEventArgs.Places.Normal, TasSayEventArgs.Places.Battle, TasSayEventArgs.Places.Game }),
                addedCommands);

            AddMissing(new CommandConfig("split", 1, "<\"h\"/\"v\"> <percent> - draws with given direction and percentual size, e.g. !split h 15"),
                       addedCommands);

            AddMissing(
                new CommandConfig("corners", 1, "<\"a\"/\"b\"> <percent> - draws corners (a or b mode differ in ordering), e.g. !corners a 15"),
                addedCommands);

            AddMissing(
                new CommandConfig("exit",
                                  3,
                                  " - exits the game",
                                  0,
                                  new[] { TasSayEventArgs.Places.Normal, TasSayEventArgs.Places.Battle, TasSayEventArgs.Places.Game }),
                addedCommands);

            AddMissing(new CommandConfig("lock", 1, " - locks the game"), addedCommands);

            AddMissing(new CommandConfig("unlock", 1, " - unlocks the game"), addedCommands);

            AddMissing(new CommandConfig("fix", 1, " - fixes teamnumbers", 5), addedCommands);

            AddMissing(new CommandConfig("votemap", 0, "[<mapname>..] - starts vote for new map, e.g. !votemap altored div"), addedCommands);

            AddMissing(
                new CommandConfig("votekick",
                                  0,
                                  "[<playerame>..] - starts vote to kick a player, e.g. !votekick Licho",
                                  0,
                                  new[] { TasSayEventArgs.Places.Normal, TasSayEventArgs.Places.Battle, TasSayEventArgs.Places.Game }),
                addedCommands);

            AddMissing(
                new CommandConfig("votespec",
                                  0,
                                  "[<playername>..] - starts vote to spectate player, e.g. !votespec Licho",
                                  0,
                                  new[] { TasSayEventArgs.Places.Normal, TasSayEventArgs.Places.Battle, TasSayEventArgs.Places.Game }),
                addedCommands);

            AddMissing(new CommandConfig("voteforcestart", 0, " - starts vote to force game to start in lobby"), addedCommands);

            AddMissing(
                new CommandConfig("voteforce",
                                  0,
                                  " - starts vote to force game to start from game",
                                  0,
                                  new[] { TasSayEventArgs.Places.Normal, TasSayEventArgs.Places.Battle, TasSayEventArgs.Places.Game }),
                addedCommands);

            AddMissing(
                new CommandConfig("voteexit",
                                  0,
                                  " - starts vote to exit game",
                                  0,
                                  new[] { TasSayEventArgs.Places.Normal, TasSayEventArgs.Places.Battle, TasSayEventArgs.Places.Game }),
                addedCommands);

            AddMissing(
                new CommandConfig("vote",
                                  0,
                                  "<number> - votes for given option (works from battle only), e.g. !vote 1",
                                  0,
                                  new[] { TasSayEventArgs.Places.Battle, TasSayEventArgs.Places.Game }),
                addedCommands);

            AddMissing(new CommandConfig("rehost", 3, "[<modname>..] - rehosts game, e.g. !rehost abosol 2.23 - rehosts AA2.23"), addedCommands);
            AddMissing(
                new CommandConfig("voterehost",
                                  0,
                                  "[<modname>..] - votes to rehost game, e.g. !rehost abosol 2.23 - rehosts AA2.23",
                                  0,
                                  new[] { TasSayEventArgs.Places.Normal, TasSayEventArgs.Places.Battle, TasSayEventArgs.Places.Game }),
                addedCommands);

            AddMissing(new CommandConfig("admins", 0, " - lists admins", 5), addedCommands);

            AddMissing(new CommandConfig("setlevel", 4, "<level> <playername> - set's rights level for player.Setting to 0 deletes players."),
                       addedCommands);

            AddMissing(new CommandConfig("setcommandlevel", 4, "<level> <commandname> - configures existing command level."), addedCommands);

            AddMissing(
                new CommandConfig("maplink",
                                  0,
                                  "[<mapname>..] - looks for maplinks at unknown-files",
                                  5,
                                  new[] { TasSayEventArgs.Places.Battle, TasSayEventArgs.Places.Normal }),
                addedCommands);

            AddMissing(
                new CommandConfig("modlink",
                                  0,
                                  "[<modname>..] - looks for modlinks at unknown-files",
                                  5,
                                  new[] { TasSayEventArgs.Places.Battle, TasSayEventArgs.Places.Normal }),
                addedCommands);

            AddMissing(new CommandConfig("id", 2, "<idnumber> [<playername>..] - forces given player to an id"), addedCommands);

            AddMissing(new CommandConfig("team", 2, "<teamnumber> [<playername>..] - forces given player to a team"), addedCommands);

            AddMissing(new CommandConfig("helpall", 0, "- lists all commands known to Springie (sorted by command level)", 5), addedCommands);

            AddMissing(
                new CommandConfig("fixcolors",
                                  1,
                                  "- changes too similar colors to more different (note that color difference is highly subjective and impossible to model mathematically, so it won't always produce results satisfying for all)",
                                  10),
                addedCommands);

            AddMissing(new CommandConfig("teamcolors", 1, "- assigns similar colors to allies", 10), addedCommands);

            AddMissing(
                new CommandConfig("springie",
                                  0,
                                  "- responds with basic springie information",
                                  5,
                                  new[] { TasSayEventArgs.Places.Normal, TasSayEventArgs.Places.Battle, TasSayEventArgs.Places.Channel }),
                addedCommands);

            AddMissing(new CommandConfig("endvote", 2, "- ends current poll"), addedCommands);

            AddMissing(new CommandConfig("addbox", 1, "<left> <top> <width> <height> [<number>] - adds a new box rectangle"), addedCommands);

            AddMissing(new CommandConfig("clearbox", 1, "[<number>] - removes a box (or removes all boxes if number not specified)"), addedCommands);


            AddMissing(new CommandConfig("listoptions", 1, " - lists all mod/map options", 5), addedCommands);

            AddMissing(new CommandConfig("setoptions", 1, "<name>=<value>[,<name>=<value>] - applies mod/map options", 0), addedCommands);

            AddMissing(new CommandConfig("votesetoptions", 1, "<name>=<value>[,<name>=<value>] - starts a vote to apply mod/map options", 0),
                       addedCommands);

            AddMissing(
                new CommandConfig("cbalance",
                                  1,
                                  "[<allycount>] - assigns people to allycount random balanced alliances but attempts to put clanmates to same teams",
                                  10),
                addedCommands);

            AddMissing(
                new CommandConfig("ban",
                                  4,
                                  "<username> [<duration>] [<reason>...] - bans user username for duration (in minutes) with given reason. Duration 0 = ban for 1000 years",
                                  0),
                addedCommands);

            AddMissing(
                new CommandConfig("spawn", -2,
                                  "<configs> - creates new autohost. Example: !spawn mod=ca:stable,title=My PWN game,password=secret",
                                  0),
                addedCommands);


            AddMissing(new CommandConfig("unban", 4, "<username> - unbans user", 0), addedCommands);

            AddMissing(new CommandConfig("listbans", 0, "- lists currently banned users", 0), addedCommands);

            AddMissing(
                new CommandConfig("stats",
                                  0,
                                  "- displays statistics, use this command to get more help",
                                  5,
                                  new[] { TasSayEventArgs.Places.Normal, TasSayEventArgs.Places.Battle, TasSayEventArgs.Places.Channel }),
                addedCommands);


            AddMissing(new CommandConfig("setpassword", 4, "<newpassword> - sets server password (needs !rehost to apply)"), addedCommands);

            AddMissing(new CommandConfig("setminrank", 4, "<minrank> - sets server minimum rank (needs !rehost to apply)"), addedCommands);

            AddMissing(new CommandConfig("setmaxplayers", 4, "<maxplayers> - sets server size (needs !rehost to apply)"), addedCommands);

            AddMissing(new CommandConfig("setgametitle", 4, "<new title> - sets server game title (needs !rehost to apply)"), addedCommands);


            AddMissing(
                new CommandConfig("boss",
                                  2,
                                  "<name> - sets <name> as a new boss, use without parameter to remove any current boss. If there is a boss on server, other non-admin people have their rights reduced"),
                addedCommands);

            AddMissing(
                new CommandConfig("voteboss",
                                  0,
                                  "<name> - sets <name> as a new boss, use without parameter to remove any current boss. If there is a boss on server, other non-admin people have their rights reduced",
                                  0,
                                  new[] { TasSayEventArgs.Places.Normal, TasSayEventArgs.Places.Battle, TasSayEventArgs.Places.Game }),
                addedCommands);

            AddMissing(new CommandConfig("spec", 2, "<username> - forces player to become spectator", 0), addedCommands);

            AddMissing(new CommandConfig("predict", 0, "predicts chances of victory", 0), addedCommands);

            AddMissing(new CommandConfig("specafk", 1, "forces all AFK player to become spectators", 0), addedCommands);

            AddMissing(new CommandConfig("kickminrank", 4, "[0/1] enables or disables automatic kicking of people based upon their rank", 0),
                       addedCommands);

            AddMissing(
                new CommandConfig("cheats",
                                  2,
                                  "enables/disables .cheats in game",
                                  0,
                                  new[] { TasSayEventArgs.Places.Normal, TasSayEventArgs.Places.Battle, TasSayEventArgs.Places.Game }),
                addedCommands);

            AddMissing(
                new CommandConfig("notify",
                                  0,
                                  "springie notifies you when game ends",
                                  0,
                                  new[]
                                  {
                                      TasSayEventArgs.Places.Normal, TasSayEventArgs.Places.Battle, TasSayEventArgs.Places.Game,
                                      TasSayEventArgs.Places.Channel
                                  }),
                addedCommands);

            AddMissing(new CommandConfig("resetpassword", 0, "resets planetwars password", 0), addedCommands);

            Commands.RemoveAll(delegate(CommandConfig c) { return !addedCommands.Contains(c); });
        }


        public static int CommandComparer(CommandConfig a, CommandConfig b)
        {
            return a.Name.CompareTo(b.Name);
        }

        public void Defaults()
        {
            DefaultRectangles.Add(new BattleRect(0.0, 0.0, 1.0, 0.15));
            DefaultRectangles.Add(new BattleRect(0.0, 0.85, 1.0, 1.0));
            AddMissingCommands();
        }

        public void SetPrivilegedUser(string name, int level)
        {
            for (var i = 0; i < PrivilegedUsers.Count; ++i)
            {
                if (PrivilegedUsers[i].Name == name)
                {
                    if (level == 0)
                    {
                        PrivilegedUsers.RemoveAt(i);
                        return;
                    }
                    else
                    {
                        PrivilegedUsers[i].Level = level;
                        return;
                    }
                }
            }
            if (level > 0) PrivilegedUsers.Add(new PrivilegedUser(name, level));
        }

        public static int UserComparer(PrivilegedUser a, PrivilegedUser b)
        {
            return a.Name.CompareTo(b.Name);
        }

        void AddMissing(CommandConfig command, List<CommandConfig> addedCommands)
        {
            foreach (var c in Commands)
            {
                if (c.Name == command.Name)
                {
                    addedCommands.Add(c);
                    return;
                }
            }
            Commands.Add(command);
            addedCommands.Add(command);
        }
    }
}