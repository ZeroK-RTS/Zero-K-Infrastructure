using System;
using System.Collections.Generic;
using System.Text;

namespace LobbyClient.Legacy
{
	/// <summary>
	/// Basic channel information - for channel enumeration
	/// </summary>
    [Obsolete]
	public class ExistingChannel
	{
		public string name;
		public string topic;
		public int userCount;
	} ;

    [Obsolete]
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

   
  
    [Obsolete]
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

   


    [Obsolete]
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

        public override string ToString()
        {
            return $"{Channel} {UserName} {Text}";
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
    [Obsolete]
	public class TasClientException: Exception
	{
		public TasClientException() {}
		public TasClientException(string message): base(message) {}
	} ;
    [Obsolete]
	public class TasEventAgreementRecieved: EventArgs
	{
		public string Text { get; protected set; }

		public TasEventAgreementRecieved(StringBuilder builder)
		{
			Text = builder.ToString();
		}
	}
}