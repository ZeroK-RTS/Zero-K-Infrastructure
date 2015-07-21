using System;
using System.Collections.Generic;
using System.Threading;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    /// <summary>
    /// Relays chat from old spring server and back
    /// </summary>
    public class ChatRelay
    {
        ZkLobbyServer state;
        LobbyClient.Legacy.TasClient springTas;
        List<string> channels;

        public ChatRelay(ZkLobbyServer state, string password, List<string> channels)
        {
            this.state = state;
            this.springTas = new LobbyClient.Legacy.TasClient(null, "ChatRelay", 0);
            this.channels = channels;
            springTas.LoginAccepted += OnLoginAccepted;
            springTas.Said += OnSaid;
            state.Said += OnSaid;

            SetupSpringTasConnection(password);
        }

        void OnSaid(object sender, LobbyClient.Legacy.TasSayEventArgs args)
        {
            var tas = (LobbyClient.Legacy.TasClient)sender;
            if (args.Place == LobbyClient.Legacy.TasSayEventArgs.Places.Channel && channels.Contains(args.Channel) && args.UserName != tas.UserName) {
                state.GhostSay(new Say() {
                    Place = SayPlace.Channel,
                    Text = args.Text,
                    IsEmote = args.IsEmote,
                    Time = DateTime.UtcNow,
                    Target = args.Channel,
                    User = args.UserName,
                });
            }
        }

        void OnLoginAccepted(object sender, LobbyClient.Legacy.TasEventArgs e)
        {
            var tas = (LobbyClient.Legacy.TasClient)sender;
            foreach (var chan in channels) if (!tas.JoinedChannels.ContainsKey(chan)) tas.JoinChannel(chan);
        }

        void SetupSpringTasConnection(string password)
        {
            springTas.LoginDenied += (sender, args) => Utils.StartAsync(() => {
                Thread.Sleep(5000);
                springTas.Login(GlobalConst.NightwatchName, password);
            });
            springTas.Connect(GlobalConst.OldSpringLobbyHost, GlobalConst.OldSpringLobbyPort);
            springTas.Connected += (sender, args) => springTas.Login(GlobalConst.NightwatchName, password);
        }

        void OnSaid(object sender, Say say)
        {
            if (say.Place == SayPlace.Channel && channels.Contains(say.Target)) {
                springTas.Say(LobbyClient.Legacy.TasClient.SayPlace.Channel, say.Target, string.Format("<{0}> {1}", say.User, say.Text), say.IsEmote);
            }
        }

    }
}
