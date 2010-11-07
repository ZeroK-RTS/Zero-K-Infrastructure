using System;
using System.Collections.Generic;
using System.Linq;
using LobbyClient;
using PlasmaShared;

namespace ZeroKLobby.MicroLobby
{
	class SpringieCommand
	{
		static readonly Dictionary<string, string> autoHosts = new Dictionary<string, string>()
		                                                       {
		                                                       	{ "sa:latest", "Boron" },
		                                                       	{ "thecursed:latest", "[GJL]autohost" },
		                                                       	{ "xta:latest", "Helium" },
		                                                       	{ "nota:latest", "Nitrogen" },
		                                                       	{ "ca-1faction:test", "Oxygen" },
		                                                       	{ "ca:stable", "Springie" },
		                                                       	{ "ca:test", "Xenon" },
		                                                       	{ "evo:test", "Lithium" },
		                                                       	{ "s44:test", "Nickel" },
		                                                       };
		public string Command { get; set; }
		public string Reply { get; set; }

		public static string GetHostSpawnerName(string gameName)
		{
			string ah;
			if (autoHosts.TryGetValue(gameName, out ah) && Program.TasClient.ExistingUsers.ContainsKey(ah)) return ah;
			else return autoHosts.Values.Shuffle().First(x => Program.TasClient.ExistingUsers.ContainsKey(x));
		}

		public static SpringieCommand Manage(int minPlayers, int maxPlayers, int teams)
		{
			return new SpringieCommand
			       {
			       	Command = String.Format("!manage {0} {1} {2}", minPlayers, maxPlayers, teams),
			       	Reply = String.Format("auto managing for {0} to {1} players and {2} teams", minPlayers, maxPlayers, teams),
			       };
		}

		public void SilentlyExcecute(string autohostName)
		{
			ActionHandler.HidePM(Command);
			ActionHandler.HidePM(Reply);
			Program.TasClient.Say(TasClient.SayPlace.User, autohostName, Command, false);
		}

		public static SpringieCommand Spawn(string modName, string title, string password)
		{
			var ret = new SpringieCommand
			          { Command = String.Format("!spawn mod={0}", modName), Reply = "I'm here! Ready to serve you! Join me!", };
			if (!String.IsNullOrEmpty(title)) ret.Command += ",title=" + title;
			if (!String.IsNullOrEmpty(password)) ret.Command += ",password=" + password;
			return ret;
		}
	}
}