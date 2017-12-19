using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;
using Ratings;

namespace ZkLobbyServer
{
    public class ChannelManager
    {
        readonly ConcurrentDictionary<string, Clan> clanChannels = new ConcurrentDictionary<string, Clan>();
        readonly ConcurrentDictionary<string, Faction> factionChannels = new ConcurrentDictionary<string, Faction>();

        ZkLobbyServer server;
        readonly List<int> topPlayersExceptions = new List<int> { 5986, 5806 }; // licho, kingraptor

        public ChannelManager(ZkLobbyServer server)
        {
            this.server = server;


            using (var db = new ZkDataContext())
            {
                factionChannels = new ConcurrentDictionary<string, Faction>(db.Factions.Where(x => !x.IsDeleted).ToDictionary(x => x.Shortcut, x => x));
                clanChannels =
                    new ConcurrentDictionary<string, Clan>(db.Clans.Where(x => !x.IsDeleted).ToList().ToDictionary(x => x.GetClanChannel(), x => x));

                // restore topics from db
                foreach (var channelTopic in db.LobbyChannelTopics)
                {
                    server.Channels[channelTopic.ChannelName] = new Channel()
                    {
                        Name = channelTopic.ChannelName,
                        Topic = new Topic() { Text = channelTopic.Topic, SetBy = channelTopic.Author, SetDate = channelTopic.SetDate }
                    };
                }
            }

            // set "zk" as deluge
            server.Channels.AddOrUpdate("zk", s => new Channel() { Name = "zk", IsDeluge = true }, (s, ch) =>
            {
                ch.IsDeluge = true;
                return ch;
            });
        }

        public async Task<bool> CanJoin(int accountID, string channel)
        {
            using (var db = new ZkDataContext())
            {
                var acc = await db.Accounts.FindAsync(accountID);
                return CanJoin(acc, channel);
            }
        }

        public async Task<List<string>> GetDefaultChannels(int accountID)
        {
            using (var db = new ZkDataContext())
            {
                var acc = await db.Accounts.FindAsync(accountID);
                return GetDefaultChannels(acc);
            }
        }

        public void AddClanChannel(Clan clan)
        {
            clanChannels[clan.GetClanChannel()] = clan;
        }

        public List<string> GetDefaultChannels(Account acc)
        {
            if (acc.IsBot) return new List<string>() { "bots" };

            var ret = new List<string>() { "zk", GlobalConst.ModeratorChannel, GlobalConst.Top20Channel, GlobalConst.CoreChannel };
            if (acc.Clan != null) ret.Add(acc.Clan.GetClanChannel());
            if (acc.Faction != null && GlobalConst.PlanetWarsMode != PlanetWarsModes.AllOffline) ret.Add(acc.Faction.Shortcut);

            return ret.Where(x => CanJoin(acc, x)).ToList();
        }

        public bool IsTop20(int lobbyID)
        {
            if (topPlayersExceptions.Contains(lobbyID)) return true;
            if (RatingSystems.GetRatingSystem(RatingCategory.Casual).GetPlayerRating(lobbyID).Rank <= 20) return true;
            if (RatingSystems.GetRatingSystem(RatingCategory.MatchMaking).GetPlayerRating(lobbyID).Rank <= 20) return true;
            return false;
        }


        public bool CanJoin(Account acc, string channel)
        {
            if (channel.StartsWith(PartyManager.PartyChannelPrefix)) return server.PartyManager.CanJoinChannel(acc.Name, channel);
            else if (channel == GlobalConst.ModeratorChannel) return acc.AdminLevel >= AdminLevel.Moderator;
            else if (channel == GlobalConst.ErrorChannel) return acc.AdminLevel >= AdminLevel.SuperAdmin;
            else if (channel == GlobalConst.Top20Channel) return IsTop20(acc.AccountID);
            else if (channel == GlobalConst.CoreChannel) return acc.DevLevel >= DevLevel.RetiredCoreDeveloper;
            else if (clanChannels.ContainsKey(channel)) return acc.ClanID == clanChannels[channel].ClanID;
            else if (factionChannels.ContainsKey(channel)) return acc.Level >= GlobalConst.FactionChannelMinLevel && acc.FactionID == factionChannels[channel].FactionID;
            return true;
        }
    }
}
