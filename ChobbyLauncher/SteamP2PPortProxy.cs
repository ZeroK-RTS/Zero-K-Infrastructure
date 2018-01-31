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
        private int localListeUdpPort;
        private int localTargetUdpPort;

        private IPEndPoint localTargetEndpoint;
        private int steamChannel;
        private CSteamID remoteSteamID;
        private Thread udpThread;
        private Thread steamThread;

        private bool close;

        public SteamP2PPortProxy() { }

        public SteamP2PPortProxy(int steamChannel, CSteamID remoteSteamID, int localTargetUdpPort)
        {
            this.steamChannel = steamChannel;
            this.remoteSteamID = remoteSteamID;
            this.localTargetUdpPort = localTargetUdpPort;

            udp = new UdpClient(0);
            localListeUdpPort = ((IPEndPoint)udp.Client.LocalEndPoint).Port;
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
                while (!close)
                {
                    var data = udp.Receive(ref localTargetEndpoint);
                    SteamNetworking.SendP2PPacket(remoteSteamID, data, (uint)data.Length, EP2PSend.k_EP2PSendUnreliable, steamChannel);
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

                while (!close)
                {
                    uint size;
                    while (SteamNetworking.IsP2PPacketAvailable(out size, steamChannel))
                    {
                        var data = new byte[size];
                        CSteamID actualRemoteSteamID;
                        if (SteamNetworking.ReadP2PPacket(data, size, out size, out actualRemoteSteamID, steamChannel))
                        {
                            if (actualRemoteSteamID == remoteSteamID) udp.Send(data, (int)size, localTargetEndpoint);
                            else
                                Trace.TraceError("Steam P2P channel {0} unexpected steamID {1}, expected {2}",
                                    steamChannel,
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
            close = true;
            udpThread.Abort();
            steamThread.Abort();
            ((IDisposable)udp)?.Dispose();
        }
    }
}