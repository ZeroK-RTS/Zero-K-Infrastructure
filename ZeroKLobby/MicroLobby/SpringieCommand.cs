using System;
using System.Collections.Generic;
using System.Linq;
using LobbyClient;
using ZkData;

namespace ZeroKLobby.MicroLobby
{
	class SpringieCommand
	{
		public string Command { get; set; }
		public string Reply { get; set; }

		public static string GetHostSpawnerName(string gameName)
		{
		    return Program.TasClient.ExistingUsers.Where(x => x.Value.IsBot && (x.Value.ClientType & Login.ClientTypes.SpringieManaged) > 0).OrderByDescending(x=>x.Key.StartsWith("Springiee"))
		        .Select(x => x.Key)
		        .FirstOrDefault();
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
			Program.TasClient.Say(SayPlace.User, autohostName, Command, false);
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