using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Steamworks;

namespace ChobbyLauncher {
    /// <summary>
    /// UDP proxy for steam p2p
    /// </summary>
    public class SteamP2PPortProxy : IDisposable
    {
        private UdpClient udp;
        public int LocalListenUdpPort { get; }
        public int LocalTargetUdpPort { get; }

        private IPEndPoint localTargetEndpoint;
        public int SteamChannel { get; }
        private CSteamID remoteSteamID;
        private Thread udpThread;
        private Thread steamThread;

        private bool closed;


        public SteamP2PPortProxy(int steamChannel, CSteamID remoteSteamID, int localTargetUdpPort)
        {
            this.SteamChannel = steamChannel;
            this.remoteSteamID = remoteSteamID;
            LocalTargetUdpPort = localTargetUdpPort;

            udp = new UdpClient(0);
            LocalListenUdpPort = ((IPEndPoint)udp.Client.LocalEndPoint).Port;
            localTargetEndpoint = new IPEndPoint(IPAddress.Loopback, localTargetUdpPort);

            udpThread = new Thread(UdpListenThread);
            udpThread.Priority = ThreadPriority.AboveNormal;

            steamThread = new Thread(SteamListenThread);
            steamThread.Priority = ThreadPriority.AboveNormal;

            udpThread.Start(this);
            steamThread.Start(this);
        }

        private void UdpListenThread()
        {

            try
            {
                while (!closed)
                {
                    var data = udp.Receive(ref localTargetEndpoint);
                    SteamNetworking.SendP2PPacket(remoteSteamID, data, (uint)data.Length, EP2PSend.k_EP2PSendUnreliable, SteamChannel);
                }
            }
            catch (ThreadAbortException ex) { }
            catch (Exception ex)
            {
                Trace.TraceError("Error steam p2p udp listen thread: {0}", ex);
            }

        }

        private void SteamListenThread()
        {
            try
            {

                while (!closed)
                {
                    uint size;
                    while (SteamNetworking.IsP2PPacketAvailable(out size, SteamChannel))
                    {
                        var data = new byte[size];
                        CSteamID actualRemoteSteamID;
                        if (SteamNetworking.ReadP2PPacket(data, size, out size, out actualRemoteSteamID, SteamChannel))
                        {
                            if (actualRemoteSteamID == remoteSteamID) udp.Send(data, (int)size, localTargetEndpoint);
                            else
                                Trace.TraceError("Steam P2P channel {0} unexpected steamID {1}, expected {2}",
                                    SteamChannel,
                                    actualRemoteSteamID,
                                    remoteSteamID);
                        }
                    }
                    Thread.Sleep(10);
                }
            }
            catch (ThreadAbortException ex) { }
            catch (Exception ex)
            {
                Trace.TraceError("Error steam p2p steam listen thread: {0}", ex);
            }
        }


        public void Dispose()
        {
            closed = true;
            udpThread.Abort();
            steamThread.Abort();
            ((IDisposable)udp)?.Dispose();
        }
    }
}