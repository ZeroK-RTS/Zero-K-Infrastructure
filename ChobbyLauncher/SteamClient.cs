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


        private ConcurrentDictionary<ulong, SteamP2PPortProxy> p2pProxies = new ConcurrentDictionary<ulong, SteamP2PPortProxy>();

        private bool isDisposed;

        private Callback<GameLobbyJoinRequested_t> lobbyJoinRequestCallback;
        private Callback<P2PSessionRequest_t> newConnectionCallback;
        private Callback<GameOverlayActivated_t> overlayActivatedCallback;

        private CommandJsonSerializer steamCommandSerializer = new CommandJsonSerializer(Utils.GetAllTypesWithAttribute<SteamP2PMessageAttribute>());

        private int tickCounter;
        private Timer timer;

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

        private static int steamChannelCounter;


        private int gameHostUdpPort;


        /// <summary>
        ///     chobby request p2p game to be created
        /// </summary>
        public void PrepareToHostP2PGame(SteamHostGameRequest request)
        {
            // clear old listeners
            foreach (var oldCli in p2pProxies) oldCli.Value?.Dispose();
            p2pProxies.Clear();


            gameHostUdpPort = PickUdpPort();

            // send channel numbers to players
            foreach (var player in request.Players)
            {
                ulong playerSteamID;
                ulong.TryParse(player.SteamID, out playerSteamID);

                p2pProxies[playerSteamID] = null;
                SendSteamMessage(playerSteamID, new SteamP2PRequestPrepareProxy() {Channel = steamChannelCounter++});
            }

            // wait for response
            var startWait = DateTime.UtcNow;
            Task.Factory.StartNew(() =>
            {
                // wait 30s for all clients to respond
                while (p2pProxies.Any(x => x.Value == null))
                {
                    if (DateTime.UtcNow.Subtract(startWait).TotalSeconds > 30)
                        Listener.SendCommand(new SteamHostGameFailed()
                        {
                            CausedBySteamID = p2pProxies.Where(x => x.Value == null).Select(x => x.Key).FirstOrDefault().ToString(),
                            Reason = "Client didn't send his confirmation"
                        });

                    Task.Delay(100);
                }

                // send command to start spring to all clients
                foreach (var cli in p2pProxies)
                {
                    var player = request.Players.First(x => x.SteamID == cli.Key.ToString());

                    // tell clients to connect to server's external port/IP
                    SendSteamMessage(cli.Key,
                        new SteamP2PDirectConnectRequest()
                        {
                            Name = player.Name,
                            Engine = request.Engine,
                            Game = request.Game,
                            Map = request.Map,
                            ScriptPassword = player.ScriptPassword
                        });
                }
                
                // send command to start spring to self
                Listener.SendCommand(new SteamHostGameSuccess() { HostPort = gameHostUdpPort });
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

        private static int PickUdpPort()
        {
            using (var udp = new UdpClient(0))
            {
                return ((IPEndPoint)udp.Client.LocalEndPoint).Port;
            }
        }


        /// <summary>
        ///     host request port from client
        /// </summary>
        private void ProcessMessage(ulong remoteUser, SteamP2PRequestPrepareProxy cmd)
        {
            foreach (var cli in p2pProxies) cli.Value?.Dispose();
            p2pProxies.Clear();
            
            p2pProxies[remoteUser] = new SteamP2PPortProxy(cmd.Channel, new CSteamID(remoteUser), PickUdpPort());

            SendSteamMessage(remoteUser, new SteamP2PConfirmCreateProxy() { Channel = cmd.Channel });
        }


        /// <summary>
        ///     client sends port to host
        /// </summary>
        private void ProcessMessage(ulong remoteUser, SteamP2PConfirmCreateProxy cmd)
        {
            p2pProxies[remoteUser] = new SteamP2PPortProxy(cmd.Channel, new CSteamID(remoteUser), gameHostUdpPort); 
        }

        /// <summary>
        /// UDP is punched, client can start 
        /// </summary>
        private void ProcessMessage(ulong remoteUser, SteamP2PDirectConnectRequest cmd)
        {
            var proxy = p2pProxies.Get(remoteUser);
            if (proxy == null)
            {
                Trace.TraceWarning("P2P requested spring client start for steamID {0} which does not have proxy prepared yet", remoteUser);
            }
            else
            {
                cmd.ClientPort = proxy.LocalTargetUdpPort;
                cmd.HostPort = proxy.LocalListenUdpPort;
                cmd.HostIP = "127.0.0.1";
                Listener.SendCommand((SteamConnectSpring)cmd);
            }
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