using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PlasmaShared;
using ZkData;

namespace LobbyClient
{
    /// <summary>
    /// Maintains custom attributes associated with users
    /// </summary>
    public class ProtocolExtension
    {
#if DEBUG
        public const string ExtensionChannelName = "extension_dev";
#else
        public const string ExtensionChannelName = "extension";
#endif

        public class JugglerState
        {
            public List<ModePair> ModeCounts = new List<ModePair>();
            public int TotalPlayers = 0;

            public class ModePair
            {
                public int Count;
                public AutohostMode Mode;
            }
        }

        public class JugglerConfig
        {
            public bool Active;
            public List<PreferencePair> Preferences = new List<PreferencePair>();

            public class PreferencePair
            {
                public AutohostMode Mode;
                public GamePreference Preference;
            }
        }


        public enum Keys
        {
            Level,
            EffectiveElo,
            Faction,
            Clan,
            Avatar,
            SpringieLevel,
            ZkAdmin,
            BanMute,
            BanLobby
        }

        readonly Action<string, Dictionary<string, string>> notifyUserExtensionChange;


        readonly Dictionary<string, Dictionary<string, string>> publishedUserAttributes = new Dictionary<string, Dictionary<string, string>>();

        readonly TasClient tas;


        readonly Dictionary<string, Dictionary<string, string>> userAttributes = new Dictionary<string, Dictionary<string, string>>();


        public ProtocolExtension(TasClient tas, Action<string, Dictionary<string, string>> notifyUserExtensionChange)
        {
            this.tas = tas;
            this.notifyUserExtensionChange = notifyUserExtensionChange;
            tas.PreviewChannelJoined += tas_PreviewChannelJoined;
            tas.PreviewSaid += tas_PreviewSaid;
            tas.ChannelUserAdded += tas_ChannelUserAdded;
            tas.LoginAccepted += (s, e) => tas.JoinChannel(ExtensionChannelName);
        }

        internal Dictionary<string, string> Get(string name)
        {
            Dictionary<string, string> dict;
            userAttributes.TryGetValue(name, out dict);
            return dict ?? new Dictionary<string, string>();
        }

        public void Publish(string name, Dictionary<string, string> data)
        {
            Dictionary<string, string> dict;
            publishedUserAttributes.TryGetValue(name, out dict);
            dict = dict ?? new Dictionary<string, string>();
            foreach (var kvp in data) dict[kvp.Key] = kvp.Value;
            publishedUserAttributes[name] = dict;
            tas.Say(TasClient.SayPlace.Channel, ExtensionChannelName, FormatMessage(name, data), false);
        }

        static Dictionary<string, string> Deserialize(string data)
        {
            var ret = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(data))
            {
                var parts = data.Split('|');
                for (var i = 0; i < parts.Length; i += 2) ret[Unescape(parts[i])] = Unescape(parts[i + 1]);
            }
            return ret;
        }

        static string Escape(string input)
        {
            return input.Replace("|", "&divider&");
        }

        string FormatMessage(string user, Dictionary<string, string> data)
        {
            return string.Format("USER_EXT {0} {1}", user, Serialize(data));
        }

        static string Serialize(Dictionary<string, string> data)
        {
            return string.Join("|", data.Select(x => string.Format("{0}|{1}", Escape(x.Key), Escape(x.Value))).ToArray());
        }

        static string Unescape(string input)
        {
            return input.Replace("&divider&", "|");
        }

        void tas_ChannelUserAdded(object sender, TasEventArgs e)
        {
            if (e.ServerParams[0] == ExtensionChannelName && e.ServerParams[1] != tas.UserName) foreach (var kvp in publishedUserAttributes) tas.Say(TasClient.SayPlace.User, e.ServerParams[1], FormatMessage(kvp.Key, kvp.Value), false);
        }

        void tas_PreviewChannelJoined(object sender, CancelEventArgs<TasEventArgs> e)
        {
            if (e.Data.ServerParams[0] == ExtensionChannelName)
            {
                e.Cancel = true;
                foreach (var kvp in publishedUserAttributes) tas.Say(TasClient.SayPlace.Channel, ExtensionChannelName, FormatMessage(kvp.Key, kvp.Value), false);
            }
        }

        private static string EncodeJson(object data) {

            var json = fastJSON.JSON.Instance;
            var payload = json.ToJSON(data);
            return string.Format("JSON {0} {1}", data.GetType().Name, payload);
        }

        public void PublishJugglerState(JugglerState state) {
            tas.Say(TasClient.SayPlace.Channel, ExtensionChannelName, EncodeJson(state),false);
        }

        public void SendMyJugglerConfig(JugglerConfig config) {
            tas.Say(TasClient.SayPlace.User, GlobalConst.NightwatchName, EncodeJson(config), false);
        }

        public void SendPlayerJugglerConfig(JugglerConfig config, string name) {
            tas.Say(TasClient.SayPlace.User, name, EncodeJson(config),false);
        }


        public Action<TasSayEventArgs, JugglerState> JugglerStateReceived = (args, state) => { };
        public Action<TasSayEventArgs, JugglerConfig> JugglerConfigReceived = (args, config) => { };



        private object DecodeJson(string data, TasSayEventArgs e) {
            try
            {
                var json = fastJSON.JSON.Instance;
                var parts = data.Split(new char[] { ' ' }, 3);
                if (parts[0] != "JSON") return null;
                var payload = parts[2];
                switch (parts[1])
                {
                    case "JugglerState":
                        {
                            var state = json.ToObject<JugglerState>(payload);
                            JugglerStateReceived(e, state);

                        }
                        break;
                    case "JugglerConfig":
                        {
                            var config = json.ToObject<JugglerConfig>(payload);
                            JugglerConfigReceived(e, config);
                        }

                        break;
                }
            }
            catch (Exception ex) {
                Trace.TraceError(ex.ToString());
            }
            return null;
        }


        private 


        void tas_PreviewSaid(object sender, CancelEventArgs<TasSayEventArgs> e)
        {
            if (e.Data.UserName == GlobalConst.NightwatchName &&
                ((e.Data.Place == TasSayEventArgs.Places.Channel && e.Data.Channel == ExtensionChannelName) ||
                 (e.Data.Place == TasSayEventArgs.Places.Normal)))
            {
                var parts = e.Data.Text.Split(new char[] { ' ' }, 3);
                if (parts.Length == 3 && parts[0] == "USER_EXT")
                {
                    e.Cancel = true;

                    var name = parts[1];
                    var data = Deserialize(parts[2]);

                    Dictionary<string, string> dict;
                    userAttributes.TryGetValue(name, out dict);
                    dict = dict ?? new Dictionary<string, string>();

                    foreach (var kvp in data) dict[kvp.Key] = kvp.Value;
                    userAttributes[name] = dict;
                    notifyUserExtensionChange(name, dict);
                }
                else if (parts.Length >=3 && parts[0] == "JSON") {
                    DecodeJson(e.Data.Text, e.Data);
                }
            }
        }
    }
}