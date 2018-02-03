using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using Steamworks;
using ZkData;

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

        public static int GetFreeUdpPort()
        {
            var port = GlobalConst.UdpHostingPortStart;

            var usedPorts =
                IPGlobalProperties.GetIPGlobalProperties()
                    .GetActiveUdpListeners()
                    .Where(x => x != null)
                    .Select(x => x.Port)
                    .Distinct()
                    .ToDictionary(x => x, x => true);

            while (usedPorts.ContainsKey(port)) port++;
            return port;
        }



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

            udpThread.Start();
            steamThread.Start();
        }

        private void UdpListenThread()
        {

            try
            {
                while (!closed)
                {
                    var data = udp.Receive(ref localTargetEndpoint);
                    SteamNetworking.SendP2PPacket(remoteSteamID, data, (uint)data.Length, EP2PSend.k_EP2PSendReliable, SteamChannel);
                }
            }
            catch (ThreadAbortException ex) { }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error steam p2p udp listen thread, proxy shutting down: {0}", ex.Message);
                Dispose();
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
                Trace.TraceWarning("Error steam p2p steam listen thread, proxy shutting down: {0}", ex.Message);
                Dispose();
            }
        }


        public void Dispose()
        {
            Trace.TraceInformation("Disposing steam p2p proxy, listen port {0},  target steam id {1}, channel {2}", LocalListenUdpPort, remoteSteamID, SteamChannel);
            closed = true;
            try {udpThread.Abort();} catch { }
            try {steamThread.Abort();} catch { }
            ((IDisposable)udp)?.Dispose();
        }
    }
}