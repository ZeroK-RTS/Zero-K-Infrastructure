using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using PlanetWarsServer;
using PlanetWarsShared;
using PlanetWarsShared.Events;
using PlanetWarsShared.Springie;

namespace Tests
{
    [TestFixture]
    public class ServerTests
    {
        #region Setup/Teardown

        [SetUp]
        public void SetUp()
        {
            server = new Server(galaxy.BinaryClone()) {DontSave = true};
        }

        [TearDown]
        public void TearDown()
        {
            if (server != null) {
                server.Dispose();
            }
        }

        #endregion

        readonly AuthInfo springieAuth = new AuthInfo("PlanetWars", "RuleAll");
        Galaxy galaxy;
        Server server;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            galaxy = new Galaxy
            {
                Factions = new List<Faction> {new Faction("Core", Color.Red), new Faction("Arm", Color.Blue)},
                Players =
                    new List<Player>
                    {
                        new Player("Barack", "Arm"),
                        new Player("John", "Core"),
                        new Player("Joe", "Arm"),
                        new Player("Sarah", "Core"),
                    },
                MapNames = new List<string> {"illinois", "arizona", "delaware", "alaska", "florida", "wisconsin",},
                Planets =
                    new List<Planet>
                    {
                        new Planet(0, 0.1f, 0.1f, "Barack", "Arm")
                        {IsStartingPlanet = true, MapName = "illinois", Name = "neptune"},
                        new Planet(1, 0.2f, 0.2f, "John", "Core")
                        {IsStartingPlanet = true, MapName = "arizona", Name = "mars"},
                        new Planet(2, 0.4f, 0.2f, "Joe", "Arm") {MapName = "delaware", Name = "mercury"},
                        new Planet(3, 0.2f, 0.1f, "Sarah", "Core") {MapName = "alaska", Name = "venus"},
                        new Planet(4, 0.2f, 0.1f) {Name = "saturn"},
                        new Planet(5, 0.6f, 0.5f) {Name = "jupiter"},
                    },
            };
            for (int i = 0; i < 6; i++) {
                for (int j = 0; j < 6; j++) {
                    galaxy.Links.Add(new Link(i, j));
                }
            }
        }

        readonly AuthInfo springie2Auth = new AuthInfo("PlanetWars2", "RuleAll");

        [Test]
        public void CalculateRanks()
        {
            galaxy.Players[0].MeasuredVictories = 10;
            galaxy.Players[0].MeasuredDefeats = 1;
            galaxy.Players[1].MeasuredVictories = 5;
            galaxy.Players[1].MeasuredDefeats = 1;

            List<PlayerRankChangedEvent> lists;
            galaxy.CalculatePlayerRanks(out lists);
            Assert.AreEqual(Rank.LtColonel, galaxy.Players[0].Rank);
            Assert.AreEqual(0, galaxy.Players[0].RankOrder);
            Assert.AreEqual(Rank.LtColonel, galaxy.Players[1].Rank);
            Assert.AreEqual(0, galaxy.Players[1].RankOrder);
        }

        [Test]
        public void ChangeCommanderInChiefTitle()
        {
            var auth = new AuthInfo("test", "test");
            var newTitle = "Grand Constable";
            StringAssert.StartsWith("Welcome", server.Register(springieAuth, auth, "Core", "jupiter"));
            var player = server.Galaxy.GetPlayer("test");
            player.Rank = Rank.CommanderInChief;
            string message;
            Assert.IsTrue(server.ChangeCommanderInChiefTitle(newTitle, auth, out message), message);
            Assert.AreEqual(newTitle, player.Title);
        }

        [Test]
        public void ChangeCommanderInChiefTitleInvalidCharacters()
        {
            var auth = new AuthInfo("test", "test");
            var newTitle = "Chief|Constable";
            StringAssert.StartsWith("Welcome", server.Register(springieAuth, auth, "Core", "jupiter"));
            var player = server.Galaxy.GetPlayer("test");
            player.Rank = Rank.CommanderInChief;
            string message;
            Assert.IsFalse(server.ChangeCommanderInChiefTitle(newTitle, auth, out message), message);
            Assert.AreNotEqual(newTitle, player.Title);
        }

        [Test]
        public void ChangeCommanderInChiefTitleTooLong()
        {
            var auth = new AuthInfo("test", "test");
            var newTitle = new String('a', 100);
            StringAssert.StartsWith("Welcome", server.Register(springieAuth, auth, "Core", "jupiter"));
            var player = server.Galaxy.GetPlayer("test");
            player.Rank = Rank.CommanderInChief;
            string message;
            Assert.IsTrue(server.ChangeCommanderInChiefTitle(newTitle, auth, out message), message);
            StringAssert.StartsWith("aaaaaaa", player.Title);
        }

        [Test]
        public void ChangeCommanderInChiefTitleTooShort()
        {
            var auth = new AuthInfo("test", "test");
            var newTitle = "a";
            StringAssert.StartsWith("Welcome", server.Register(springieAuth, auth, "Core", "jupiter"));
            var player = server.Galaxy.GetPlayer("test");
            player.Rank = Rank.CommanderInChief;
            string message;
            Assert.IsFalse(server.ChangeCommanderInChiefTitle(newTitle, auth, out message), message);
            Assert.AreNotEqual(newTitle, player.Title);
        }

        [Test]
        public void ChangeMap()
        {
            var auth = new AuthInfo("test", "test");
            StringAssert.StartsWith("Welcome", server.Register(springieAuth, auth, "Core", "jupiter"));
            var newMap = server.Galaxy.Planets[5].MapName == "wisconsin" ? "florida" : "wisconsin";
            string message;
            Assert.IsTrue(server.ChangePlanetMap(newMap, auth, out message), message);
            Assert.AreEqual(newMap, server.Galaxy.Planets[5].MapName);
        }

        [Test]
        public void ChangeMapName()
        {
            var auth = new AuthInfo("test", "test");
            server.Register(springieAuth, auth, "Core", "jupiter");
            string message;
            Assert.IsTrue(server.ChangePlanetName("florida", auth, out message), message);
            Assert.AreEqual("florida", server.Galaxy.Planets[5].Name);
        }

        [Test]
        public void ChangeMapUsedMap()
        {
            var auth = new AuthInfo("test", "test");
            server.Register(springieAuth, auth, "Core", "jupiter");
            string message;
            Assert.IsFalse(server.ChangePlanetMap("illinois", auth, out message), message);
            Assert.AreNotEqual("illinois", server.Galaxy.Planets[5].MapName);
        }

        [Test]
        public void ChangeMapWrongMap()
        {
            var auth = new AuthInfo("test", "test");
            server.Register(springieAuth, auth, "Core", "jupiter");
            string message;
            Assert.IsFalse(server.ChangePlanetMap("wrong map", auth, out message), message);
            Assert.AreNotEqual("illinois", server.Galaxy.Planets[5].MapName);
        }

        [Test]
        public void ChangePassword()
        {
            var auth = new AuthInfo("test", "test");
            server.Register(springieAuth, auth, "Core", "jupiter");
            var newPassword = "test2";
            string message;
            Assert.IsTrue(server.ChangePlayerPassword(newPassword, auth, out message), message);
            Assert.IsFalse(server.ChangePlayerPassword(newPassword, auth, out message), message);
            Assert.IsTrue(
                server.ChangePlayerPassword("test", new AuthInfo(auth.Login, newPassword), out message), message);
        }

        [Test]
        public void ChangePasswordTooLong()
        {
            var auth = new AuthInfo("test", "test");
            server.Register(springieAuth, auth, "Core", "jupiter");
            var newPassword = new String('a', 200);
            string message;
            Assert.IsFalse(server.ChangePlayerPassword(newPassword, auth, out message), message);
            Assert.IsTrue(server.ChangePlayerPassword("test", auth, out message), message);
        }

        [Test]
        public void ChangePasswordTooShort()
        {
            var auth = new AuthInfo("test", "test");
            server.Register(springieAuth, auth, "Core", "jupiter");
            var newPassword = String.Empty;
            string message;
            Assert.IsFalse(server.ChangePlayerPassword(newPassword, auth, out message), message);
            Assert.IsTrue(server.ChangePlayerPassword("test", auth, out message), message);
        }

        [Test]
        public void ChangePasswordWrongPassword()
        {
            var auth = new AuthInfo("test", "test");
            server.Register(springieAuth, auth, "Core", "jupiter");
            var newPassword = "test2";
            string message;
            Assert.IsFalse(
                server.ChangePlayerPassword(newPassword, new AuthInfo("test", "wrong"), out message), message);
            Assert.IsTrue(server.ChangePlayerPassword("test", auth, out message), message);
        }

        [Test]
        public void ChangePlanetName()
        {
            var auth = new AuthInfo("test", "test");
            var newName = "newname";
            StringAssert.StartsWith("Welcome", server.Register(springieAuth, auth, "Core", "jupiter"));
            string message;
            Assert.IsTrue(server.ChangePlanetName(newName, auth, out message), message);
            Assert.AreEqual(newName, server.Galaxy.Planets[5].Name);
        }

        [Test]
        public void ChangePlanetNameAlreadyUsed()
        {
            var auth = new AuthInfo("test", "test");
            var newName = "neptune";
            StringAssert.StartsWith("Welcome", server.Register(springieAuth, auth, "Core", "jupiter"));
            string message;
            Assert.IsFalse(server.ChangePlanetName(newName, auth, out message), message);
            Assert.AreNotEqual(newName, server.Galaxy.Planets[5].Name);
        }

        [Test]
        public void ChangePlanetNameInvalidCharacters()
        {
            var auth = new AuthInfo("test", "test");
            var newName = "invalid&name";
            StringAssert.StartsWith("Welcome", server.Register(springieAuth, auth, "Core", "jupiter"));
            string message;
            Assert.IsFalse(server.ChangePlanetName(newName, auth, out message), message);
            Assert.AreNotEqual(newName, server.Galaxy.Planets[5].Name);
        }

        [Test]
        public void ChangePlanetNameTooLong()
        {
            var auth = new AuthInfo("test", "test");
            var newName = new String('a', 100);
            StringAssert.StartsWith("Welcome", server.Register(springieAuth, auth, "Core", "jupiter"));
            string message;
            Assert.IsTrue(server.ChangePlanetName(newName, auth, out message), message);
            StringAssert.StartsWith("aaaaaaa", server.Galaxy.Planets[5].Name);
        }

        [Test]
        public void ChangePlanetNameTooShort()
        {
            var auth = new AuthInfo("test", "test");
            var newName = "n";
            StringAssert.StartsWith("Welcome", server.Register(springieAuth, auth, "Core", "jupiter"));
            string message;
            Assert.IsFalse(server.ChangePlanetName(newName, auth, out message), message);
            Assert.AreNotEqual(newName, server.Galaxy.Planets[5].Name);
        }

        [Test]
        public void GetPlayersToNotify()
        {
            var mapName = "illinois";
            server.Galaxy.Players[0].ReminderLevel = ReminderLevel.MyPlanet;
            server.Galaxy.Players[0].ReminderEvent = ReminderEvent.OnBattlePreparing;
            server.Galaxy.Players[0].ReminderRoundInitiative = ReminderRoundInitiative.Offense |
                                                               ReminderRoundInitiative.Defense;
            var players = server.GetPlayersToNotify(springieAuth, mapName, ReminderEvent.OnBattlePreparing);
            Assert.That(players, Has.Member("Barack"));
            Assert.That(players, Has.Length(1));
        }

        [Test]
        public void GetPlayersToNotify2()
        {
            server.Galaxy.Turn = 1;
            var mapName = "illinois";
            server.Galaxy.Players[0].ReminderLevel = ReminderLevel.MyPlanet;
            server.Galaxy.Players[0].ReminderEvent = ReminderEvent.OnBattlePreparing;
            server.Galaxy.Players[0].ReminderRoundInitiative = ReminderRoundInitiative.Offense |
                                                               ReminderRoundInitiative.Defense;
            server.Galaxy.Players[1].ReminderLevel = ReminderLevel.AllBattles;
            server.Galaxy.Players[1].ReminderEvent = ReminderEvent.OnBattlePreparing;
            server.Galaxy.Players[1].ReminderRoundInitiative = ReminderRoundInitiative.Offense |
                                                               ReminderRoundInitiative.Defense;
            var players = server.GetPlayersToNotify(springieAuth, mapName, ReminderEvent.OnBattlePreparing);
            Assert.That(players, Has.Member("John"));
            Assert.That(players, Has.Member("Barack"));
            Assert.That(players, Has.Length(2));
        }

        [Test]
        public void GetPlayersToNotify3()
        {
            var mapName = "illinois";
            server.Galaxy.Players[0].ReminderEvent = ReminderEvent.None;
            server.Galaxy.Players[1].ReminderLevel = ReminderLevel.AllBattles;
            var players = server.GetPlayersToNotify(springieAuth, mapName, ReminderEvent.OnBattlePreparing);
            Assert.That(players, Has.Member("John"));
            Assert.That(players, Has.Length(1));
        }

        [Test]
        public void GetPlayersToNotify4()
        {
            var mapName = "illinois";
            server.Galaxy.Players[0].ReminderEvent = ReminderEvent.None;
            server.Galaxy.Players[1].ReminderLevel = ReminderLevel.AllBattles;
            var players = server.GetPlayersToNotify(springieAuth, mapName, ReminderEvent.OnBattleStarted);
            Assert.That(players, Has.Length(0));
        }

        [Test]
        public void GetPlayersToNotify5()
        {
            var mapName = "delaware";
            server.Galaxy.Players[3].ReminderEvent = ReminderEvent.OnBattleStarted | ReminderEvent.OnBattlePreparing |
                                                     ReminderEvent.OnBattleEnded;
            server.Galaxy.Players[3].ReminderLevel = ReminderLevel.AllBattles;
            var players = server.GetPlayersToNotify(springieAuth, mapName, ReminderEvent.OnBattleStarted);
            Assert.That(players, Has.Member("Sarah"));
            Assert.That(players, Has.Length(1));
        }

        [Test]
        public void GetPlayersToNotify6()
        {
            var mapName = "alaska";
            server.Galaxy.Players[3].ReminderEvent = ReminderEvent.OnBattleStarted | ReminderEvent.OnBattlePreparing |
                                                     ReminderEvent.OnBattleEnded;
            server.Galaxy.Players[1].ReminderLevel = ReminderLevel.AllBattles;
            var players = server.GetPlayersToNotify(springieAuth, mapName, ReminderEvent.OnBattlePreparing);
            Console.WriteLine(players.Contains("Sarah"));
            Assert.That(players, Has.Member("John"));
            Assert.That(players, Has.Member("Sarah"));
            Assert.That(players, Has.Length(2));
        }

        [Test]
        public void MultipleSpringiesOnBattleEnded1()
        {
            var attackedMap = server.GetAttackOptions(springieAuth).First().MapName;
            server.GetPlayersToNotify(springieAuth, attackedMap, ReminderEvent.OnBattleStarted);
            server.GetPlayersToNotify(springieAuth, attackedMap, ReminderEvent.OnBattleEnded);
            Assert.IsTrue(server.GetAttackOptions(springie2Auth).Any(p => p.MapName == attackedMap));
        }

        [Test]
        public void MultipleSpringiesOnBattleEnded2()
        {
            var attackedMap = server.GetAttackOptions(springieAuth).First().MapName;
            server.GetPlayersToNotify(springieAuth, attackedMap, ReminderEvent.OnBattleStarted);
            server.GetPlayersToNotify(springieAuth, attackedMap, ReminderEvent.OnBattleEnded);
            Assert.IsTrue(server.GetAttackOptions(springieAuth).Any(p => p.MapName == attackedMap));
        }

        [Test]
        public void MultipleSpringiesOnBattlePreparing1()
        {
            var attackedMap = server.GetAttackOptions(springieAuth).First().MapName;
            server.GetPlayersToNotify(springieAuth, attackedMap, ReminderEvent.OnBattlePreparing);
            Assert.IsTrue(server.GetAttackOptions(springie2Auth).Any(p => p.MapName != attackedMap));   
        }

        [Test]
        public void MultipleSpringiesOnBattlePreparing2()
        {
            var attackedMap = server.GetAttackOptions(springieAuth).First().MapName;
            server.GetPlayersToNotify(springieAuth, attackedMap, ReminderEvent.OnBattlePreparing);
            Assert.IsTrue(server.GetAttackOptions(springieAuth).Any(p => p.MapName == attackedMap));
        }

        [Test]
        public void MultipleSpringiesOnBattleStarted1()
        {
            var attackedMap = server.GetAttackOptions(springieAuth).First().MapName;
            server.GetPlayersToNotify(springieAuth, attackedMap, ReminderEvent.OnBattleStarted);
            Assert.IsTrue(server.GetAttackOptions(springie2Auth).All(p => p.MapName != attackedMap));
        }

        [Test]
        public void MultipleSpringiesOnBattleStarted2()
        {
            var attackedMap = server.GetAttackOptions(springieAuth).First().MapName;
            server.GetPlayersToNotify(springieAuth, attackedMap, ReminderEvent.OnBattleStarted);
            Assert.IsTrue(server.GetAttackOptions(springieAuth).Any(p => p.MapName == attackedMap));
        }

        [Test]
        public void OutNumberedTeam()
        {
            var modOptions = server.GetStartupModOptions(
                springieAuth,
                "delaware",
                new List<IPlayer>
                {
                    server.Galaxy.Players[0],
                    //server.Galaxy.Players[2], 
                    server.Galaxy.Players[1],
                    server.Galaxy.Players[3],
                });
            var text = Encoding.ASCII.GetString(Convert.FromBase64String(modOptions.Replace("_", "=")));
            Console.WriteLine(text);
        }

        [Test]
        public void Register()
        {
            var account = new AuthInfo("test", "pass");
            var result = server.Register(springieAuth, account, "Core", null);
            Assert.IsTrue(result.StartsWith("Welcome to PlanetWars! Your planet is"));
            Assert.IsTrue(server.ValidateLogin(account));
            Assert.AreEqual("Core", server.Galaxy.GetPlayer("test").FactionName);
            var planet = server.Galaxy.Planets.First(p => p.OwnerName == "test");
            Assert.IsTrue(
                server.Galaxy.Events.Any(
                    p => p.IsPlanetRelated(planet.ID) && p.IsPlayerRelated("test") && p.IsFactionRelated("Core")));
        }

        [Test]
        public void RegisterNameTaken()
        {
            server.Register(springieAuth, new AuthInfo("test", "test"), "Core", null);
            var result = server.Register(springieAuth, new AuthInfo("test", "pass"), "Arm", null);
            Assert.AreEqual("Name already taken.", result);
            Assert.AreEqual("Core", server.Galaxy.GetPlayer("test").FactionName);
            Assert.AreEqual(1, server.Galaxy.Events.Count(p => p.IsPlayerRelated("test")));
        }

        [Test]
        public void RegisterWithNoNewMaps()
        {
            var noFreePlanetGalaxy = new Galaxy
            {
                Factions = new List<Faction> {new Faction("Core", Color.Red), new Faction("Arm", Color.Blue)},
                Players = new List<Player> {new Player("Barack", "Arm"), new Player("John", "Core"),},
                MapNames = new List<string> {"illinois", "arizona",},
                Planets =
                    new List<Planet>
                    {
                        new Planet(0, 0.1f, 0.1f, "Barack", "Arm") {IsStartingPlanet = true, MapName = "illinois"},
                        new Planet(1, 0.2f, 0.2f, "John", "Core") {IsStartingPlanet = true, MapName = "arizona"},
                    }
            };
            server.Dispose();
            using (var noFreePlanetServer = new Server(noFreePlanetGalaxy) {DontSave = true}) {
                var result = noFreePlanetServer.Register(springieAuth, new AuthInfo("test", "test"), "Core", null);
                Assert.AreEqual(
                    "Welcome to PlanetWars! You are in, but you don't own any planet (no free planets left.)", result);
                Assert.IsTrue(noFreePlanetServer.Galaxy.Players.Any(p => p.Name == "test"));
                Assert.IsTrue(
                    noFreePlanetServer.Galaxy.Events.Any(p => p.IsPlayerRelated("test") && p.IsFactionRelated("Core")));
            }
        }

        [Test]
        public void RegisterWithOccupietPlanet()
        {
            var result = server.Register(springieAuth, new AuthInfo("test", "test"), "Core", "no planet has this name");
            StringAssert.Contains("not in the galaxy.", result);
            Assert.IsFalse(server.Galaxy.Players.Any(p => p.Name == "test"));
            Assert.IsFalse(server.Galaxy.Events.Any(p => p.IsPlayerRelated("test")));
        }

        [Test]
        public void RegisterWithPlanet()
        {
            var galaxy = server.State.Galaxy;
            var planetName = "jupiter";
            var result = server.Register(springieAuth, new AuthInfo("test", "test"), "Core", planetName);
            StringAssert.StartsWith("Welcome to PlanetWars! Your planet is", result);
            var planet = galaxy.Planets.First(p => p.OwnerName == "test");
            Assert.IsNotNull(planet.MapName);
            Assert.AreEqual(planet.FactionName, "Core");
            Assert.AreEqual(planet.Name, planetName);
            Assert.IsTrue(
                server.Galaxy.Events.Any(
                    p => p.IsPlanetRelated(planet.ID) && p.IsPlayerRelated("test") && p.IsFactionRelated("Core")));
        }

        [Test]
        public void RegisterWithWrongPlanet()
        {
            var result = server.Register(springieAuth, new AuthInfo("test", "test"), "Core", "no planet has this name");
            StringAssert.Contains("not in the galaxy.", result);
            Assert.IsFalse(server.Galaxy.Players.Any(p => p.Name == "test"));
            Assert.IsFalse(server.Galaxy.Events.Any(p => p.IsPlayerRelated("test")));
        }

        [Test]
        public void SendBattleResultAttackerWins()
        {
            var participants = new[]
            {
                new EndGamePlayerInfo {Name = "Barack", OnVictoryTeam = true, Side = "Arm", AllyNumber = 0,},
                new EndGamePlayerInfo {Name = "John", OnVictoryTeam = false, Side = "Core", AllyNumber = 1,},
                new EndGamePlayerInfo {Name = "Joe", Spectator = true},
                new EndGamePlayerInfo {Name = "Sarah", Spectator = true},
            };
            server.SpringieStates[springieAuth.Login] = new SpringieState(
                1, ReminderEvent.OnBattleStarted, "Arm");
            server.SpringieStates[springieAuth.Login].GameStartedStatus =
                server.SpringieStates[springieAuth.Login].BinaryClone();
            server.SendBattleResult(springieAuth, "arizona", participants);
            Assert.AreEqual("Arm", server.Galaxy.Planets[1].FactionName);
            Assert.AreEqual(1, server.Galaxy.Players[0].Victories);
            Assert.AreEqual(0, server.Galaxy.Players[0].Defeats);
            Assert.AreEqual(0, server.Galaxy.Players[1].Victories);
            Assert.AreEqual(1, server.Galaxy.Players[1].Defeats);
            Assert.AreEqual(0, server.Galaxy.Players[2].Victories);
            Assert.AreEqual(0, server.Galaxy.Players[2].Defeats);
            Assert.AreEqual(0, server.Galaxy.Players[3].Victories);
            Assert.AreEqual(0, server.Galaxy.Players[3].Defeats);
        }

        [Test]
        public void SendBattleResultDefenderWins()
        {
            var participants = new[]
            {
                new EndGamePlayerInfo {Name = "Barack", OnVictoryTeam = false, Side = "Arm", AllyNumber = 0,},
                new EndGamePlayerInfo {Name = "John", OnVictoryTeam = true, Side = "Core", AllyNumber = 1,},
                new EndGamePlayerInfo {Name = "Joe", Spectator = true},
                new EndGamePlayerInfo {Name = "Sarah", Spectator = true},
            };
            server.SpringieStates[springieAuth.Login] = new SpringieState(
                1, ReminderEvent.OnBattleStarted, "Arm");
            server.SpringieStates[springieAuth.Login].GameStartedStatus =
                server.SpringieStates[springieAuth.Login].BinaryClone();
            server.SendBattleResult(springieAuth, "arizona", participants);
            Assert.AreEqual("Core", server.Galaxy.Planets[1].FactionName);
            Assert.AreEqual(0, server.Galaxy.Players[0].Victories);
            Assert.AreEqual(1, server.Galaxy.Players[0].Defeats);
            Assert.AreEqual(1, server.Galaxy.Players[1].Victories);
            Assert.AreEqual(0, server.Galaxy.Players[1].Defeats);
            Assert.AreEqual(0, server.Galaxy.Players[2].Victories);
            Assert.AreEqual(0, server.Galaxy.Players[2].Defeats);
            Assert.AreEqual(0, server.Galaxy.Players[3].Victories);
            Assert.AreEqual(0, server.Galaxy.Players[3].Defeats);
        }

        [Test]
        public void SendBattleResultDraw()
        {
            var participants = new[]
            {
                new EndGamePlayerInfo {Name = "Barack", OnVictoryTeam = false, Side = "Arm", AllyNumber = 0,},
                new EndGamePlayerInfo {Name = "John", OnVictoryTeam = false, Side = "Core", AllyNumber = 1,},
                new EndGamePlayerInfo {Name = "Joe", Spectator = true},
                new EndGamePlayerInfo {Name = "Sarah", Spectator = true},
            };
            server.SendBattleResult(springieAuth, "arizona", participants);
            Assert.AreEqual("Core", server.Galaxy.Planets[1].FactionName);
            Assert.AreEqual(0, server.Galaxy.Players[0].Victories);
            Assert.AreEqual(0, server.Galaxy.Players[0].Defeats);
            Assert.AreEqual(0, server.Galaxy.Players[1].Victories);
            Assert.AreEqual(0, server.Galaxy.Players[1].Defeats);
            Assert.AreEqual(0, server.Galaxy.Players[2].Victories);
            Assert.AreEqual(0, server.Galaxy.Players[2].Defeats);
            Assert.AreEqual(0, server.Galaxy.Players[3].Victories);
            Assert.AreEqual(0, server.Galaxy.Players[3].Defeats);
        }
    }
}