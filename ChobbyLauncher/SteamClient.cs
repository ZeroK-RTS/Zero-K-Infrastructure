using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LumiSoft.Net.STUN.Client;
using PlasmaShared;
using Steamworks;
using ZkData;
using Timer = System.Timers.Timer;

namespace ChobbyLauncher
{
    // see handling corrupted state exceptions https://msdn.microsoft.com/en-us/magazine/dd419661.aspx?f=255&MSPPError=-2147217396
    public class SteamClientHelper : IDisposable
    {
        public enum OverlayOption
        {
            LobbyInvite,
            Friends,
            Community,
            Players,
            Settings,
            OfficialGameGroup,
            Achievements
        }


        private ConcurrentDictionary<ulong, SteamP2PClientPort> clientPorts = new ConcurrentDictionary<ulong, SteamP2PClientPort>();

        private bool isDisposed;

        private Callback<GameLobbyJoinRequested_t> lobbyJoinRequestCallback;
        private Callback<P2PSessionRequest_t> newConnectionCallback;
        private Callback<GameOverlayActivated_t> overlayActivatedCallback;

        private CommandJsonSerializer steamCommandSerializer = new CommandJsonSerializer(Utils.GetAllTypesWithAttribute<SteamP2PMessageAttribute>());

        private int tickCounter;
        private Timer timer;
        private UdpClient udpClient;

        public string AuthToken { get; private set; }

        public List<ulong> Friends { get; private set; }
        public bool IsOnline { get; private set; }
        public ChobbylaLocalListener Listener { get; set; }

        public ulong? LobbyID { get; set; }

        public string MySteamNameSanitized { get; set; }


        public void Dispose()
        {
            try
            {
                isDisposed = true;
                timer?.Stop();
                timer?.Dispose();
                if (IsOnline) SteamAPI.Shutdown();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        public void ConnectToSteam()
        {
            TimerOnElapsed(this);
            timer = new Timer(100);
            timer.AutoReset = false;
            timer.Elapsed += (sender, args) => TimerOnElapsed(this);
            timer.Start();
        }


        public ulong? GetLobbyOwner(ulong lobbyID)
        {
            if (IsOnline)
                foreach (var f in GetFriends())
                {
                    FriendGameInfo_t gi;
                    SteamFriends.GetFriendGamePlayed(new CSteamID(f), out gi);
                    if (gi.m_steamIDLobby.m_SteamID == lobbyID) return f;
                }
            return null;
        }


        public void InviteFriendToGame(ulong lobbyID, ulong friendID)
        {
            if (IsOnline) SteamMatchmaking.InviteUserToLobby(new CSteamID(lobbyID), new CSteamID(friendID));
        }

        public event Action<ulong> JoinFriendRequest = (steamID) => { };

        public void OpenOverlaySection(OverlayOption option)
        {
            if (IsOnline) SteamFriends.ActivateGameOverlay(option.ToString());
        }

        public void OpenOverlayWebsite(string url)
        {
            if (IsOnline) SteamFriends.ActivateGameOverlayToWebPage(url);
        }

        public event Action<bool> OverlayActivated = (b) => { };

        /// <summary>
        ///     chobby request p2p game to be created
        /// </summary>
        public void PrepareToHostP2PGame(SteamHostGameRequest request)
        {
            var socket = new UdpClient(0);
            udpClient = socket;

            // send requests to clients to resolve their external IPs
            clientPorts.Clear();
            foreach (var player in request.Players)
            {
                ulong playerSteamID;
                ulong.TryParse(player.SteamID, out playerSteamID);

                clientPorts[playerSteamID] = null;
                SendSteamMessage(playerSteamID, new SteamP2PRequestClientPort());
            }

            // resolve my own address
            var hostResolve = StunUDP(udpClient);
            if ((hostResolve == null) || (hostResolve.NetType == STUN_NetType.UdpBlocked))
            {
                Listener.SendCommand(new SteamHostGameFailed() { CausedBySteamID = GetSteamID().ToString(), Reason = "Host cannot open UDP port" });
                return;
            }

            var startWait = DateTime.UtcNow;
            Task.Factory.StartNew(() =>
            {
                // wait 30s for all clients to respond
                while (clientPorts.Any(x => x.Value == null))
                {
                    if (DateTime.UtcNow.Subtract(startWait).TotalSeconds > 30)
                        Listener.SendCommand(new SteamHostGameFailed()
                        {
                            CausedBySteamID = clientPorts.Where(x => x.Value == null).Select(x => x.Key).FirstOrDefault().ToString(),
                            Reason = "Client didn't send his UDP port"
                        });

                    Task.Delay(100);
                }

                // any client without valid ip/port ?
                var failedClient = clientPorts.Where(x => (x.Value.IP == null) || (x.Value.Port == 0)).Select(x => x.Key).FirstOrDefault();
                if (failedClient != 0)
                    Listener.SendCommand(new SteamHostGameFailed()
                    {
                        CausedBySteamID = failedClient.ToString(),
                        Reason = "Client could not resolve his NAT/firewall"
                    });

                var hostExtPort = hostResolve.PublicEndPoint.Port;
                var hostExtIP = hostResolve.PublicEndPoint.Address.ToString();
                var hostLocalPort = ((IPEndPoint)udpClient.Client.LocalEndPoint).Port;

                var buffer = new byte[] { 1, 2, 3, 4, 5, 6 };
                foreach (var cli in clientPorts)
                {
                    var player = request.Players.First(x => x.SteamID == cli.Key.ToString());

                    // send some packets to each client's external IP/port to punch NAT
                    udpClient.Send(buffer, buffer.Length, cli.Value.IP, cli.Value.Port);
                    udpClient.Send(buffer, buffer.Length, cli.Value.IP, cli.Value.Port);
                    udpClient.Send(buffer, buffer.Length, cli.Value.IP, cli.Value.Port);

                    // tell clients to connect to server's external port/IP
                    SendSteamMessage(cli.Key,
                        new SteamP2PDirectConnectRequest()
                        {
                            HostPort = hostExtPort,
                            HostIP = hostExtIP,
                            Name = player.Name,
                            Engine = request.Engine,
                            Game = request.Game,
                            Map = request.Map,
                            ScriptPassword = player.ScriptPassword
                        });
                }
                

                udpClient.Close(); // release socket

                Listener.SendCommand(new SteamHostGameSuccess() { HostPort = hostLocalPort });
            });
        }

        public void SendSteamNotifyJoin(ulong toClientID)
        {
            SendSteamMessage(toClientID, new Dummy());
            SendSteamMessage(toClientID, new SteamP2PNotifyJoin() { JoinerName = MySteamNameSanitized });
        }


        public event Action SteamOffline = () => { };
        public event Action SteamOnline = () => { };


        private void CreateLobbyAsync(Action<ulong?> onCreated)
        {
            if (IsOnline)
            {
                var onLobbyCreated = new CallResult<LobbyCreated_t>();
                var callID = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 16);
                onLobbyCreated.Set(callID,
                    (t, failure) =>
                    {
                        if (!failure && (t.m_eResult == EResult.k_EResultOK)) onCreated?.Invoke(t.m_ulSteamIDLobby);
                        else onCreated?.Invoke((ulong?)null);
                    });
            }
        }

        private byte[] GetClientAuthToken()
        {
            var buf = new byte[256];
            uint ticketSize;
            SteamUser.GetAuthSessionTicket(buf, buf.Length, out ticketSize);
            var truncArray = new byte[ticketSize];
            Array.Copy(buf, truncArray, truncArray.Length);
            return truncArray;
        }


        private string GetClientAuthTokenHex()
        {
            if (IsOnline) return GetClientAuthToken().ToHex();
            else return null;
        }


        private List<ulong> GetFriends()
        {
            if (IsOnline)
            {
                var ret = new List<ulong>();
                var cnt = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
                for (var i = 0; i < cnt; i++) ret.Add(SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate).m_SteamID);
                return ret;
            }
            return null;
        }

        private string GetMyName()
        {
            if (IsOnline) return SteamFriends.GetPersonaName();
            return null;
        }

        private ulong GetSteamID()
        {
            if (IsOnline) return SteamUser.GetSteamID().m_SteamID;
            return 0;
        }

        private void OnSteamOnline()
        {
            Trace.TraceInformation("Steam online");

            lobbyJoinRequestCallback = new Callback<GameLobbyJoinRequested_t>(t => { JoinFriendRequest(t.m_steamIDFriend.m_SteamID); });
            overlayActivatedCallback = new Callback<GameOverlayActivated_t>(t => { OverlayActivated(t.m_bActive != 0); });
            newConnectionCallback = Callback<P2PSessionRequest_t>.Create(t => SteamNetworking.AcceptP2PSessionWithUser(t.m_steamIDRemote));
            MySteamNameSanitized = Utils.StripInvalidLobbyNameChars(GetMyName());

            var ev = new EventWaitHandle(false, EventResetMode.ManualReset);
            AuthToken = GetClientAuthTokenHex();
            CreateLobbyAsync((lobbyID) =>
            {
                if (lobbyID != null) LobbyID = lobbyID;
                ev.Set();
            });
            Friends = GetFriends();
            ev.WaitOne(2000);
            SteamOnline?.Invoke();
        }

        private void ProcessMessage(ulong remoteUser, Dummy cmd)
        {
        }

        private void ProcessMessage(ulong remoteUser, SteamP2PNotifyJoin cmd)
        {
            if (Listener != null) Listener.SendCommand(new SteamFriendJoinedMe() { FriendSteamID = remoteUser.ToString(), FriendSteamName = cmd.JoinerName });
        }

        /// <summary>
        ///     host request port from client
        /// </summary>
        private void ProcessMessage(ulong remoteUser, SteamP2PRequestClientPort cmd)
        {
            var socket = new UdpClient(0);
            udpClient = socket;
            var result = StunUDP(udpClient);
            if ((result == null) || (result.NetType == STUN_NetType.UdpBlocked)) SendSteamMessage(remoteUser, new SteamP2PClientPort());
            else
                SendSteamMessage(remoteUser,
                    new SteamP2PClientPort() { Port = result.PublicEndPoint.Port, IP = result.PublicEndPoint.Address.ToString() });
        }


        /// <summary>
        ///     client sends port to host
        /// </summary>
        private void ProcessMessage(ulong remoteUser, SteamP2PClientPort cmd)
        {
            clientPorts[remoteUser] = cmd;
        }

        /// <summary>
        /// UDP is punched, client can start 
        /// </summary>
        private void ProcessMessage(ulong remoteUser, SteamP2PDirectConnectRequest cmd)
        {
            cmd.ClientPort = ((IPEndPoint)udpClient.Client.LocalEndPoint).Port;
            udpClient.Close();
            Listener.SendCommand((SteamConnectSpring)cmd);
        }


        /// <summary>
        ///     Sends steam message to target client
        /// </summary>
        /// <param name="targetClientID"></param>
        /// <param name="message"></param>
        private void SendSteamMessage<T>(ulong targetClientID, T message)
        {
            if (IsOnline)
            {
                var cmd = steamCommandSerializer.SerializeToLine(message);
                Trace.TraceInformation("SteamP2P >> {0} : {1}", targetClientID, cmd);
                var data = Encoding.UTF8.GetBytes(cmd);
                SteamNetworking.SendP2PPacket(new CSteamID(targetClientID), data, (uint)data.Length, EP2PSend.k_EP2PSendReliable);
            }
        }

        private static STUN_Result StunUDP(UdpClient socket)
        {
            try
            {
                socket.AllowNatTraversal(true);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Allow nat traversal failed: {0}", ex.Message);
            }
            var servers = new[] { "stun.l.google.com:19302", "stun.services.mozilla.com", "stunserver.org" };
            foreach (var server in servers)
            {
                var host = server.Split(':').FirstOrDefault();
                try
                {
                    int port;
                    if (!int.TryParse(server.Split(':').LastOrDefault(), out port) || (port == 0)) port = 3478;

                    return STUN_Client.Query(host, port, socket.Client);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("STUN request to {0} failed : {1}", host, ex);
                }
            }
            return null;
        }


        [HandleProcessCorruptedStateExceptions]
        private void TimerOnElapsed(object sender)
        {
            try
            {
                if (isDisposed) return;
                timer?.Stop();
                if (tickCounter % 50 == 0)
                    if (!IsOnline)
                        if (SteamAPI.Init() && SteamAPI.IsSteamRunning())
                        {
                            IsOnline = true;

                            OnSteamOnline();
                        }
                if (IsOnline)
                    if (SteamAPI.IsSteamRunning())
                    {
                        SteamAPI.RunCallbacks();

                        uint networkSize;
                        while (SteamNetworking.IsP2PPacketAvailable(out networkSize))
                            try
                            {
                                var buf = new byte[networkSize];
                                CSteamID remoteUser;
                                if (SteamNetworking.ReadP2PPacket(buf, networkSize, out networkSize, out remoteUser))
                                {
                                    var str = Encoding.UTF8.GetString(buf);
                                    Trace.TraceInformation("SteamP2P << {0} : {1}", remoteUser.m_SteamID, str);
                                    dynamic cmd = steamCommandSerializer.DeserializeLine(str);
                                    ProcessMessage(remoteUser.m_SteamID, cmd);
                                }
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceError("Error processing steam P2P message: {0}", ex);
                            }
                    }
                    else
                    {
                        IsOnline = false;
                        SteamOffline();
                    }
            }
            catch (DllNotFoundException ex)
            {
                Trace.TraceWarning("Error initializing steam, disabling susbystem: {0} library not found", ex.Message);
                if (timer != null) timer.Dispose();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
            finally
            {
                tickCounter++;
                if (!isDisposed) timer?.Start();
            }
        }
    }
}