﻿#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ZkData;

#endregion

namespace LobbyClient
{
    public class Talker: IDisposable
    {
        public const int TO_ALLIES = 252;
        public const int TO_EVERYONE = 254;
        public const int TO_EVERYONE_LEGACY = 125;
        public const int TO_SPECTATORS = 253;

        public enum SpringEventType: byte
        {
            /// Server has started ()
            SERVER_STARTED = 0,

            /// Server is about to exit ()
            SERVER_QUIT = 1,

            /// Game starts ()
            SERVER_STARTPLAYING = 2,

            /// Game has ended ()
            SERVER_GAMEOVER = 3,


            /// An information message from server (string message)
            SERVER_MESSAGE = 4,

            /// Server gave out a warning (string warningmessage)
            SERVER_WARNING = 5,

            /// Player has joined the game (uchar playernumber, string Name)
            PLAYER_JOINED = 10,

            /// Player has left (uchar playernumber, uchar reason (0: lost connection, 1: left, 2: kicked) )
            PLAYER_LEFT = 11,

            /// Player has updated its ready-state (uchar playernumber, uchar state (0: not ready, 1: ready, 2: state not changed) )
            PLAYER_READY = 12,

            /// Player has sent a chat message (uchar playernumber, string text)
            PLAYER_CHAT = 13,

            /// Player has been defeated (uchar playernumber)
            PLAYER_DEFEATED = 14,


            GAME_LUAMSG = 20, // todo use this or /wbynum 255  to send data to autohost

            //(uchar teamnumber), CTeam::Statistics(in binary form)
            GAME_TEAMSTATS = 60,


        };


        protected bool close;

        readonly int loopbackPort;
        readonly Dictionary<int, String> playerIdToName = new Dictionary<int, string>();
        int springTalkPort;

        readonly Thread thread;
        readonly UdpClient udp;


        public int LoopbackPort
        {
            get { return loopbackPort; }
        }


        public event EventHandler<SpringEventArgs> SpringEvent;


        public Talker()
        {
            udp = new UdpClient(0);
            loopbackPort = ((IPEndPoint)udp.Client.LocalEndPoint).Port;

            thread = Utils.SafeThread(Listener);
            thread.Start();
        }

        public void Dispose()
        {
            Close();
        }

        public void Close()
        {
            close = true;
            var udclose = new UdpClient();
            udclose.Send(new byte[1] { (byte)SpringEventType.SERVER_QUIT }, 1, "127.0.0.1", loopbackPort);
            thread.Join(1000);
        }


        public void SendText(string text)
        {
            if (String.IsNullOrEmpty(text)) return;
            var lines = text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var s in lines) {
                var bytes = Encoding.UTF8.GetBytes(s.Substring(0, Math.Min(s.Length, 250))); // take only first 250 characters to prevent crashes
                if (springTalkPort != 0) udp.Send(bytes, bytes.Length, "127.0.0.1", springTalkPort);
            }
        }


        public string TranslateIdToPlayerName(int playerNumber)
        {
            string val;
            playerIdToName.TryGetValue(playerNumber, out val);
            return val;
        }

        void Listener()
        {
            while (!close) {
                var endpoint = new IPEndPoint(IPAddress.Loopback, 0);
                byte[] data = null;
                try
                {
                    data = udp.Receive(ref endpoint);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("UdpListener unexpected error port {0} : {1}", endpoint.Port, ex.Message);
                }
                springTalkPort = endpoint.Port;
                if (data?.Length > 0) {
                    var sea = new SpringEventArgs();

                    sea.EventType = (SpringEventType)data[0];

                    int msgSize;
                    switch (sea.EventType) {
                        case SpringEventType.PLAYER_JOINED:
                            sea.PlayerNumber = data[1];
                            sea.PlayerName = Encoding.ASCII.GetString(data, 2, data.Length - 2);
                            playerIdToName[sea.PlayerNumber] = sea.PlayerName;
                            break;
                        case SpringEventType.PLAYER_LEFT:
                            sea.PlayerNumber = data[1];
                            sea.Param = data[2];
                            break;

                        case SpringEventType.PLAYER_READY:
                            sea.PlayerNumber = data[1];
                            if (data.Length <= 2) sea.EventType = SpringEventType.PLAYER_DEFEATED; // hack for spring 
                            else sea.Param = data[2];
                            break;

                        case SpringEventType.PLAYER_CHAT:
                            sea.PlayerNumber = data[1];
                            sea.Param = data[2];
                            sea.Text = Encoding.UTF8.GetString(data, 3, data.Length - 3);
                            break;

                        case SpringEventType.PLAYER_DEFEATED:
                            sea.PlayerNumber = data[1];
                            break;

                        case SpringEventType.SERVER_STARTPLAYING:
                            msgSize = data[1] + (data[2]<<8) + (data[3]<<16) + (data[4]<<24);
                            if (msgSize != data.Length) Trace.TraceWarning("Warning spring message size mismatch, expected {0} got {1} on STARTPLAYING", msgSize, data.Length);
                            sea.GameID = data.Skip(5).Take(16).ToArray().ToHex();
                            sea.ReplayFileName = Encoding.UTF8.GetString(data, 21, data.Length - 21);
                            break;
                            
                        case SpringEventType.SERVER_GAMEOVER:
                            msgSize = data[1];
                            sea.PlayerNumber = data[2];
                            if (msgSize != data.Length) Trace.TraceWarning("Warning spring message size mismatch, expected {0} got {1} on GAMEOVER", msgSize, data.Length);
                            sea.winningAllyTeams = data.Skip(3).Take(msgSize - 3).ToArray();
                            break;

                        case SpringEventType.SERVER_MESSAGE:

                            break;

                        case SpringEventType.GAME_LUAMSG:
                            sea.PlayerNumber = data[1];
                            sea.Param = data[4];
                            sea.Text = Encoding.ASCII.GetString(data, 5, data.Length - 5);
                            break;
                    }
                    if (sea.PlayerName == null) {
                        var translated = TranslateIdToPlayerName(sea.PlayerNumber);
                        if (translated != null) sea.PlayerName = translated;
                    }

                    if (SpringEvent != null) SpringEvent(this, sea);
                }
            }
        }

        public class SpringEventArgs: EventArgs
        {
            public SpringEventType EventType;
            public byte Param;
            public string PlayerName;
            public byte PlayerNumber;
            public string Text;
            public string GameID;
            public string ReplayFileName;
            public byte[] winningAllyTeams;
        }
    }
}
