using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Steamworks;
using ZkData;

namespace ChobbyLauncher
{
    // see handling corrupted state exceptions https://msdn.microsoft.com/en-us/magazine/dd419661.aspx?f=255&MSPPError=-2147217396
    public class SteamClientHelper: IDisposable
    {
        int tickCounter;
        public bool IsOnline { get; private set; }
        Timer timer;
        private Callback<GameLobbyJoinRequested_t> lobbyJoinRequestCallback;


        public event Action SteamOnline = () => { };
        public event Action SteamOffline = () => { };

        public event Action<ulong> JoinFriendRequest = (steamID) => {};
        
        public void ConnectToSteam()
        {
            TimerOnElapsed(this);
            timer = new Timer(TimerOnElapsed, null, 100, 100);
        }


        public void CreateLobbyAsync(Action<ulong?> onCreated)
        {
            if (IsOnline)
            {
                var onLobbyCreated = new CallResult<LobbyCreated_t>();
                var callID = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, 16);
                onLobbyCreated.Set(callID, (t, failure) =>
                {
                    if (!failure && t.m_eResult == EResult.k_EResultOK)
                    {
                        onCreated?.Invoke(t.m_ulSteamIDLobby);
                    }
                    else
                    {
                        onCreated?.Invoke((ulong?)null);
                    }
                });
            }
        }
        
        
        [HandleProcessCorruptedStateExceptions]
        void TimerOnElapsed(object sender)
        {
            try
            {
                if (tickCounter % 300 == 0)
                {
                    if (!IsOnline)
                    {
                        if (SteamAPI.Init() && SteamAPI.IsSteamRunning())
                        {
                            IsOnline = true;

                            lobbyJoinRequestCallback = new Callback<GameLobbyJoinRequested_t>(t =>
                            {
                                JoinFriendRequest(t.m_steamIDFriend.m_SteamID);
                            });
                            SteamOnline();
                        }
                    }
                }
                if (IsOnline)
                {
                    if (SteamAPI.IsSteamRunning()) SteamAPI.RunCallbacks();
                    else
                    {
                        IsOnline = false;
                        SteamOffline();
                    }
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

            tickCounter++;
        }

        public byte[] GetClientAuthToken()
        {
            var buf = new byte[256];
            uint ticketSize;
            SteamUser.GetAuthSessionTicket(buf, buf.Length, out ticketSize);
            var truncArray = new byte[ticketSize];
            Array.Copy(buf, truncArray, truncArray.Length);
            return truncArray;


        }

        public ulong GetSteamID()
        {
            return SteamUser.GetSteamID().m_SteamID;
        }

        public string GetMyName()
        {
            return SteamFriends.GetPersonaName();
        }


        public List<ulong> GetFriends()
        {
            var ret = new List<ulong>();
            var cnt = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
            for (int i = 0; i < cnt; i++)
            {
                ret.Add(SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate).m_SteamID);
            }
            return ret;
        }


        public string GetClientAuthTokenHex()
        {
            return GetClientAuthToken().ToHex();
        }



        public void Dispose()
        {
            try
            {
                if (timer != null) timer.Dispose();
                if (IsOnline) SteamAPI.Shutdown();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }
    }
}
