using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace LobbyClient
{

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

    public class Channel
    {
        public ConcurrentDictionary<string, User> Users = new ConcurrentDictionary<string, User>();
        public string Name { get; set; }
        public string Topic { get; set; }
        public string TopicSetBy { get; set; }
        public DateTime? TopicSetDate { get; set; }
        public string Password;
    }


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
		    if (scriptPassword == null) ScriptPassword = userName;
			//ScriptPassword = scriptPassword;
		}
	}


    public class TasSayEventArgs: EventArgs
	{

		public string Channel { get; set; }
		public static TasSayEventArgs Default = new TasSayEventArgs(SayPlace.Battle, "", "", "", false);

		public bool IsEmote { get; set; }

		public SayPlace Place { get; set; }

		public string Text { get; set; }

		public string UserName { get; set; }

		public TasSayEventArgs(SayPlace place, string channel, string username, string text, bool isEmote)
		{
			Place = place;
			UserName = username;
			Text = text;
			IsEmote = isEmote;
			Channel = channel;
		}
	} ;


    public class OldNewPair<T>
    {
        public T Old;
        public T New;

        public OldNewPair(T old, T @new)
        {
            Old = old;
            New = @new;
        }
    }

    public class ChannelUserInfo
    {
        public Channel Channel;
        public List<User> Users;
    }

    public class ChannelUserRemovedInfo
    {
        public Channel Channel;
        public User User;
        public string Reason;
    }

}