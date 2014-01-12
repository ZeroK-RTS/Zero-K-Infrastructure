using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using PlasmaShared;
using ServiceStack.Text;
using ZkData;

namespace LobbyClient
{
    /// <summary>
    /// Maintains custom attributes associated with users
    /// </summary>
    public class ProtocolExtension
    {
        public const string ExtensionChannelName = "extension";


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

        readonly Action<string, Dictionary<string, string>> notifyUserExtensionChange = (s, dictionary) => { };


        readonly Dictionary<string, JugglerConfig> publishedJugglerConfigs = new Dictionary<string, JugglerConfig>();
        readonly Dictionary<string, Dictionary<string, string>> publishedUserAttributes = new Dictionary<string, Dictionary<string, string>>();

        readonly TasClient tas;
        readonly Dictionary<string, Type> typeCache = new Dictionary<string, Type>()
        {
            { typeof(JugglerConfig).Name, typeof(JugglerConfig) },
            { typeof(JugglerState).Name, typeof(JugglerState) },
            { typeof(SiteToLobbyCommand).Name, typeof(SiteToLobbyCommand) },
        };


        readonly Dictionary<string, Dictionary<string, string>> userAttributes = new Dictionary<string, Dictionary<string, string>>();
        public Action<TasSayEventArgs, object> JsonDataReceived = (args, state) => { };
        public Action<TasSayEventArgs, JugglerConfig> JugglerConfigReceived = (args, config) => { };
        public Action<TasSayEventArgs, JugglerState> JugglerStateReceived = (args, state) => { };


        public ProtocolExtension(TasClient tas, Action<string, Dictionary<string, string>> notifyUserExtensionChange) {
            this.tas = tas;
            this.notifyUserExtensionChange = notifyUserExtensionChange;
            tas.PreviewChannelJoined += tas_PreviewChannelJoined;
            tas.PreviewSaid += tas_PreviewSaid;
            tas.ChannelUserAdded += tas_ChannelUserAdded;
            tas.LoginAccepted += (s, e) => tas.JoinChannel(ExtensionChannelName);
        }

        internal Dictionary<string, string> Get(string name) {
            Dictionary<string, string> dict;
            userAttributes.TryGetValue(name, out dict);
            return dict ?? new Dictionary<string, string>();
        }

        public JugglerConfig GetPublishedConfig(string name) {
            JugglerConfig conf;
            publishedJugglerConfigs.TryGetValue(name, out conf);
            return conf;
        }


        public void Publish(string name, Dictionary<string, string> data) {
            var dict = new Dictionary<string, string>(data);
            publishedUserAttributes[name] = dict;
            tas.Say(TasClient.SayPlace.Channel, ExtensionChannelName, FormatMessage(name, data), false);
        }

        public void PublishAccountData(Account acc) {
            if (acc != null && tas.ExistingUsers.ContainsKey(acc.Name)) {
                var data = new Dictionary<string, string>
                {
                    { Keys.Level.ToString(), acc.Level.ToString() },
                    { Keys.EffectiveElo.ToString(), ((int)acc.EffectiveElo).ToString() },
                    { Keys.Faction.ToString(), acc.Faction != null ? acc.Faction.Shortcut : "" },
                    { Keys.Clan.ToString(), acc.Clan != null ? acc.Clan.Shortcut : "" },
                    { Keys.Avatar.ToString(), acc.Avatar },
                    { Keys.SpringieLevel.ToString(), acc.SpringieLevel.ToString() }
                };
                if (acc.IsZeroKAdmin) data.Add(Keys.ZkAdmin.ToString(), "1");

                if (acc.PunishmentsByAccountID.Any(x => x.BanExpires > DateTime.UtcNow && x.BanMute)) data.Add(Keys.BanMute.ToString(), "1");
                if (acc.PunishmentsByAccountID.Any(x => x.BanExpires > DateTime.UtcNow && x.BanLobby)) data.Add(Keys.BanLobby.ToString(), "1");

                tas.Extensions.Publish(acc.Name, data);

                // if (acc.PunishmentsByAccountID.Any(x => x.BanExpires > DateTime.UtcNow && x.BanLobby)) tas.AdminKickFromLobby(acc.Name, "Banned");
                var penalty = acc.PunishmentsByAccountID.Any(x => x.BanExpires > DateTime.UtcNow && x.BanLobby);
                if (penalty != null) tas.AdminKickFromLobby(acc.Name, string.Format("Banned until {0}, reason: {1}", penalty.BanExpires, penalty.Reason));
				}
        }

        public void PublishJugglerState(JugglerState state) {
            tas.Say(TasClient.SayPlace.Channel, ExtensionChannelName, EncodeJson(state), false);
        }

        public void PublishPlayerJugglerConfig(JugglerConfig config, string name) {
            publishedJugglerConfigs[name] = config;
            tas.Say(TasClient.SayPlace.User, name, EncodeJson(config), false);
        }

        public void SendJsonData(string username, object data) {
            tas.Say(TasClient.SayPlace.User, username, EncodeJson(data), false);
        }

        public void SendMyJugglerConfig(JugglerConfig config) {
            tas.Say(TasClient.SayPlace.User, GlobalConst.NightwatchName, EncodeJson(config), false);
        }


        object DecodeJson(string data, TasSayEventArgs e) {
            try {
                var parts = data.Split(new[] { ' ' }, 3);
                if (parts[0] != "!JSON") return null;
                var payload = parts[2];
                var tname = parts[1];

                Type type;
                if (!typeCache.TryGetValue(tname, out type)) {
                    type = Type.GetType(tname) ??
                           Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(x => x.Name == tname) ??
                           AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).FirstOrDefault(x => x.Name == tname);
                    typeCache[tname] = type;
                }

                if (type != null) {
                    var decoded = JsonSerializer.DeserializeFromString(payload, type);
                    var state = decoded as JugglerState;
                    if (state != null) JugglerStateReceived(e, state);
                    var config = decoded as JugglerConfig;
                    if (config != null) JugglerConfigReceived(e, config);
                    JsonDataReceived(e, decoded);
                }
            } catch (Exception ex) {
                Trace.TraceError(ex.ToString());
            }
            return null;
        }

        static Dictionary<string, string> Deserialize(string data) {
            var ret = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(data)) {
                var parts = data.Split('|');
                for (var i = 0; i < parts.Length; i += 2) ret[Unescape(parts[i])] = Unescape(parts[i + 1]);
            }
            return ret;
        }

        static string EncodeJson(object data) {
            var payload = JsonSerializer.SerializeToString(data);
            return string.Format("!JSON {0} {1}", data.GetType().Name, payload);
        }

        static string Escape(string input) {
            return input.Replace("|", "&divider&");
        }

        string FormatMessage(string user, Dictionary<string, string> data) {
            return string.Format("USER_EXT {0} {1}", user, Serialize(data));
        }

        static string Serialize(Dictionary<string, string> data) {
            return string.Join("|", data.Select(x => string.Format("{0}|{1}", Escape(x.Key), Escape(x.Value))).ToArray());
        }

        static string Unescape(string input) {
            return input.Replace("&divider&", "|");
        }

        void tas_ChannelUserAdded(object sender, TasEventArgs e) {
            if (e.ServerParams[0] == ExtensionChannelName && e.ServerParams[1] != tas.UserName) {
                foreach (var kvp in publishedUserAttributes) tas.Say(TasClient.SayPlace.User, e.ServerParams[1], FormatMessage(kvp.Key, kvp.Value), false);

                JugglerConfig config;
                if (publishedJugglerConfigs.TryGetValue(e.ServerParams[1], out config)) tas.Say(TasClient.SayPlace.User, e.ServerParams[1], EncodeJson(config), false);
            }
        }

        void tas_PreviewChannelJoined(object sender, CancelEventArgs<TasEventArgs> e) {
            if (e.Data.ServerParams[0] == ExtensionChannelName) {
                e.Cancel = true;
                foreach (var kvp in publishedUserAttributes) tas.Say(TasClient.SayPlace.Channel, ExtensionChannelName, FormatMessage(kvp.Key, kvp.Value), false);
                foreach (var kvp in publishedJugglerConfigs) tas.Say(TasClient.SayPlace.User, kvp.Key, EncodeJson(kvp.Value), false);
            }
        }


        void tas_PreviewSaid(object sender, CancelEventArgs<TasSayEventArgs> e) {
            if (e.Data.Channel == GlobalConst.NightwatchName || e.Data.UserName == GlobalConst.NightwatchName ||
                e.Data.Channel == ExtensionChannelName || tas.UserName == GlobalConst.NightwatchName) {
                var parts = e.Data.Text.Split(new[] { ' ' }, 3);
                if (parts.Length == 3 && parts[0] == "USER_EXT") {
                    e.Cancel = true;

                    var name = parts[1];
                    var data = Deserialize(parts[2]);

                    var dict = new Dictionary<string, string>();
                    //userAttributes.TryGetValue(name, out dict);
                    //dict = dict ?? new Dictionary<string, string>();

                    foreach (var kvp in data) dict[kvp.Key] = kvp.Value;
                    userAttributes[name] = dict;
                    notifyUserExtensionChange(name, dict);
                }
                else if (parts.Length >= 3 && parts[0] == "!JSON") {
                    e.Cancel = true;
                    DecodeJson(e.Data.Text, e.Data);
                }
            }
        }

        public class JugglerConfig
        {
            public bool Active { get; set; }
            public List<PreferencePair> Preferences { get; set; }

            public JugglerConfig(Account acc): this() {
                Active = acc.MatchMakingActive;
                foreach (var item in acc.Preferences) Preferences.Add(new PreferencePair { Mode = item.Key, Preference = item.Value });
            }

            public JugglerConfig() {
                Preferences = new List<PreferencePair>();
            }

            public class PreferencePair
            {
                public AutohostMode Mode { get; set; }
                public GamePreference Preference { get; set; }
            }
        }

        public class JugglerState
        {
            public List<ModePair> ModeCounts { get; set; }
            public int TotalPlayers { get; set; }

            public JugglerState() {
                ModeCounts = new List<ModePair>();
            }

            public class ModePair
            {
                public int Count { get; set; }
                public AutohostMode Mode { get; set; }
                public int Playing { get; set; }
            }
        }

        public class SiteToLobbyCommand
        {
            public string SpringLink { get; set; }
        }
    }
}