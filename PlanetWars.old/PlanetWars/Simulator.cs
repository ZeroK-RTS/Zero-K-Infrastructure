using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using PlanetWars.Properties;
using PlanetWars.Utility;
using PlanetWarsShared;
using PlanetWarsShared.Springie;

namespace PlanetWars
{
	static class Simulator
	{
		static readonly List<AuthInfo> auths = new List<AuthInfo>();
		static readonly AuthInfo springieAuth = new AuthInfo("PlanetWars", "RuleAll");

		public static string MakeBattle()
		{
			ConnectIfNecessary();
			var g = GalaxyMap.Instance.Galaxy;
			const int TeamCount = 2;
			if (g.Players.Count < TeamCount) {
				return string.Format("Not enough players. ({0}/{1})", g.Players.Count, TeamCount);
			}
			var r = Program.Random;
			var playerCount = Math.Min(r.Next(TeamCount, 16), g.Players.Count);
			var specCount = Math.Min(r.Next(0, 8), g.Players.Count - playerCount);
			var factions = g.Factions.TakeRandom(2);
			var winningFactionName = r.Next(10) == 0 ? null : factions.TakeRandom().Name; // draw is possible
			var participants = new List<EndGamePlayerInfo>(playerCount + specCount);
			foreach (var faction in factions) {
				var factionIndex = factions.IndexOf(faction);
				var teamSize = (playerCount + (factionIndex%2 == 0 ? 0 : 1))/TeamCount;
				var players = g.Players.TakeRandom(teamSize);
				foreach (var player in players) {
					participants.Add(
						new EndGamePlayerInfo
						{
							Name = player.Name,
							OnVictoryTeam = player.FactionName == winningFactionName,
							Side = player.FactionName,
							Rank = player.RankOrder,
							AllyNumber = factionIndex,
							Spectator = false,
						});
				}
			}
			for (int specIndex = 0; specIndex < specCount; specIndex++) {
				participants.Add(new EndGamePlayerInfo {Spectator = true,});
			}
			var attackOptions = g.GetAttackOptions().ToArray();
			if (!attackOptions.Any()) {
				return "No attack option";
			}
            foreach (var a in attackOptions)
            {
                g.Planets.Single(p => p.MapName == a.MapName);
            }
			var attackedPlanet = attackOptions.TakeRandom();
			var result = Program.SpringieServer.SendBattleResult(springieAuth, attackedPlanet.MapName, participants);
			if (result == null) {
				throw new Exception("Error in sending battle restults.");
			}
			Debug.WriteLine(g.Events.OrderBy(e => e.Time).Last().ToHtml());
			return result.MessageToDisplay;
		}

		static void ConnectIfNecessary()
		{
			string serverString = string.Format("tcp://{0}/IServer", Settings.Default.ServerUrl);
			Program.SpringieServer = Program.SpringieServer ??
			                         (ISpringieServer)Activator.GetObject(typeof (ISpringieServer), serverString);
		}

		public static string AddPlayer()
		{
			ConnectIfNecessary();
			var faction = Program.SpringieServer.GetFactions(springieAuth).TakeRandom();
			var auth = new AuthInfo(Resources.names.Replace("\r\n", "\n").Split('\n').TakeRandom(), "1");
			var result = Program.SpringieServer.Register(springieAuth, auth, faction.Name, null);
			if (result.StartsWith("Welcome to PlanetWars!")) {
				auths.Add(auth);
				return result + " (success)";
			}
			return result + " (failed)";
		}

		static string MakeRandomString()
		{
			var chars = "abcdefgijkmnopqrstwxyz1234567890";
			var lenght = Program.Random.Next(3, 15);
			var sb = new StringBuilder(lenght);
			for (int i = 0; i < lenght; i++) {
				sb.Append(chars[Program.Random.Next(chars.Length - 1)]);
			}
			return sb.ToString();
		}

		public static string MakePlayerChangePassword()
		{
			ConnectIfNecessary();
			var newPassword = "1";
			var auth = GetRandomAuth();
			string message;
			if (Program.Server.ChangePlayerPassword(newPassword, auth, out message)) {
				auth.Password = "1";
				return message + " (success)";
			}
			return message + " (failed)";
		}

		public static string MakePlayerChangePlanetName()
		{
			ConnectIfNecessary();
			var newName = Resources.names.Replace("\r\n", "\n").Split('\n').TakeRandom();
			string result;
			if (Program.Server.ChangePlanetName(newName, GetRandomAuth(), out result))
			{
				return result + " (success)";
			}
			return result + " (failed)";
		}

		public static string MakePlayerSetMap()
		{
			ConnectIfNecessary();
			var maps = GalaxyMap.Instance.Galaxy.GetAvailableMaps();
			if (!maps.Any()) {
				return "No maps left.";
			}
			var newMap = maps.TakeRandom();
			string result;
			if (Program.Server.ChangePlanetMap(newMap, GetRandomAuth(), out result))
			{
				return result + " (success)";
			}
			return result + " (failed)";
		}

		public static string MakeCommanderInChiefSetName()
		{
			ConnectIfNecessary();
			var commanders = GalaxyMap.Instance.Galaxy.GetCommandersInChief();
			if (!commanders.Any()) {
				return "No commander.";
			}
			var commander = Program.Random.Next(0, 2) == 0
			                	? commanders.TakeRandom()
			                	: GalaxyMap.Instance.Galaxy.Players.TakeRandom();
			var auth = auths.SingleOrDefault(a => a.Login == commander.Name);
			if (auth == null) {
				return "Commander auth not available;";
			}
			var newRank = Resources.names.Replace("\r\n", "\n").Split('\n').TakeRandom();
			string result;
			;
			if (Program.Server.ChangeCommanderInChiefTitle(newRank, auth, out result))
			{
				return result + " (success)";
			}
			return result + " (failed)";
		}

		static AuthInfo GetRandomAuth()
		{
#if false // in case passwords are not all 1
			if (!auths.Any()) {
				Debug.Print("No player auth info available.");
				return new AuthInfo();
			}
			var auth = auths.TakeRandom();
			if (Program.Random.Next(0, 5) == 3) {
				return new AuthInfo(auth.Login, "wrong password");
			}

			return auth;
#endif
			return new AuthInfo(GalaxyMap.Instance.Galaxy.Players.TakeRandom().Name, "1");
		}

		public static void SimulateGame()
		{
			SimulateGame(10);
		}

		public static void SimulateGame(int iterations)
		{
			ConnectIfNecessary();
			for (int i = 0; i < iterations; i++) {
				for (int j = 0; j < 1; j++) {
					Debug.WriteLine(AddPlayer());
				}
                Program.MainForm.UpdateGalaxy();
				for (int j = 0; j < 1; j++) {
					Debug.WriteLine(MakePlayerChangePassword());
					Debug.WriteLine(MakePlayerSetMap());
					Debug.WriteLine(MakePlayerChangePlanetName());
					Debug.WriteLine(MakeCommanderInChiefSetName());
				}
				for (int j = 0; j < 5; j++) {
					Debug.WriteLine(MakeBattle());
				}
			}
		}
	}
}