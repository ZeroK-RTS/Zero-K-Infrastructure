using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Timers;
using PlasmaShared;
using ZkData;
using Steamworks;

namespace ZeroKLobby.Steam
{
    // see handling corrupted state exceptions https://msdn.microsoft.com/en-us/magazine/dd419661.aspx?f=255&MSPPError=-2147217396
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
            timer = new Timer(100);
            timer.AutoReset = false;
            timer.Elapsed += (sender, args) => TimerOnElapsed(this);
            timer.Start();
        }


        [HandleProcessCorruptedStateExceptions]
        void TimerOnElapsed(object sender)
        {
            try
            {
                timer?.Stop();
                if (tickCounter%300 == 0)
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
                timer?.Start();
            }


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


