using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LobbyClient;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;
using PlasmaShared;
using ZkData;
using ZeroKWeb.SpringieInterface;

namespace BattleResultProbe
{
    /// <summary>
    ///     Stores a finished battle through <see cref="BattleResultHandler.SaveSpringBattle" /> - the
    ///     real writer, not a copy - on .NET 9, against a real database.
    ///
    ///     usage: BattleResultProbe   (reads ZK_CONNECTION_STRING)
    ///
    ///     tools/dedicated-server-check.sh proves the port can drive an engine to the point of
    ///     producing a SpringBattleContext. This proves the other end: that such a context becomes
    ///     rows. Between them the only untested link is a game the engine finishes by itself.
    ///
    ///     **Everything happens in a transaction that is always rolled back**, so this can run
    ///     against the committed fixture on every pull request without changing it.
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            var failures = 0;
            void Check(bool ok, string what)
            {
                Console.WriteLine((ok ? "   ok    " : "   FAIL  ") + what);
                if (!ok) failures++;
            }

            Console.WriteLine("a finished battle, stored through the real handler:");

            // Before anything else, the lookup everything here depends on - asked about a name that
            // does NOT exist, which is the case that was broken. AccountByName falls back to a
            // case-insensitive query only when the exact match finds nothing, and that fallback used
            // string.Equals with a StringComparison, which EF Core cannot translate. So it threw
            // InvalidOperationException for exactly the "no such account" answer its callers test
            // for. Every check written before this one used a name that existed.
            using (var db = new ZkDataContext())
            {
                Account missing = null;
                var threw = false;
                try
                {
                    missing = Account.AccountByName(db, "no-such-account-" + Guid.NewGuid().ToString("N"));
                }
                catch (Exception ex)
                {
                    threw = true;
                    Console.WriteLine("         " + ex.GetType().Name + ": " + ex.Message.Split('\n')[0]);
                }
                Check(!threw, "AccountByName answers about a name that does not exist instead of throwing");
                Check(missing == null, "and the answer is null, which is what every caller tests for");
            }


            using (var db = new ZkDataContext())
            using (var transaction = db.Database.BeginTransaction())
            {
                // A context CAPTURED from a real engine, when one was handed over. Without it the
                // probe builds its own, which answers "does a context become rows" but not "is the
                // context a real game produces one this writer accepts" - and those are different
                // questions. tools/dedicated-server-check.sh writes the file; see ZK_BATTLE_CONTEXT_IN.
                var capturePath = Environment.GetEnvironmentVariable("ZK_BATTLE_CONTEXT_IN");
                if (!string.IsNullOrEmpty(capturePath) && File.Exists(capturePath))
                    return StoreCaptured(db, transaction, capturePath, Check, () => failures);

                // Fixture names, not the engine's: this asks whether a context BECOMES ROWS, and the
                // handler resolves the founder, the map and the game out of the database by name.
                var founder = db.Accounts.OrderBy(x => x.AccountID).First();
                var second = db.Accounts.OrderBy(x => x.AccountID).Skip(1).First();
                var map = db.Resources.First(x => x.TypeID == ResourceType.Map);
                var mod = db.Resources.First(x => x.TypeID == ResourceType.Mod);

                var context = new SpringBattleContext
                {
                    StartTime = DateTime.UtcNow.AddMinutes(-12),
                    Duration = 727,
                    EngineBattleID = "0123456789ABCDEF0123456789ABCDEF",
                    ReplayName = "/somewhere/probe-battle.sdfz",
                    GameEndedOk = true,
                    LobbyStartContext = new LobbyHostingContext
                    {
                        FounderName = founder.Name,
                        Map = map.InternalName,
                        Mod = mod.InternalName,
                        Title = "battle result probe",
                        EngineVersion = "105.1.1-2457-g8095d30",
                        Mode = AutohostMode.None,
                        IsMission = false,
                        IsMatchMakerGame = false,
                    },
                };
                context.ActualPlayers.Add(new BattlePlayerResult(founder.Name) { AllyNumber = 0, IsVictoryTeam = true, IsSpectator = false, LoseTime = null });
                context.ActualPlayers.Add(new BattlePlayerResult(second.Name) { AllyNumber = 1, IsVictoryTeam = false, IsSpectator = false, LoseTime = 700 });

                var battle = BattleResultHandler.SaveSpringBattle(context, db);

                Check(battle != null, "SaveSpringBattle returned a battle");
                if (battle == null) { transaction.Rollback(); return 1; }

                Check(battle.SpringBattleID != 0, "the database assigned a SpringBattleID (" + battle.SpringBattleID + ")");
                Check(battle.HostAccountID == founder.AccountID, "the founder resolved to an account by name");
                Check(battle.MapResourceID == map.ResourceID && battle.ModResourceID == mod.ResourceID, "the map and the game resolved to Resources by name");
                Check(battle.Duration == 727, "the duration survived the round trip");
                Check(battle.EngineGameID == "0123456789ABCDEF0123456789ABCDEF", "the engine's game id survived");
                Check(battle.ReplayFileName == "probe-battle.sdfz", "the replay path was reduced to a file name");

                // Read back through a second query rather than off the returned object: the point is
                // what is IN the database, and an in-memory graph can look right while nothing landed.
                var players = db.SpringBattlePlayers.Where(x => x.SpringBattleID == battle.SpringBattleID).ToList();
                Check(players.Count == 2, "both players were written (" + players.Count + ")");
                Check(players.All(x => x.SpringBattleID == battle.SpringBattleID),
                    "every player row points at the battle - this is the EF6 zero-key trap the sweep looked for, executed rather than read");
                Check(players.Any(x => x.AccountID == founder.AccountID && x.IsInVictoryTeam),
                    "the winner was stored as the winner");
                Check(players.Any(x => x.AccountID == second.AccountID && !x.IsInVictoryTeam && x.LoseTime == 700),
                    "the loser was stored with its lose time");

                var stored = db.SpringBattles.FirstOrDefault(x => x.SpringBattleID == battle.SpringBattleID);
                Check(stored != null, "the battle can be read back out of the database");

                transaction.Rollback();
            }

            // Proving the rollback, so this can run against the committed fixture forever.
            using (var db = new ZkDataContext())
            {
                var leftover = db.SpringBattles.Count(x => x.Title == "battle result probe");
                Check(leftover == 0, "nothing was left behind: the fixture is untouched");
            }

            Console.WriteLine();
            if (failures > 0) { Console.WriteLine(failures + " check(s) failed"); return 1; }
            Console.WriteLine("a battle context becomes rows, through the real handler, on .NET 9");
            return 0;
        }

        /// <summary>
        ///     Stores a context a real engine produced.
        ///
        ///     Its founder, map and game are the ones that game was played with, and the fixture has
        ///     never heard of them - so they are created here, inside the same transaction that is
        ///     rolled back. That is not a workaround: a real deployment has these rows because the
        ///     account registered and the registrar published the map, and a battle cannot be stored
        ///     without them. Creating them is stating that prerequisite out loud.
        /// </summary>
        private static int StoreCaptured(ZkDataContext db, IDbContextTransaction transaction, string path,
            Action<bool, string> check, Func<int> failures)
        {
            var context = JsonConvert.DeserializeObject<SpringBattleContext>(File.ReadAllText(path));
            check(context?.LobbyStartContext != null, "the captured context deserialised");
            if (context?.LobbyStartContext == null) { transaction.Rollback(); return 1; }

            Console.WriteLine("         captured from a real engine: founder=" + context.LobbyStartContext.FounderName
                              + ", map=" + context.LobbyStartContext.Map + ", game=" + context.LobbyStartContext.Mod);

            foreach (var name in context.ActualPlayers.Select(x => x.Name)
                         .Concat(new[] { context.LobbyStartContext.FounderName }).Distinct())
                if (Account.AccountByName(db, name) == null)
                {
                    var account = new Account { Name = name };
                    account.SetName(name);
                    account.SetPasswordHashed("not-a-real-hash");
                    account.SetAvatar();
                    db.Accounts.Add(account);
                }

            foreach (var pair in new[]
                     {
                         new { Name = context.LobbyStartContext.Map, Type = ResourceType.Map },
                         new { Name = context.LobbyStartContext.Mod, Type = ResourceType.Mod },
                     })
                if (!db.Resources.Any(x => x.InternalName == pair.Name))
                    db.Resources.Add(new Resource { InternalName = pair.Name, TypeID = pair.Type });
            db.SaveChanges();

            var battle = BattleResultHandler.SaveSpringBattle(context, db);
            check(battle != null && battle.SpringBattleID != 0, "the captured battle was stored (" + battle?.SpringBattleID + ")");
            if (battle == null) { transaction.Rollback(); return 1; }

            var players = db.SpringBattlePlayers.Where(x => x.SpringBattleID == battle.SpringBattleID).ToList();
            check(players.Count == context.ActualPlayers.Count,
                "every player the engine reported was written (" + players.Count + " of " + context.ActualPlayers.Count + ")");
            check(battle.EngineGameID == context.EngineBattleID, "the engine's own game id was stored");
            check(battle.Duration == context.Duration, "the duration the engine reported was stored");

            transaction.Rollback();

            Console.WriteLine();
            if (failures() > 0) { Console.WriteLine(failures() + " check(s) failed"); return 1; }
            Console.WriteLine("a context a REAL ENGINE produced becomes rows, through the real handler, on .NET 9");
            return 0;
        }
    }
}
