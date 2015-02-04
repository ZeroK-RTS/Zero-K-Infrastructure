using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using PlasmaShared;
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
            SteamID,
            DisplayName,
        }


        readonly Action<string, Dictionary<string, string>> notifyUserExtensionChange = (s, dictionary) => { };


        readonly Dictionary<string, Dictionary<string, string>> publishedUserAttributes = new Dictionary<string, Dictionary<string, string>>();

        public TasClient tas { get; private set; }
        readonly Dictionary<string, Type> typeCache = new Dictionary<string, Type>()
        {
            { typeof(SiteToLobbyCommand).Name, typeof(SiteToLobbyCommand) },
            { typeof(PwMatchCommand).Name, typeof(PwMatchCommand) },
        };


        readonly Dictionary<string, Dictionary<string, string>> userAttributes = new Dictionary<string, Dictionary<string, string>>();
        public Action<TasSayEventArgs, object> JsonDataReceived = (args, state) => { };


        public ProtocolExtension(TasClient tas, Action<string, Dictionary<string, string>> notifyUserExtensionChange) {
            this.tas = tas;
            this.notifyUserExtensionChange = notifyUserExtensionChange;
            tas.PreviewSaid += tas_PreviewSaid;
            //tas.LoginAccepted += (s, e) => tas.JoinChannel(ExtensionChannelName);
        }

        internal Dictionary<string, string> Get(string name) {
            Dictionary<string, string> dict;
            userAttributes.TryGetValue(name, out dict);
            return dict ?? new Dictionary<string, string>();
        }


        public void Publish(string name, Dictionary<string, string> data) {
            var dict = new Dictionary<string, string>(data);
            publishedUserAttributes[name] = dict;
            //tas.Say(SayPlace.Channel, ExtensionChannelName, FormatMessage(name, data), false);
        }


        public void SendJsonData(object data)
        {
            tas.Say(SayPlace.Channel, ExtensionChannelName, EncodeJson(data), false);
        }


        public void SendJsonData(string username, object data) {
            tas.Say(SayPlace.User, username, EncodeJson(data), false);
        }

        public void SendJsonDataToChannel(string channel, object data)
        {
            tas.Say(SayPlace.Channel, channel, EncodeJson(data), false);
        }



        object DecodeJson(string data, TasSayEventArgs e) {
            try {
                var parts = data.Split(new[] { ' ' }, 3);
                if (parts[0] != "!JSON") return null;
                var payload = parts[2];
                var tname = parts[1];

                Type type;
                if (!typeCache.TryGetValue(tname, out type)) {
                    
                    type = Type.GetType(tname);
                    if (type == null) {
                        //We going to check if current application have the Class that we need. Reference: http://www.codeproject.com/Articles/38870/Examining-an-Assembly-at-Runtime
                        //If we do: "type = Assembly.GetExecutingAssembly().GetTypes().FirstOrDefault(x => x.Name == tname)"
                        //it will return exception "ReflectionTypeLoadException" in ZKL Linux because it doesn't like when
                        //referenced module (like TextToSpeech) is mentioned but not loaded.
                        foreach (Module namespace_obj in Assembly.GetExecutingAssembly().GetModules(false)) {
                            Type[] class_obj = namespace_obj.FindTypes(Module.FilterTypeName, tname);
                            if (class_obj != null && class_obj.Length > 0 && class_obj[0] != null) {
                                type = class_obj[0];
                                break;
                            }
                        }
                    }
                    if (type == null) type = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).FirstOrDefault(x => x.Name == tname);

                    typeCache[tname] = type;
                }

                if (type != null) {
                    var decoded = JsonConvert.DeserializeObject(payload, type);
                    if (decoded != null) JsonDataReceived(e, decoded);
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
            var payload = JsonConvert.SerializeObject(data, new JsonSerializerSettings() {Formatting = Formatting.None});
            return string.Format("!JSON {0} {1}", data.GetType().Name, payload);
        }

        static string Escape(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input.Replace("|", "&divider&");
        }

        static string Serialize(Dictionary<string, string> data) {
            return string.Join("|", data.Select(x => string.Format("{0}|{1}", Escape(x.Key), Escape(x.Value))).ToArray());
        }

        static string Unescape(string input) {
            return input.Replace("&divider&", "|");
        }



        void tas_PreviewSaid(object sender, CancelEventArgs<TasSayEventArgs> e) {
            if (e.Data.Channel == GlobalConst.NightwatchName || e.Data.UserName == GlobalConst.NightwatchName || tas.UserName == GlobalConst.NightwatchName) {
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

   
        public class SiteToLobbyCommand
        {
            public string SpringLink { get; set; }
        }
    }
}