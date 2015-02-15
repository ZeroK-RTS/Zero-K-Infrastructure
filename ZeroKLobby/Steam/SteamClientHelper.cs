using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ZkData;
using Steamworks;

namespace ZeroKLobby.Steam
{
    public class SteamClientHelper : IDisposable
    {
        int tickCounter;
        public bool IsOnline { get; private set; }
        Timer timer;


        public event Action SteamOnline = () => { };
        public event Action SteamOffline = () => { };


        public void ConnectToSteam()
        {
            TimerOnElapsed(this);
            timer = new Timer(TimerOnElapsed, null, 100, 100);
        }


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


