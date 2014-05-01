using System;
using System.Collections.Generic;
using System.Text;

namespace LobbyClient
{
	/// <summary>
	/// Basic channel information - for channel enumeration
	/// </summary>
	public class ExistingChannel
	{
		public string name;
		public string topic;
		public int userCount;
	} ;

	public class TasEventArgs: EventArgs
	{
		List<string> serverParams = new List<string>();

		public List<string> ServerParams { get { return serverParams; } set { serverParams = value; } }

		public TasEventArgs() {}

		public TasEventArgs(params string[] serverParams)
		{
			this.serverParams = new List<string>(serverParams);
		}
	} ;


	public class BattleInfoEventArgs: EventArgs
	{
		public int BattleID { get; private set; }
		public bool IsLocked { get; private set; }
		public int MapHash { get; private set; }
		public string MapName { get; private set; }
		public int SpectatorCount { get; private set; }

		public BattleInfoEventArgs(int battleID, int spectatorCount, string mapName, int mapHash, bool isLocked)
		{
			BattleID = battleID;
			SpectatorCount = spectatorCount;
			MapName = mapName;
			MapHash = mapHash;
			IsLocked = isLocked;
		}
	}

	public class BattleUserEventArgs: EventArgs
	{
		public int BattleID { get; private set; }
		public string ScriptPassword { get; private set; }
		public string UserName { get; private set; }

		public BattleUserEventArgs(string userName, int battleID, string scriptPassword = null)
		{
			UserName = userName;
			BattleID = battleID;
			ScriptPassword = scriptPassword;
		}
	}

    public class KickedFromServerEventArgs : EventArgs
    {
        //WarningBar.DisplayWarning("You have been kicked server by " + name + ".\r\nReason: " + match.Groups[2].Value)
        public string UserName { get; private set; }
        public string Reason { get; private set; }

        public KickedFromServerEventArgs(string userName, string reason = null)
		{
			UserName = userName;
			Reason = reason;
		}
    }


    public class UserLobbyVersionEventArgs: EventArgs {
        public string Name;
        public string LobbyVersion;
        public UserLobbyVersionEventArgs() {}

        public UserLobbyVersionEventArgs(string name, string lobbyVersion)
        {
            Name = name;
            LobbyVersion = lobbyVersion;
        }
    }

    public class UserIPEventArgs: EventArgs {
        public string Name;
        public string IP;
        public UserIPEventArgs() {}

        public UserIPEventArgs(string name, string ip)
        {
            Name = name;
            IP = ip;
        }
    }

    public class UserIDEventArgs : EventArgs
    {
        public string Name;
        public int ID;
        public UserIDEventArgs() { }

        public UserIDEventArgs(string name, int id)
        {
            Name = name;
            ID = id;
        }
    }



    public class TasSayEventArgs: EventArgs
	{
		public enum Origins
		{
			Server,
			Player
		}

		public enum Places
		{
			Normal,
			Motd,
			Channel,
			Battle,
			MessageBox,
			Broadcast,
			Game,
			Server
		}

		public string Channel { get; set; }
		public static TasSayEventArgs Default = new TasSayEventArgs(Origins.Player, Places.Battle, "", "", "", false);

		public bool IsEmote { get; set; }

		public Origins Origin { get; set; }

		public Places Place { get; set; }

		public string Text { get; set; }

		public string UserName { get; set; }

		public TasSayEventArgs(Origins origin, Places place, string channel, string username, string text, bool isEmote)
		{
			Origin = origin;
			Place = place;
			UserName = username;
			Text = text;
			IsEmote = isEmote;
			Channel = channel;
		}
	} ;

	public class TasInputArgs: EventArgs
	{
		public string[] Args;
		public string Command;

		public TasInputArgs(string command, string[] args)
		{
			Command = command;
			Args = args;
		}
	} ;

	public class TasClientException: Exception
	{
		public TasClientException() {}
		public TasClientException(string message): base(message) {}
	} ;

	public class TasEventAgreementRecieved: EventArgs
	{
		public string Text { get; protected set; }

		public TasEventAgreementRecieved(StringBuilder builder)
		{
			Text = builder.ToString();
		}
	}
}