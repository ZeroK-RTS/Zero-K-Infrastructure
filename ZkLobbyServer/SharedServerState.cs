using System;
using System.Collections.Concurrent;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LobbyClient;
using MaxMind.Db;
using MaxMind.GeoIP2;
using ZkData;

namespace ZkLobbyServer
{
    public class SharedServerState
    {
        public int BattleCounter;
        public ConcurrentDictionary<int, Battle> Battles = new ConcurrentDictionary<int, Battle>();
        public int ClientCounter;
        public ConcurrentDictionary<string, ConnectedUser> ConnectedUsers = new ConcurrentDictionary<string, ConnectedUser>();
        public string Engine { get; set; }
        public string Game { get; set; }

        public LoginChecker LoginChecker;
        public ConcurrentDictionary<string, Channel> Rooms = new ConcurrentDictionary<string, Channel>();
        public CommandJsonSerializer Serializer = new CommandJsonSerializer();
        public string Version { get; private set; }


        public SharedServerState(string geoIPpath)
        {
            var entry = Assembly.GetExecutingAssembly();
            Version = entry.GetName().Version.ToString();
            Engine = GlobalConst.DefaultEngineOverride;
            Game = "zk:stable";
            LoginChecker = new LoginChecker(this, geoIPpath);

            /*JoinedChannel += async (sender, added) => {
                using (var db = new ZkDataContext()) {
                    await
                        db.LobbyChatHistories.Where(x => x.Target == added.ChannelName && x.SayPlace == SayPlace.Channel)
                            .OrderByDescending(x => x.Time)
                            .Take(1000)
                            .ForEachAsync(async (chatHistory) => {
                                ConnectedUser conus;
                                if (ConnectedUsers.TryGetValue(added.UserName, out conus)) {
                                    await
                                        conus.SendCommand(new Say() {
                                            IsEmote = chatHistory.IsEmote,
                                            Ring = chatHistory.Ring,
                                            Text = chatHistory.Text,
                                            User = chatHistory.User,
                                            Time = chatHistory.Time,
                                            Place = chatHistory.SayPlace,
                                            Target = chatHistory.Target
                                        });
                                }
                            });
                }
            };*/
        }

        public async Task StoreChatHistory(Say say)
        {
            using (var db = new ZkDataContext()) {
                db.LobbyChatHistories.Add(new LobbyChatHistory() {
                    Text = say.Text,
                    IsEmote = say.IsEmote,
                    Ring = say.Ring,
                    User = say.User,
                    Target = say.Target,
                    SayPlace = say.Place,
                    Time = say.Time ?? DateTime.UtcNow,
                });
                await db.SaveChangesAsync();
            }
        }
    }
}