using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using LobbyClient;
using ZkData;

namespace NightWatch
{
    public class ChatRelay
    {
        TasClient zkTas;
        LobbyClient.Legacy.TasClient springTas;
        List<string> channels;

        public ChatRelay(TasClient zkTas, string password, List<string> channels)
        {
            this.springTas = new LobbyClient.Legacy.TasClient(null, "ChatRelay", 0);
            this.channels = channels;
            this.zkTas = zkTas;
            springTas.LoginAccepted += OnLoginAccepted;
            zkTas.LoginAccepted += OnLoginAccepted;
            springTas.Said += OnSaid;
            zkTas.Said += OnSaid;

            SetupSpringTasConnection(password);
        }

        void OnSaid(object sender, LobbyClient.Legacy.TasSayEventArgs args)
        {
            var tas = (LobbyClient.Legacy.TasClient)sender;
            if (args.Place == LobbyClient.Legacy.TasSayEventArgs.Places.Channel && channels.Contains(args.Channel) && args.UserName != tas.UserName)
            {
                var otherTas = zkTas;
                otherTas.Say(SayPlace.Channel, args.Channel, string.Format("<{0}> {1}", args.UserName, args.Text), args.IsEmote);
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

        void OnLoginAccepted(object sender, TasEventArgs tasEventArgs)
        {
            var tas = (TasClient)sender;
            foreach (var chan in channels) if (!tas.JoinedChannels.ContainsKey(chan)) tas.JoinChannel(chan);
        }
    }
}
