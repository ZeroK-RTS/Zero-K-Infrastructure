using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LobbyClient;
using PlasmaShared;
using ZkData;
using TasClient = LobbyClient.Legacy.TasClient;
using TasEventArgs = LobbyClient.Legacy.TasEventArgs;
using TasSayEventArgs = LobbyClient.Legacy.TasSayEventArgs;

namespace ZkLobbyServer
{
    public class SpringRelaySource: IChatRelaySource
    {
        public TasClient SpringTas { get; private set; }

        private List<string> channels;
        public SpringRelaySource(List<string> channels)
        {
            this.channels = channels;
            this.SpringTas = new TasClient(null, "ChatRelay", 0);
            SpringTas.LoginAccepted += OnSpringTasLoginAccepted;
            SpringTas.Said += OnSpringTasSaid;

            SetupSpringTasConnection(new Secrets().GetNightwatchPassword());
        }

        void SetupSpringTasConnection(string password)
        {
            SpringTas.Connected += (sender, args) => SpringTas.Login(GlobalConst.NightwatchName, password);
            SpringTas.Connect(GlobalConst.OldSpringLobbyHost, GlobalConst.OldSpringLobbyPort);
        }

        void OnSpringTasLoginAccepted(object sender, TasEventArgs e)
        {
            foreach (var chan in channels) if (!SpringTas.JoinedChannels.ContainsKey(chan)) SpringTas.JoinChannel(chan);
        }

        void OnSpringTasSaid(object sender, TasSayEventArgs args)
        {
            try
            {
                if (args.Place == TasSayEventArgs.Places.Channel && args.UserName != SpringTas.UserName)
                {
                    OnChatRelayMessage?.Invoke(this, new ChatRelayMessage(args.Channel, args.UserName, args.Text, SaySource.Spring, args.IsEmote));
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error reading tas message  {0} : {1}", args, ex);
            }
        }

        public List<string> GetUsers(string channel)
        {
            return SpringTas.JoinedChannels.Get(channel)?.ChannelUsers?.ToList() ?? new List<string>();
        }

        public event Action<IChatRelaySource, ChatRelayMessage> OnChatRelayMessage;
        public void SendMessage(ChatRelayMessage msg)
        {
            try
            {
                if (msg.Source != SaySource.Spring)
                {
                    if (msg.User != GlobalConst.NightwatchName) SpringTas.Say(TasClient.SayPlace.Channel, msg.Channel, string.Format("<{0}> {1}", msg.User, msg.Message), msg.IsEmote);
                    else SpringTas.Say(TasClient.SayPlace.Channel, msg.Channel, msg.Message, msg.IsEmote);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error sending chat message to spring: {0}" ,ex);
            }
        }

        public void SendPm(string user, string message)
        {
            try
            {
                SpringTas.Say(TasClient.SayPlace.User, user, message, true);
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error sending spring PM: {0}" ,ex);
            }
        }
    }
}