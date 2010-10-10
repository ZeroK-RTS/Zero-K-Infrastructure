#region using

using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using LobbyClient;

#endregion

namespace CaTracker
{
    public class CaClient
    {
        ClientConnection con;
        bool mapTrack;
        string playerName = "";
        TasClient tas;


        public CaClient(TcpClient cli, TasClient tas)
        {
            try
            {
                con = new ClientConnection();
                con.ConnectionClosed += con_ConnectionClosed;
                con.CommandRecieved += con_CommandRecieved;

                this.tas = tas;
                tas.BattleUserJoined += tas_BattleUserJoined;
                tas.BattleInfoChanged += tas_BattleInfoChanged;

                con.Connect(cli);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                throw;
            }
        }


        public void Dispose()
        {
            try
            {
                if (tas != null)
                {
                    tas.BattleUserJoined -= tas_BattleUserJoined;
                    tas.BattleInfoChanged -= tas_BattleInfoChanged;
                    tas = null;
                }

                if (con != null)
                {
                    con.ConnectionClosed -= con_ConnectionClosed;
                    con.CommandRecieved -= con_CommandRecieved;
                    con.Dispose();
                    con = null;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
            }
        }

        public void SendRequest(string name)
        {
            SendCommand("G", name);
        }

        void OnBattleInfoChanged(BattleInfoEventArgs e)
        {
            var bid = e.BattleID;
            var newMap = e.MapName;
            if (mapTrack && tas.ExistingBattles[bid].Users.Any(x => x.Name == playerName)) SendRequest(newMap);
        }

        void OnBattleUserJoined(BattleUserEventArgs e)
        {
            if (mapTrack && e.UserName == playerName)
            {
                var bid = e.BattleID;
                SendRequest(tas.ExistingBattles[bid].MapName);
                SendRequest(tas.ExistingBattles[bid].ModName);
            }
        }


        void OnCommandRecieved(ServerConnectionEventArgs e)
        {
            try
            {
                switch (e.Command)
                {
                    case "TRACK":
                        if (e.Parameters.Length <= 1) Console.WriteLine("Warning, malformed TRACK_MAP command");
                        else SetMapTrackingMode(e.Parameters[0]);
                        break;
                }
            }
            catch (Exception ex)
            {
                var sb = new StringBuilder();
                foreach (var s in e.Parameters)
                {
                    if (s == null) sb.AppendFormat("null,");
                    else sb.AppendFormat("{0},", s);
                }
                Console.WriteLine("Error recieving client command {0} - {1}: {2}\n", e.Command, sb, ex);
            }
        }

        void OnConnectionClosed()
        {
            Dispose();
        }


        void SendCommand(string command, params string[] parameters)
        {
            try
            {
                if (con != null) con.SendCommand(command, parameters);
            }
            catch
            {
                // sending data to closed connection likely
            }
        }


        void SetMapTrackingMode(String newName)
        {
            mapTrack = !string.IsNullOrEmpty(newName);
            playerName = newName;

            if (mapTrack)
            {
                foreach (var b in tas.ExistingBattles.Values)
                {
                    foreach (var s in b.Users)
                    {
                        if (s.Name == playerName)
                        {
                            SendRequest(b.MapName);
                            SendRequest(b.ModName);
                            break;
                        }
                    }
                }
            }
        }

        void con_CommandRecieved(object sender, ServerConnectionEventArgs e)
        {
            OnCommandRecieved(e);
        }


        void con_ConnectionClosed(object sender, EventArgs e)
        {
            OnConnectionClosed();
        }


        void tas_BattleInfoChanged(object sender, BattleInfoEventArgs e1)
        {
            OnBattleInfoChanged(e1);
        }

        void tas_BattleUserJoined(object sender, BattleUserEventArgs e1)
        {
            OnBattleUserJoined(e1);
        }
    }
}