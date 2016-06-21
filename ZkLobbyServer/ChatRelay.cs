using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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

        public ChatRelay(ZkLobbyServer zkServer, string password, List<string> channels)
        {
            this.zkServer = zkServer;
            this.springTas = new TasClient(null, "ChatRelay", 0);
            this.channels = channels;
            springTas.LoginAccepted += OnSpringTasLoginAccepted;
            springTas.Said += OnSpringTasSaid;
            zkServer.Said += OnZkServerSaid;

            SetupSpringTasConnection(password);
        }

        void OnZkServerSaid(object sender, Say say)
        {
            try
            {
                if (say.AllowRelay && say.Place == SayPlace.Channel && channels.Contains(say.Target))
                {
                    if (say.Text.StartsWith("!names")) zkServer.GhostChanSay(say.Target, string.Join(", ", springTas.JoinedChannels[say.Target].ChannelUsers), true, false);
                    springTas.Say(TasClient.SayPlace.Channel, say.Target, string.Format("<{0}> {1}", say.User, say.Text), say.IsEmote);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error relaying message  {0} {1} {2} : {3}", say?.Target, say?.User, say?.Text, ex);
            }
        }

        void SetupSpringTasConnection(string password)
        {
            springTas.LoginDenied += (sender, args) => Utils.StartAsync(() => {
                Thread.Sleep(5000);
                springTas.Login(GlobalConst.NightwatchName, password);
            });
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
                        springTas.Say(TasClient.SayPlace.Channel,
                            args.Channel,
                            string.Join(", ", zkServer.Rooms[args.Channel].Users.Select(x => x.Key)),
                            true);
                    }

                    zkServer.GhostSay(new Say()
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