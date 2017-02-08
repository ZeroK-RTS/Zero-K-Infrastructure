using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Discord;
using LobbyClient;
using ZkData;
using TasClient = LobbyClient.Legacy.TasClient;
using TasEventArgs = LobbyClient.Legacy.TasEventArgs;
using TasSayEventArgs = LobbyClient.Legacy.TasSayEventArgs;

namespace ZkLobbyServer
{
    /// <summary>
    /// Relays chat from old spring server and back
    /// </summary>
    public class ChatRelay
    {
        readonly List<string> channels;
        readonly TasClient springTas;
        readonly ZkLobbyServer zkServer;
        private readonly DiscordClient discord;

        const ulong DiscordZkChannelID = 278805140708786177;

        public ChatRelay(ZkLobbyServer zkServer, string password, List<string> channels)
        {
            this.zkServer = zkServer;
            this.springTas = new TasClient(null, "ChatRelay", 0);
            this.channels = channels;
            springTas.LoginAccepted += OnSpringTasLoginAccepted;
            springTas.Said += OnSpringTasSaid;
            zkServer.Said += OnZkServerSaid;

            discord = new DiscordClient();
            discord.MessageReceived += DiscordOnMessageReceived;

            discord.Connect(new Secrets().GetNightwatchDiscordToken(), TokenType.Bot);
            

            SetupSpringTasConnection(password);
        }

        private void DiscordOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            if (messageEventArgs.Channel.Id == DiscordZkChannelID && !messageEventArgs.User.IsBot) zkServer.GhostSay(
                new Say()
                {
                    AllowRelay = false,
                    User = messageEventArgs.User.ToString(),
                    IsEmote = false,
                    Place = SayPlace.Channel,
                    Target = "zk",
                    Text = messageEventArgs.Message.Text"
                });
        }

        void OnZkServerSaid(object sender, Say say)
        {
            try
            {
                if (say.AllowRelay && say.Place == SayPlace.Channel && channels.Contains(say.Target))
                {
                    if (say.Text.StartsWith("!names")) zkServer.GhostPm(say.User, string.Join(", ", springTas.JoinedChannels[say.Target].ChannelUsers));
                    else springTas.Say(TasClient.SayPlace.Channel, say.Target, string.Format("<{0}> {1}", say.User, say.Text), say.IsEmote);
                }
                if (say.AllowRelay && say.Place == SayPlace.Channel && say.Target == "zk" && say.User != GlobalConst.NightwatchName)
                {
                    discord.GetChannel(DiscordZkChannelID).SendMessage($"<{say.User}> {say.Text}");
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error relaying message  {0} {1} {2} : {3}", say?.Target, say?.User, say?.Text, ex);
            }
        }

        void SetupSpringTasConnection(string password)
        {
            springTas.Connected += (sender, args) => springTas.Login(GlobalConst.NightwatchName, password);
            springTas.Connect(GlobalConst.OldSpringLobbyHost, GlobalConst.OldSpringLobbyPort);
        }

        void OnSpringTasLoginAccepted(object sender, TasEventArgs e)
        {
            foreach (var chan in channels) if (!springTas.JoinedChannels.ContainsKey(chan)) springTas.JoinChannel(chan);
        }

        void OnSpringTasSaid(object sender, TasSayEventArgs args)
        {
            try
            {
                if (args.Place == TasSayEventArgs.Places.Channel && channels.Contains(args.Channel) && args.UserName != springTas.UserName)
                {
                    if (args.Text.StartsWith("!names"))
                    {
                        springTas.Say(TasClient.SayPlace.User,
                            args.UserName,
                            string.Join(", ", zkServer.Channels[args.Channel].Users.Select(x => x.Key)),
                            true);
                    }

                    else zkServer.GhostSay(new Say()
                    {
                        Place = SayPlace.Channel,
                        Text = args.Text,
                        IsEmote = args.IsEmote,
                        Time = DateTime.UtcNow,
                        Target = args.Channel,
                        User = args.UserName,
                        AllowRelay = false
                    });
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error relaying message  {0} : {1}", args,ex);
            }
        }
    }
}
