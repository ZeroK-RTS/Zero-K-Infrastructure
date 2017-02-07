using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameAnalyticsSDK.Net;
using PlasmaDownloader;
using PlasmaShared;
using ZkData;

namespace ChobbyLauncher
{

    public class ChobbylaLocalListener
    {
        private CommandJsonSerializer serializer;
        private TcpTransport transport;
        private Chobbyla chobbyla;
        private TextToSpeechBase tts;


        public ChobbylaLocalListener(Chobbyla chobbyla)
        {
            this.chobbyla = chobbyla;
            serializer = new CommandJsonSerializer(Utils.GetAllTypesWithAttribute<ChobbyMessageAttribute>());
            tts = TextToSpeechBase.Create();
            chobbyla.Steam.JoinFriendRequest += SteamOnJoinFriendRequest;
            chobbyla.Steam.OverlayActivated += SteamOnOverlayActivated;
        }

        private void SteamOnOverlayActivated(bool b)
        {
            SendCommand(new SteamOverlayChanged() { IsActive = b });
        }

        private void SteamOnJoinFriendRequest(ulong friendSteamID)
        {
            SendCommand(new SteamJoinFriend() { FriendSteamID = friendSteamID.ToString() });
        }


        /// <summary>
        /// Starts listening on a new thread
        /// </summary>
        /// <returns>listen port</returns>
        public int StartListening()
        {
            var listener = new TcpListener(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Server.SetSocketOption(SocketOptionLevel.Socket,
                SocketOptionName.Linger,
                new LingerOption(GlobalConst.TcpLingerStateEnabled, GlobalConst.TcpLingerStateSeconds));
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 0);
            listener.Start();

            var th = new Thread(() =>
            {
                SynchronizationContext.SetSynchronizationContext(null);
                var tcp = listener.AcceptTcpClient();
                transport = new TcpTransport(tcp);
                transport.ConnectAndRun(OnCommandReceived, OnConnected, OnConnectionClosed);
            });
            th.Start();
            return ((IPEndPoint)listener.Server.LocalEndPoint).Port;
        }

        public async Task Process(OpenUrl args)
        {
            try
            {
                System.Diagnostics.Process.Start(args.Url);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error opening URL {0} : {1}", args.Url, ex);
            }
        }

        
        public async Task Process(Alert args)
        {
            try
            {

                if (Environment.OSVersion.Platform != PlatformID.Unix)
                {
                    // todo implement for linux with #define NET_WM_STATE_DEMANDS_ATTENTION=42
                    var info = new WindowsApi.FLASHWINFO();
                    info.hwnd = chobbyla.process.MainWindowHandle;
                    info.dwFlags = 0x0000000C | 0x00000003; // flash all until foreground
                    info.cbSize = Convert.ToUInt32(Marshal.SizeOf(info));
                    WindowsApi.FlashWindowEx(ref info);

                    SystemSounds.Exclamation.Play();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error alerting {0} : {1}", args.Message, ex);
            }
        }



        public async Task Process(Restart args)
        {
            try
            {
                System.Diagnostics.Process.Start(Application.ExecutablePath);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error restarting: {0}", ex);
            }
        }

        public async Task Process(TtsVolume args)
        {
            try
            {
                tts?.SetVolume(args.Volume);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error setting TTS volume {0}: {1}",args?.Volume, ex);
            }
        }

        public async Task Process(TtsSay args)
        {
            try
            {
                tts?.Say(args.Name ?? "", args.Text);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error speaking TTS {0}, {1}: {2}", args?.Name, args?.Text, ex);
            }
        }



        public async Task Process(OpenFolder args)
        {
            try
            {
                System.Diagnostics.Process.Start(chobbyla.paths.WritableDirectory);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error opening folder {0} : {1}", args.Folder, ex);
            }
        }



        public async Task SendCommand<T>(T data)
        {
            try
            {
                var line = serializer.SerializeToLine(data);
                await transport.SendLine(line);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Wrapper error sending {0} : {1}", data, ex);
            }
        }


        private async Task OnCommandReceived(string line)
        {
            try
            {
                dynamic obj = serializer.DeserializeLine(line);
                await Process(obj);
            }
            catch (Exception ex)
            {
                Trace.TraceError("{0} error processing line {1} : {2}", this, line, ex);
            }
        }


        public async Task Process(DownloadFile args)
        {
            try
            {
                DownloadType type;
                if (string.IsNullOrEmpty(args.FileType) || !Enum.TryParse(args.FileType, out type)) type = DownloadType.NOTKNOWN;
                var down = chobbyla.downloader.GetResource(type, args.Name);
                ReportDownloadResult(args, down); // executes as async
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error downloading file {0} : {1}", args?.Name, ex);
            }
        }

        public async Task Process(SteamOpenOverlaySection args)
        {
            try
            {
                chobbyla.Steam.OpenOverlaySection(args.Option ?? SteamClientHelper.OverlayOption.LobbyInvite);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error opening overlay page {0} : {1}", args?.Option, ex);
            }
        }

        public async Task Process(SteamOpenOverlayWebsite args)
        {
            try
            {
                if (chobbyla.Steam.IsOnline) chobbyla.Steam.OpenOverlayWebsite(args.Url);
                else System.Diagnostics.Process.Start(args.Url);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error opening overlay url {0} : {1}", args.Url, ex);
            }
        }

        public async Task Process(SteamInviteFriendToGame args)
        {
            try
            {
                if (chobbyla.LobbyID != null) chobbyla.Steam.InviteFriendToGame(chobbyla.LobbyID.Value, ulong.Parse(args.SteamID));
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error inviting friend to game {0} : {1}", args?.SteamID, ex);
            }
        }


        public async Task Process(GaAddErrorEvent args)
        {
            try
            {
                GameAnalytics.AddErrorEvent(args.Severity, args.Message);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error adding GA error event {0} : {1}", args?.Message, ex);
            }
        }


        public async Task Process(GaAddDesignEvent args)
        {
            try
            {
                if (args.Value != null) GameAnalytics.AddDesignEvent(args.EventID, args.Value.Value);
                else GameAnalytics.AddDesignEvent(args.EventID);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error adding GA design event {0} : {1}", args?.EventID, ex);
            }
        }


        public async Task Process(GaAddProgressionEvent args)
        {
            try
            {
                if (args.Score != null)
                {
                    if (!string.IsNullOrEmpty(args.Progression3)) GameAnalytics.AddProgressionEvent(args.Status, args.Progression1, args.Progression2, args.Progression3, args.Score.Value);
                    else if (!string.IsNullOrEmpty(args.Progression2)) GameAnalytics.AddProgressionEvent(args.Status, args.Progression1, args.Progression2, args.Score.Value);
                    else GameAnalytics.AddProgressionEvent(args.Status, args.Progression1, args.Score.Value);
                }
                else
                {
                    if (!string.IsNullOrEmpty(args.Progression3)) GameAnalytics.AddProgressionEvent(args.Status, args.Progression1, args.Progression2, args.Progression3);
                    else if (!string.IsNullOrEmpty(args.Progression2)) GameAnalytics.AddProgressionEvent(args.Status, args.Progression1, args.Progression2);
                    else GameAnalytics.AddProgressionEvent(args.Status, args.Progression1);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error adding GA progression event {0}", ex);
            }
        }

        private async Task ReportDownloadResult(DownloadFile args, Download down)
        {
            try
            {
                if (down != null) await down.WaitHandle.AsTask(TimeSpan.FromMinutes(20));
                await SendCommand(new DownloadFileDone() { Name = args.Name, FileType = args.FileType, IsSuccess = down?.IsComplete == true });
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error processing download result for file {0} : {1}", args.Name, ex);
            }
        }


        private async Task OnConnected()
        {
            Trace.TraceInformation("Chobby connected to wrapper");
            try
            {
                if (chobbyla.Steam.IsOnline)
                {
                    await
                        SendCommand(new SteamOnline()
                        {
                            AuthToken = chobbyla.AuthToken,
                            Friends = chobbyla.Friends.Select(x => x.ToString()).ToList(),
                            FriendSteamID =
                                chobbyla.InitialConnectLobbyID != 0 ? chobbyla.Steam.GetLobbyOwner(chobbyla.InitialConnectLobbyID)?.ToString() : null,
                            SuggestedName = chobbyla.MySteamNameSanitized
                        });
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error processing OnConnected: {0}" ,ex);
            }
        }

        private async Task OnConnectionClosed(bool arg)
        {
            Trace.TraceInformation("Chobby closed connection");
        }
    }
}