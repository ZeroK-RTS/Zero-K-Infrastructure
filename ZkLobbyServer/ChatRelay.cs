using System;
using System.Collections.Generic;
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
            if (!say.IsGhostSay && say.Place == SayPlace.Channel && channels.Contains(say.Target)) springTas.Say(TasClient.SayPlace.Channel, say.Target, string.Format("<{0}> {1}", say.User, say.Text), say.IsEmote);
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
            if (args.Place == TasSayEventArgs.Places.Channel && channels.Contains(args.Channel) && args.UserName != springTas.UserName) {
                zkServer.GhostSay(new Say() {
                    Place = SayPlace.Channel,
                    Text = args.Text,
                    IsEmote = args.IsEmote,
                    Time = DateTime.UtcNow,
                    Target = args.Channel,
                    User = args.UserName,
                });
            }
        }
    }
}