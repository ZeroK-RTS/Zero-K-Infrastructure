using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using LobbyClient;
using PlasmaShared.SpringieInterfaceReference;

namespace Springie.autohost
{
    public class MatchMakerQueue
    {
        static readonly string[] allowedCommands =
        {
            "map", "help", "ring", "vote", "saveboxes", "clearbox", "addbox", "endvote", "maplink", "y", "n",
            "votemap", "votekick", "kick", "corners", "split"
        };
        readonly AutoHost ah;
        int count;
        int lastCount;
        DateTime scheduledStart;


        bool starting;
        DateTime startingFrom;
        readonly TasClient tas;
        readonly List<string> userOrder = new List<string>();

        public MatchMakerQueue(AutoHost ah)
        {
            this.ah = ah;
            ah.Commands.Commands.RemoveAll(x => !allowedCommands.Contains(x.Name));

            tas = ah.tas;

            tas.BattleJoined += (sender, args) =>
            {
                UpdateCount();
                StopIfCountLow();
            };

            tas.BattleUserJoined += (sender, args) =>
            {
                if (tas.MyBattleID != args.BattleID) return;
                userOrder.Add(args.UserName);
                tas.Say(TasClient.SayPlace.BattlePrivate,
                    args.UserName,
                    string.Format("Hi {0}, you are {1}. in the queue", args.UserName, userOrder.Count),
                    true);
            };

            tas.BattleUserLeft += (sender, args) =>
            {
                if (tas.MyBattleID != args.BattleID) return;
                userOrder.Remove(args.UserName);

                UpdateCount();
                StopIfCountLow();
            };

            tas.BattleUserStatusChanged += (sender, args) =>
            {
                UpdateCount();
                if (count != lastCount) // user count changed
                {
                    if (count > lastCount) // users added
                    {
                        if (count >= ah.config.MinToJuggle) // enough to start
                        {
                            if (!starting) // start fresh
                            {
                                startingFrom = DateTime.Now;
                                scheduledStart = startingFrom.AddMinutes(1); // start in one minute
                                starting = true;
                            }
                            else // postpone
                            {
                                DateTime postpone = scheduledStart.AddMinutes(1);
                                DateTime deadline = startingFrom.AddMinutes(3);
                                if (postpone > deadline) scheduledStart = deadline;
                                else scheduledStart = postpone;
                            }
                            tas.Say(TasClient.SayPlace.Battle,
                                "",
                                string.Format("Queue starting in {0}s", Math.Round(scheduledStart.Subtract(DateTime.Now).TotalSeconds)),
                                true);
                        }
                        else // not enough to start
                            tas.Say(TasClient.SayPlace.Battle, "", string.Format("Queue needs {0} more people", ah.config.MinToJuggle - count), true);
                    }
                    else // users removed
                        StopIfCountLow();

                    lastCount = count;
                }
            };

            var timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += (sender, args) =>
            {
                if (starting && DateTime.Now >= scheduledStart)
                {
                    starting = false; 

                    var teams = BuildTeams();
                    if (teams == null) tas.Say(TasClient.SayPlace.Battle, "", "Queue cannot start yet because of skill differences", true);
                    else
                    {
                        foreach (var t in teams) StartSlave(t, tas.MyBattle.MapName);
                    }
                }

            };

            timer.Start();
        }

        void StartSlave(List<UserBattleStatus> team, string map)
        {
            var eloTopTwo = team.OrderByDescending(x => x.LobbyUser.EffectiveElo).Take(2).ToList();
            var title = string.Format("QuickMatch {0} {1} vs {2}", tas.MyBattle.QueueName, eloTopTwo[0], eloTopTwo[1]);

            var slave = Program.main.SpawnAutoHost(ah.config, new SpawnConfig(tas.UserName)
            {
                Engine = tas.MyBattle.EngineVersion,
                Mod = tas.MyBattle.ModName,
                Map = tas.MyBattle.MapName,
                Title = title
            });

            slave.spring.SpringExited += (sender, args) => Program.main.StopAutohost(slave); // remove after spring exits
            slave.tas.MyBattleStarted += (sender, args) => slave.tas.ChangeLock(true); // lock running game

            slave.tas.BattleJoined += (sender, args) =>
            {
                foreach (var u in team)
                {
                    tas.ForceJoinBattle(u.Name, slave.tas.MyBattleID);
                }
                slave.QuickMatchSlaveStartGame(team);
            };

            slave.Start();
        }

        List<List<UserBattleStatus>> BuildTeams()
        {
            var orderedUsers = new List<UserBattleStatus>();
            if (count < ah.config.MinToJuggle) return null; // not enough people

            foreach (var u in userOrder)
            {
                var ubs = tas.MyBattle.Users.FirstOrDefault(x => x.Name == u && x.SyncStatus == SyncStatuses.Synced);
                if (ubs != null) orderedUsers.Add(ubs);
            }

            
            var ret = new List<List<UserBattleStatus>>();

            if (ah.config.MaxEloDifference > 0) // make groups using available elo matching (used for 1v1)
            {
                while (orderedUsers.Count > 0)
                {
                    var pivot = orderedUsers[0];
                    var group = new List<UserBattleStatus>();
                    group.Add(pivot);
                    foreach (var candidate in
                        orderedUsers.Where(
                            x => !group.Contains(x) && Math.Abs(pivot.LobbyUser.EffectiveElo - x.LobbyUser.EffectiveElo) <= ah.config.MaxEloDifference)
                        )
                    {
                        group.Add(candidate);
                        if (group.Count >= ah.config.MaxToJuggle) break;
                    }

                    if (group.Count >= ah.config.MinToJuggle)
                    {
                        ret.Add(group);
                        foreach (var g in group) orderedUsers.Remove(g); // add group to result and remove grouped candidates
                    }
                    orderedUsers.Remove(pivot);
                }
            }
            else // no elo difference limits, group by elo bands oterwise trim to be disible by 2
            {
                if (count < 10 && count % 2 == 1) // for small teams trim to make even teams
                {
                   orderedUsers.Remove(orderedUsers[orderedUsers.Count - 1]);
                }

                if (orderedUsers.Count > ah.config.MaxToJuggle) // split by elo
                {
                    var eloOrder = orderedUsers.OrderBy(x => x.LobbyUser.EffectiveElo).ToList();
                    while (eloOrder.Count > 0)
                    {
                        var toMove = ah.config.MinToJuggle.Value;
                        if (eloOrder.Count < ah.config.MaxToJuggle) toMove = eloOrder.Count; // last group move all
                        else
                        {
                            if (eloOrder.Count/2.0 < ah.config.MaxPlayers && eloOrder.Count%4 == 0) toMove = eloOrder.Count/2; // split exactly in half if possible
                        }
                        var group = eloOrder.Take(toMove).ToList();
                        ret.Add(group);
                        foreach (var g in group) eloOrder.Remove(g);
                    }
                } else ret.Add(orderedUsers);
            }

            if (ret.Count == 0) return null; // return null if no groups for consistency
            return ret;
        }

        void StopIfCountLow()
        {
            if (count < ah.config.MinToJuggle) starting = false;
            tas.Say(TasClient.SayPlace.Battle, "", string.Format("Queue needs {0} more people", ah.config.MinToJuggle - count), true);
        }

        void UpdateCount()
        {
            count = tas.MyBattle.Users.Count(x => x.SyncStatus == SyncStatuses.Synced && x.Name != tas.MyBattle.Founder.Name);
        }
    }
}