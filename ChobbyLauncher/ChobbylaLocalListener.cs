using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
using Timer = System.Threading.Timer;

namespace ChobbyLauncher
{

    public class ChobbylaLocalListener
    {
        private CommandJsonSerializer serializer;
        private TcpTransport transport;
        private Chobbyla chobbyla;
        private TextToSpeechBase tts;
        private SteamClientHelper steam;
        private ulong initialConnectLobbyID;
        private Timer timer;
        private DiscordController discordController;


        public ChobbylaLocalListener(Chobbyla chobbyla, SteamClientHelper steam, ulong initialConnectLobbyID)
        {
            this.chobbyla = chobbyla;
            this.steam = steam;
            steam.Listener = this;
            this.initialConnectLobbyID = initialConnectLobbyID;
            serializer = new CommandJsonSerializer(Utils.GetAllTypesWithAttribute<ChobbyMessageAttribute>());
            tts = TextToSpeechBase.Create();
            steam.JoinFriendRequest += SteamOnJoinFriendRequest;
            steam.OverlayActivated += SteamOnOverlayActivated;
            steam.SteamOnline += () => { SendSteamOnline(); };
            steam.SteamOffline += () => { SendSteamOffline(); };
            discordController = new DiscordController(GlobalConst.ZeroKDiscordID, GlobalConst.SteamAppID.ToString());
            discordController.OnJoin += DiscordOnJoinCallback;
            discordController.OnDisconnected += DiscordOnDisconnectedCallback;
            discordController.OnError += DiscordOnErrorCallback;
            discordController.OnReady += DiscordOnReadyCallback;
            discordController.OnRequest += DiscordOnRequestCallback;
            discordController.OnSpectate += DiscordOnSpectateCallback;

            timer = new Timer((o)=>OnTimerTick(), this, 500, 500);
        }


        private void OnTimerTick()
        {
            foreach (var d in chobbyla.downloader.Downloads.Where(x=>x.IsComplete == null))
            {
                SendCommand(new DownloadFileProgress()
                {
                    Name = d.Name,
                    FileType = d.DownloadType.ToString(),
                    Progress = d.TotalProgress.ToString("F2", CultureInfo.InvariantCulture),
                    SecondsRemaining = d.SecondsRemaining,
                    TotalLength = d.TotalLength,
                    CurrentSpeed = d.CurrentSpeed
                });
            }

            
            discordController.Update();
        }

        private void SteamOnOverlayActivated(bool b)
        {
            SendCommand(new SteamOverlayChanged() { IsActive = b });
        }

        private void SteamOnJoinFriendRequest(ulong friendSteamID)
        {
            SendCommand(new SteamJoinFriend() { FriendSteamID = friendSteamID.ToString() });
            steam.SendSteamNotifyJoin(friendSteamID);
        }


        private void DiscordOnErrorCallback(int errorCode, string message)
        {
            SendCommand(new DiscordOnError { ErrorCode = errorCode, Message = message });
        }

        private void DiscordOnJoinCallback(string secret)
        {
            SendCommand(new DiscordOnJoin() { Secret = secret });
        }

        private void DiscordOnSpectateCallback(string secret)
        {
            SendCommand(new DiscordOnSpectate { Secret = secret });
        }

        private void DiscordOnRequestCallback(ref DiscordOnJoinRequest request)
        {
            SendCommand(request);
        }

        private void DiscordOnReadyCallback()
        {
            SendCommand(new DiscordOnReady());
        }

        private void DiscordOnDisconnectedCallback(int errorCode, string message)
        {
            SendCommand(new DiscordOnDisconnected { ErrorCode = errorCode, Message = message });
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
                MinimizeChobby();
                System.Diagnostics.Process.Start(args.Url);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error opening URL {0} : {1}", args.Url, ex);
            }
        }

        private void MinimizeChobby()
        {
            try
            {
                if (chobbyla.process.MainWindowHandle != IntPtr.Zero)
                {
                    WindowsApi.ShowWindow(chobbyla.process.MainWindowHandle, WindowsApi.SwCommand.SW_MINIMIZE);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error minimizing chobby zero-k window: {0}", ex);
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
                MinimizeChobby();
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
                Trace.TraceInformation("Chobbyla >> {0}", line);
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
                Trace.TraceInformation("Chobbyla << {0}", line);
                await Process(obj);
            }
            catch (Exception ex)
            {
                Trace.TraceError("{0} error processing line {1} : {2}", this, line, ex);
            }
        }


        public async Task Process(AbortDownload args)
        {
            try
            {
                DownloadType type;
                if (string.IsNullOrEmpty(args.FileType) || !Enum.TryParse(args.FileType, out type)) type = DownloadType.NOTKNOWN;
                chobbyla.downloader.Downloads
                    .Where(x => (x.TypeOfResource == type && (x.Alias == args.Name || x.Name == args.Name)))
                    .ForEach(x => x.Abort());
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error cancelling download {0} : {1}", args?.Name, ex);
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

        public async Task Process(DownloadImage args)
        {
            try
            {
                ImageType type;
                if (string.IsNullOrEmpty(args.ImageType) || !Enum.TryParse(args.ImageType, out type)) type = ImageType.Clans;
                using (var wc = new WebClient())
                {

                    wc.DownloadFile($"{GlobalConst.BaseSiteUrl}/img/{type}/{args.Name}.png",
                        Path.Combine(chobbyla.paths.WritableDirectory, "LuaUI", "Configs", type.ToString(), $"{args.Name}.png"));
                    SendCommand(new DownloadImageDone() { Name = args.Name, ImageType = args.ImageType, RequestToken = args.RequestToken });
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error downloading image {0} : {1}", args?.Name, ex);
            }
        }


        public async Task Process(SteamOpenOverlaySection args)
        {
            try
            {
                steam.OpenOverlaySection(args.Option ?? SteamClientHelper.OverlayOption.LobbyInvite);
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
                if (steam.IsOnline) steam.OpenOverlayWebsite(args.Url);
                else
                {
                    MinimizeChobby();
                    System.Diagnostics.Process.Start(args.Url);
                }
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
                if (steam.LobbyID != null) steam.InviteFriendToGame(steam.LobbyID.Value, ulong.Parse(args.SteamID));
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

        public async Task Process(GaAddBusinessEvent args)
        {
            try
            {
                GameAnalytics.AddBusinessEvent(args.Currency, args.Amount, args.ItemType, args.ItemId, args.CartType);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error adding GA business event: {0}",  ex);
            }
        }

        public async Task Process(GaAddResourceEvent args)
        {
            try
            {
                GameAnalytics.AddResourceEvent(args.FlowType, args.Currency, args.Amount, args.ItemType, args.ItemId);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error adding GA resource event: {0}", ex);
            }
        }


        public async Task Process(GaConfigureResourceCurrencies args)
        {
            try
            {
                GameAnalytics.ConfigureAvailableResourceCurrencies(args.List);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error configuring GA resource currencies: {0}", ex);
            }
        }

        public async Task Process(GaConfigureResourceItemTypes args)
        {
            try
            {
                GameAnalytics.ConfigureAvailableResourceItemTypes(args.List);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error configuring GA resource currencies: {0}", ex);
            }
        }


        public async Task Process(GaConfigureCustomDimensions args)
        {
            try
            {
                switch (args.Level)
                {
                    case 3:
                        GameAnalytics.ConfigureAvailableCustomDimensions03(args.List);
                        break;
                    case 2:
                        GameAnalytics.ConfigureAvailableCustomDimensions02(args.List);
                        break;
                    case 1:
                    default:
                        GameAnalytics.ConfigureAvailableCustomDimensions03(args.List);
                        break;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error configuring GA custom dimensions: {0}", ex);
            }
        }

        public async Task Process(GaSetCustomDimension args)
        {
            try
            {
                switch (args.Level)
                {
                    case 3:
                        GameAnalytics.SetCustomDimension03(args.Value);
                        break;
                    case 2:
                        GameAnalytics.SetCustomDimension02(args.Value);
                        break;
                    case 1:
                    default:
                        GameAnalytics.SetCustomDimension01(args.Value);
                        break;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error setting GA custom dimensions: {0}", ex);
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
                if (down != null)
                {
                    await down.WaitHandle.AsTask(TimeSpan.FromMinutes(30));
                }
                await SendCommand(new DownloadFileDone() { Name = args.Name, FileType = args.FileType, IsSuccess = down?.IsComplete == true, IsAborted = down?.IsAborted == true});
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error processing download result for file {0} : {1}", args.Name, ex);
            }
        }

        public async Task Process(SteamHostGameRequest args)
        {
            try
            {
                steam.PrepareToHostP2PGame(args);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error processing steamhostgamerequest: {0}",ex);
            }
        }

        private async Task Process(StartNewSpring args)
        {
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    if (args.Downloads?.Any() == true)
                    {
                        foreach (var x in args.Downloads)
                        {
                            DownloadType type;
                            if (string.IsNullOrEmpty(x.FileType) || !Enum.TryParse(x.FileType, out type)) type = DownloadType.NOTKNOWN;
                            var result = await chobbyla.downloader.DownloadFile(type, x.Name, null);
                            if (!result) Trace.TraceWarning("Download of {0} {1} has failed", x.FileType, x.Name);
                        }
                    }
                    if (!await chobbyla.downloader.DownloadFile(DownloadType.ENGINE, args.Engine, null)) Trace.TraceWarning("Download of engine {0} has failed", args.Engine);


                    var process = new Process { StartInfo = { CreateNoWindow = false, UseShellExecute = false } };
                    var paths = chobbyla.paths;
                    paths.SetDefaultEnvVars(process.StartInfo, args.Engine);

                    process.StartInfo.FileName = paths.GetSpringExecutablePath(args.Engine);
                    process.StartInfo.WorkingDirectory = Path.GetDirectoryName(paths.GetSpringExecutablePath(args.Engine));

                    string startFilePath = null;
                    if (!string.IsNullOrEmpty(args.StartDemoName))
                    {
                        if (!args.StartDemoName.EndsWith(".sdfz") && !args.StartDemoName.EndsWith(".sdf")) args.StartDemoName = args.StartDemoName + ".sdfz";
                        startFilePath = Path.Combine(paths.WritableDirectory, "demos", args.StartDemoName);
                        if (!File.Exists(startFilePath))
                        {
                            Trace.TraceWarning("Demo file {0} not found, aborting", startFilePath);
                            return;
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(args.StartScriptContent))
                    {
                        startFilePath = Path.Combine(paths.WritableDirectory, "_script.txt");
                        File.WriteAllText(startFilePath, args.StartScriptContent);
                    }

                    var configFilePath = Path.Combine(paths.WritableDirectory, "springsettings.cfg");
                    if (!string.IsNullOrEmpty(args.SpringSettings))
                    {
                        configFilePath = Path.Combine(paths.WritableDirectory, "_springsettings.cfg");
                        File.WriteAllText(configFilePath, args.SpringSettings);
                    }

                    process.StartInfo.Arguments = $"\"{startFilePath}\" --config \"{configFilePath}\"";
                    process.Start();
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error processing StartNewSpring: {0}", ex);
                }
            });
        }


        private async Task Process(DiscordUpdatePresence args)
        {
            discordController.UpdatePresence(args);
        }

        private async Task Process(DiscordRespond args)
        {
            discordController.Respond(args.UserId, (DiscordRpc.Reply)args.Reply);
        }
        

        private async Task OnConnected()
        {
            Trace.TraceInformation("Chobby connected to wrapper");
            try
            {
                await SendSteamOnline();
               
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error processing OnConnected: {0}" ,ex);
            }

            try
            {
                await
                    SendCommand(new WrapperOnline()
                    {
                        DefaultServerHost = GlobalConst.LobbyServerHost,
                        DefaultServerPort = GlobalConst.LobbyServerPort,
                        UserID = Utils.GetMyUserID().ToString(),
                        IsSteamFolder = chobbyla.IsSteamFolder

                    });
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error sending WrapperOnline", ex);
            }

            discordController.Init();
        }


        private async Task SendSteamOnline()
        {
            if (steam.IsOnline)
            {
                var friendId = initialConnectLobbyID != 0 ? steam.GetLobbyOwner(initialConnectLobbyID) : null;
                    
                await
                    SendCommand(new SteamOnline()
                    {
                        AuthToken = steam.AuthToken,
                        Friends = steam.Friends.Select(x => x.ToString()).ToList(),
                        FriendSteamID = friendId?.ToString(),
                        SuggestedName = steam.MySteamNameSanitized,
                    });

                if (friendId != null) steam.SendSteamNotifyJoin(friendId.Value);
            }
        }

        private async Task SendSteamOffline()
        {
            await SendCommand(new SteamOffline());
        }


        private async Task OnConnectionClosed(bool arg)
        {
            timer.Dispose();
            discordController.Dispose();            
            Trace.TraceInformation("Chobby closed connection");
        }
    }

}
