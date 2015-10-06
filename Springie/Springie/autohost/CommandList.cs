using System.Collections.Generic;
using System.Linq;
using LobbyClient;
using PlasmaShared;

namespace Springie.autohost
{
    public class CommandList
    {
        public List<CommandConfig> Commands = new List<CommandConfig>();

        public CommandList(AhConfig config)
        {
            AddMissing(new CommandConfig("help", 0, " - lists all commands available specifically to you", 5) { AllowSpecs = true});

            AddMissing(new CommandConfig("random",
                                         1,
                                         "<allycount> - assigns people to <allycount> random alliances, e.g. !random - makes 2 random alliances",
                                         10));

            AddMissing(new CommandConfig("balance",
                                         1,
                                         "<allycount> - assigns people to <allycount> rank balanced alliances, e.g. !balance - makes 2 random but balanced alliances",
                                         10));

            AddMissing(new CommandConfig("start", 1, " - starts game", 5));

            AddMissing(new CommandConfig("ring",
                                         1,
                                         "[<filters>..] - rings all unready or specific player(s), e.g. !ring - rings unready, !ring icho - rings Licho",
                                         5,
                                         new[] { SayPlace.User, SayPlace.Battle, SayPlace.Game })
                                         { AllowSpecs = true});

            AddMissing(new CommandConfig("listmaps", 1, "[<filters>..] - lists maps on server, e.g. !listmaps altor div", 10));

            AddMissing(new CommandConfig("listmods", 1, "[<filters>..] - lists games on server, e.g. !listmods absolute 2.23", 5));
            AddMissing(new CommandConfig("map", 3, "[<filters>..] - changes server map, eg. !map altor div"));
            AddMissing(new CommandConfig("mapremote", 0, "[<filters>..] - changes server map, eg. !map altor div"));    // see https://github.com/ZeroK-RTS/Zero-K-Infrastructure/issues/756

            AddMissing(new CommandConfig("forcestart", 3, " - starts game forcibly (ignoring warnings)", 5));

            AddMissing(new CommandConfig("say",
                                         3,
                                         "<text> - says something in game",
                                         0,
                                         new[] { SayPlace.User, SayPlace.Battle, SayPlace.Game }));

            AddMissing(new CommandConfig("force",
                                         3,
                                         " - forces game start inside game",
                                         8,
                                         new[] { SayPlace.User, SayPlace.Battle, SayPlace.Game }));
            AddMissing(new CommandConfig("kick",
                                         3,
                                         "[<filters>..] - kicks a player",
                                         0,
                                         new[] { SayPlace.User, SayPlace.Battle, SayPlace.Game }));

            AddMissing(new CommandConfig("split", 1, "<\"h\"/\"v\"> <percent> - draws with given direction and percentual size, e.g. !split h 15"));

            AddMissing(new CommandConfig("transmit", 0, "Internal command transfer to ingame") { AllowSpecs = true});

            AddMissing(new CommandConfig("corners", 1, "<\"a\"/\"b\"> <percent> - draws corners (a or b mode differ in ordering), e.g. !corners a 15"));

            AddMissing(new CommandConfig("exit",
                                         3,
                                         " - exits the game",
                                         0,
                                         new[] { SayPlace.User, SayPlace.Battle, SayPlace.Game }));

            AddMissing(new CommandConfig("votemap", 1, "[<mapname>..] - starts vote for new map, e.g. !votemap altored div"));

            AddMissing(new CommandConfig("votekick",
                                         2,
                                         "[<playerame>..] - starts vote to kick a player, e.g. !votekick Licho",
                                         0,
                                         new[] { SayPlace.User, SayPlace.Battle, SayPlace.Game }));

            AddMissing(new CommandConfig("votespec",
                                         2,
                                         "[<playername>..] - starts vote to spectate player, e.g. !votespec Licho",
                                         0,
                                         new[] { SayPlace.User, SayPlace.Battle, SayPlace.Game }));

            AddMissing(new CommandConfig("votesplitplayers",
                                         2,
                                         "- starts vote to split the game into 2",
                                         0,
                                         new[] { SayPlace.User, SayPlace.Battle, SayPlace.Game }) { AllowSpecs = true});

            
            AddMissing(new CommandConfig("splitplayers",
                                         3,
                                         " - splots players to 2 hosts based on their elo",
                                         2,
                                         new[] { SayPlace.User, SayPlace.Battle, SayPlace.Game }));

            AddMissing(new CommandConfig("voteforcestart", 2, " - starts vote to force game to start in lobby"));

            AddMissing(new CommandConfig("voteforce",
                                         2,
                                         " - starts vote to force game to start from game",
                                         0,
                                         new[] { SayPlace.User, SayPlace.Battle, SayPlace.Game }));

            AddMissing(new CommandConfig("voteexit",
                                         2,
                                         " - starts vote to exit game",
                                         0,
                                         new[] { SayPlace.User, SayPlace.Battle, SayPlace.Game }));

            AddMissing(new CommandConfig("voteresign",
                                                     0,
                                                     " - starts a vote to resign game",
                                                     0,
                                                     new[] { SayPlace.User, SayPlace.Battle, SayPlace.Game }));

            AddMissing(new CommandConfig("vote",
                                         0,
                                         "<number> - votes for given option (works from battle only), e.g. !vote 1",
                                         0,
                                         new[] { SayPlace.Battle, SayPlace.Game }) { AllowSpecs = true});
            AddMissing(new CommandConfig("y",
                                         0,
                                         "- votes for given option 1 (works from battle only), e.g. !y; !vote 1",
                                         0,
                                         new[] { SayPlace.Battle, SayPlace.Game }) { AllowSpecs = true});
            AddMissing(new CommandConfig("n",
                                         0,
                                         "- votes for given option 2 (works from battle only), e.g. !n; !vote 2",
                                         0,
                                         new[] { SayPlace.Battle, SayPlace.Game }) { AllowSpecs = true});

            AddMissing(new CommandConfig("rehost", 3, "[<modname>..] - rehosts game, e.g. !rehost abosol 2.23 - rehosts AA2.23"));

            AddMissing(new CommandConfig("maplink",
                                         0,
                                         "[<mapname>..] - looks for maplinks at unknown-files",
                                         5,
                                         new[] { SayPlace.Battle, SayPlace.User }));

            AddMissing(new CommandConfig("modlink",
                                         0,
                                         "[<modname>..] - looks for modlinks at unknown-files",
                                         5,
                                         new[] { SayPlace.Battle, SayPlace.User }));

            AddMissing(new CommandConfig("team", 3, "<teamnumber> [<playername>..] - forces given player to a team"));

            AddMissing(new CommandConfig("adduser", 0, "<pw> - technical command used for mid-game spectator join", 0, new[] { SayPlace.Battle, SayPlace.User }) { AllowSpecs = true});

            AddMissing(new CommandConfig("helpall", 0, "- lists all commands known to Springie (sorted by command level)", 5) { AllowSpecs = true});

            AddMissing(new CommandConfig("setengine", 3, "[version] - sets a new spring version", 2));

            AddMissing(new CommandConfig("springie",
                                         0,
                                         "- responds with basic springie information",
                                         5,
                                         new[] { SayPlace.User, SayPlace.Battle, SayPlace.Channel })
                                         { AllowSpecs = true});

            AddMissing(new CommandConfig("endvote", 1, "- ends current poll"));

            AddMissing(new CommandConfig("addbox", 1, "<left> <top> <width> <height> [<number>] - adds a new box rectangle"));

            AddMissing(new CommandConfig("clearbox", 1, "[<number>] - removes a box (or removes all boxes if number not specified)"));

            AddMissing(new CommandConfig("listoptions", 1, " - lists all mod/map options", 5));

            AddMissing(new CommandConfig("setoptions", 3, "<name>=<value>[,<name>=<value>] - applies mod/map options", 0));

            AddMissing(new CommandConfig("votesetoptions", 1, "<name>=<value>[,<name>=<value>] - starts a vote to apply mod/map options", 0));

            AddMissing(new CommandConfig("resetoptions", 3, " - sets default mod/map options", 0));

            AddMissing(new CommandConfig("voteresetoptions", 1, " - starts a vote to set default mod/map options", 0));

            AddMissing(new CommandConfig("cbalance",
                                         1,
                                         "[<allycount>] - assigns people to allycount random balanced alliances but attempts to put clanmates to same teams",
                                         10));

            AddMissing(new CommandConfig("spawn",
                                         -2,
                                         "<configs> - creates new autohost. Example: !spawn mod=ca:stable,title=My PWN game,password=secret",
                                         0)
                                         { AllowSpecs = true});

            AddMissing(new CommandConfig("setpassword", 3, "<newpassword> - sets server password (needs !rehost to apply)"));

            AddMissing(new CommandConfig("setmaxplayers", 3, "<maxplayers> - sets server size (needs !rehost to apply)"));

            AddMissing(new CommandConfig("setgametitle", 3, "<new title> - sets server game title (needs !rehost to apply)"));

            AddMissing(new CommandConfig("boss",
                                         3,
                                         "<name> - sets <name> as a new boss, use w5ithout parameter to remove any current boss. If there is a boss on server, other non-admin people have their rights reduced"));
 

            AddMissing(new CommandConfig("spec", 3, "<username> - forces player to become spectator", 0));

            AddMissing(new CommandConfig("predict", 0, "predicts chances of victory", 0) { AllowSpecs = true});

            AddMissing(new CommandConfig("specafk", 2, "forces all AFK player to become spectators", 0));

            AddMissing(new CommandConfig("cheats",
                                         3,
                                         "enables/disables .cheats in game",
                                         0,
                                         new[] { SayPlace.User, SayPlace.Battle, SayPlace.Game }));
            
            AddMissing(new CommandConfig("hostsay",
                                         3,
                                         "says something as host, useful for /nocost etc",
                                         0,
                                         new[] { SayPlace.User, SayPlace.Battle, SayPlace.Game }));

            AddMissing(new CommandConfig("notify",
                                         0,
                                         "springie notifies you when game ends",
                                         0,
                                         new[]
                                         {
                                             SayPlace.User, SayPlace.Battle, SayPlace.Game,
                                             //SayPlace.Channel // this does silly stuff with !notify in #zk
                                         })
                                         { AllowSpecs = true});

            AddMissing(new CommandConfig("saveboxes", 4, "- saves boxes for current map"));
            AddMissing(new CommandConfig("move", 4, "<where> - moves players to a new host"));
            AddMissing(new CommandConfig("votemove", 2, "<where> - moves players to a new host") { AllowSpecs = true});

            if (config != null && config.CommandLevels != null)
            {
                foreach (var c in config.CommandLevels) {
                    var entry = Commands.FirstOrDefault(x => x.Name == c.Command);
                    if (entry != null) entry.Level = c.Level;
                }
            }
        }


        void AddMissing(CommandConfig command)
        {
            Commands.Add(command);
        }
    }
}
