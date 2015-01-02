#region using

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Springie.Client;

#endregion

namespace Springie.SpringNamespace
{
	internal class Talker : IDisposable
	{
		#region SpringEventType enum

		public enum SpringEventType : byte
		{
			/// Server has started ()
			SERVER_STARTED = 0,

			/// Server is about to exit ()
			SERVER_QUIT = 1,

			/// Game starts ()
			SERVER_STARTPLAYING = 2,

			/// Game has ended ()
			SERVER_GAMEOVER = 3,

			/// Player has joined the game (uchar playernumber, string name)
			PLAYER_JOINED = 10,

			/// Player has left (uchar playernumber, uchar reason (0: lost connection, 1: left, 2: kicked) )
			PLAYER_LEFT = 11,

			/// Player has updated its ready-state (uchar playernumber, uchar state (0: not ready, 1: ready, 2: state not changed) )
			PLAYER_READY = 12,

			/// Player has sent a chat message (uchar playernumber, string text)
			PLAYER_CHAT = 13,

			/// Player has been defeated (uchar playernumber)
			PLAYER_DEFEATED = 14,
			
			
			GAME_LUAMSG = 20 // todo use this or /wbynum 255  to send data to autohost

		} ;

		public const int TO_ALLIES = 252;
		public const int TO_SPECTATORS = 253;
		public const int TO_EVERYONE = 254;
		public const int TO_EVERYONE_LEGACY = 125;

		#endregion

		#region Fields

		protected bool close;
		private List<Battle.GrPlayer> initialPlayers;

		private int loopbackPort;
		private int springTalkPort;

		private Thread thread;
		private UdpClient udp;

		#endregion

		#region Properties

		public int LoopbackPort
		{
			get { return loopbackPort; }
		}

		#endregion

		#region Events

		public event EventHandler<SpringEventArgs> SpringEvent;

		#endregion

		#region Constructors

		public Talker()
		{
			udp = new UdpClient(0);
			loopbackPort = ((IPEndPoint) udp.Client.LocalEndPoint).Port;

			thread = new Thread(delegate() { Listener(); });
			thread.Start();
		}

		public void Dispose()
		{
			Close();
		}

		#endregion

		#region Public methods

		public void Close()
		{
			close = true;
			var udclose = new UdpClient();
			udclose.Send(new byte[2] {0, (byte) SpringEventType.SERVER_QUIT}, 1, "127.0.0.1", loopbackPort);
			thread.Join(1000);
		}


		public void SendText(string text)
		{
            if (String.IsNullOrEmpty(text)) return;
            var lines = text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string s in lines) {
                var bytes = Encoding.ASCII.GetBytes(s.Substring(0, Math.Min(s.Length, 250))); // take only first 250 characters to prevent crashes
                if (springTalkPort != 0) udp.Send(bytes, bytes.Length, "127.0.0.1", springTalkPort);
            }
		}


		public void SetPlayers(List<Battle.GrPlayer> players)
		{
			initialPlayers = players;
		}

		#endregion

		#region Other methods

		private void Listener()
		{
			while (!close) {
				var endpoint = new IPEndPoint(IPAddress.Loopback, 0);
				var data = udp.Receive(ref endpoint);
				if (endpoint.Port != loopbackPort) springTalkPort = endpoint.Port;
				if (data.Length > 0) {
					var sea = new SpringEventArgs();

					sea.EventType = (SpringEventType) data[0];

					switch (sea.EventType) {
						case SpringEventType.PLAYER_JOINED:
							sea.PlayerNumber = data[1];
							sea.PlayerName = Encoding.ASCII.GetString(data, 2, data.Length - 2);
							break;
						case SpringEventType.PLAYER_LEFT:
							sea.PlayerNumber = data[1];
							sea.Param = data[2];
							break;
						case SpringEventType.PLAYER_READY:
                            sea.PlayerNumber = data[1];
                            if (data.Length <= 2) {
                                sea.EventType = SpringEventType.PLAYER_DEFEATED; // hack for spring bug
                            } else sea.Param = data[2];
							break;

						case SpringEventType.PLAYER_CHAT:
							sea.PlayerNumber = data[1];
							sea.Param = data[2];
							sea.Text = Encoding.ASCII.GetString(data, 3, data.Length - 3);
							break;

						case SpringEventType.PLAYER_DEFEATED:
							sea.PlayerNumber = data[1];
							break;
					}
					if (sea.PlayerName == null) sea.PlayerName = TranslateIdToPlayerName(sea.PlayerNumber).name;

					if (SpringEvent != null) SpringEvent(this, sea);
				}
			}
		}

		#endregion

        public UserBattleStatus TranslateIdToPlayerName(int playerNumber)
        {
            if (playerNumber < 0 || playerNumber >= initialPlayers.Count) return null;
            return initialPlayers[playerNumber].user;
        }

		#region Nested type: SpringEventArgs

		public class SpringEventArgs : EventArgs
		{
			#region Properties

			public SpringEventType EventType;
			public byte Param;
			public string PlayerName;
			public byte PlayerNumber;
			public string Text;

			#endregion
		}

		#endregion
	}
}