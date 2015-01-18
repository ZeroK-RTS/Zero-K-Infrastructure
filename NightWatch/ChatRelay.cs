using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using LobbyClient;
using ZkData;

namespace NightWatch
{
    public class ChatRelay
    {
        TasClient zkTas;
        TasClient springTas;
        Timer reconnect;
        List<string> channels;

        public ChatRelay(TasClient zkTas, string password, List<string> channels)
        {
            this.springTas = new TasClient(null, "ChatRelay", GlobalConst.ZkLobbyUserCpu);
            this.channels = channels;
            this.zkTas = zkTas;
            springTas.LoginAccepted += OnLoginAccepted;
            zkTas.LoginAccepted += OnLoginAccepted;
            springTas.Said += OnSaid;
            zkTas.Said += OnSaid;

            SetupSpringTasConnection(password);
        }

        void SetupSpringTasConnection(string password)
        {
            
            springTas.Connect("lobby.springrts.com", 8200);
            springTas.Connected += (sender, args) => springTas.Login(GlobalConst.NightwatchName, password);
            reconnect = new Timer(10000);
            reconnect.AutoReset = true;
            reconnect.Elapsed += (sender, args) => { if (!springTas.IsConnected) springTas.Connect("lobby.springrts.com", 8200); };
            reconnect.Start();
        }

        void OnSaid(object sender, TasSayEventArgs args)
        {
            var tas = (TasClient)sender;
            if (args.Place == TasSayEventArgs.Places.Channel && channels.Contains(args.Channel) && args.UserName != tas.UserName)
            {
                var otherTas = tas == zkTas ? springTas : zkTas;
                otherTas.Say(TasClient.SayPlace.Channel, args.Channel, string.Format("<{0}> {1}", args.UserName, args.Text), args.IsEmote);
            }
        }

        void OnLoginAccepted(object sender, TasEventArgs tasEventArgs)
        {
            var tas = (TasClient)sender;
            foreach (var chan in channels) if (!tas.JoinedChannels.ContainsKey(chan)) tas.JoinChannel(chan);
        }
    }
}
