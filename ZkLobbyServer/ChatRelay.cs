using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using LobbyClient;
using ZkData;
using ZkLobbyServer;

namespace NightWatch
{
    public class ChatRelay
    {
        SharedServerState state;
        LobbyClient.Legacy.TasClient springTas;
        List<string> channels;

        public ChatRelay(SharedServerState state, string password, List<string> channels)
        {
            this.springTas = new LobbyClient.Legacy.TasClient(null, "ChatRelay", 0);
            this.channels = channels;
            springTas.LoginAccepted += OnLoginAccepted;
            springTas.Said += OnSaid;
            zkTas.Said += OnSaid;

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

        void OnSaid(object sender, TasSayEventArgs args)
        {
            var tas = (TasClient)sender;
            if (args.Place == SayPlace.Channel && channels.Contains(args.Channel) && args.UserName != tas.UserName) {
                var otherTas = springTas;
                otherTas.Say(LobbyClient.Legacy.TasClient.SayPlace.Channel, args.Channel, string.Format("<{0}> {1}", args.UserName, args.Text), args.IsEmote);
            }
        }

    }
}
