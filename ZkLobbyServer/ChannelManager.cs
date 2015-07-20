using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZkData;

namespace ZkLobbyServer
{
    public class ChannelManager
    {
        const int topPlayersRefreshMinutes = 60;
        readonly ConcurrentDictionary<string, Clan> clanChannels = new ConcurrentDictionary<string, Clan>();
        readonly ConcurrentDictionary<string, Faction> factionChannels = new ConcurrentDictionary<string, Faction>();

        DateTime lastRefresh = DateTime.MinValue;
        ZkLobbyServer server;
        List<Account> top1v1 = new List<Account>();
        readonly List<int> topPlayersExceptions = new List<int> { 5986, 5806 }; // licho, kingraptor
        List<Account> topTeam = new List<Account>();

        public ChannelManager(ZkLobbyServer server)
        {
            this.server = server;
            using (var db = new ZkDataContext()) {
                factionChannels = new ConcurrentDictionary<string, Faction>(db.Factions.Where(x => !x.IsDeleted).ToDictionary(x => x.Shortcut, x => x));
                clanChannels =
                    new ConcurrentDictionary<string, Clan>(db.Clans.Where(x => !x.IsDeleted).ToList().ToDictionary(x => x.GetClanChannel(), x => x));
            }
        }

        public async Task<bool> CanJoin(int accountID, string channel)
        {
            using (var db = new ZkDataContext()) {
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

        public List<string> GetDefaultChannels(Account acc)
        {
            var ret = new List<string>() { "zk", GlobalConst.ModeratorChannel, GlobalConst.Top20Channel };
            if (acc.Clan != null) ret.Add(acc.Clan.GetClanChannel());
            if (acc.Faction != null) ret.Add(acc.Faction.Shortcut);

            return ret.Where(x => CanJoin(acc, x)).ToList();
        }

        public bool IsTop20(int lobbyID)
        {
            if (DateTime.UtcNow.Subtract(lastRefresh).TotalMinutes > topPlayersRefreshMinutes) Refresh(20);

            if (topTeam.Any(x => x.AccountID == lobbyID) || top1v1.Any(x => x.AccountID == lobbyID) || topPlayersExceptions.Contains(lobbyID)) return true;
            else return false;
        }


        public void Refresh(int count = 20)
        {
            var lastMonth = DateTime.UtcNow.AddMonths(-1);
            using (var db = new ZkDataContext()) {
                topTeam =
                    db.Accounts.Where(x => x.SpringBattlePlayers.Any(y => y.SpringBattle.StartTime > lastMonth))
                        .OrderByDescending(x => x.Elo)
                        .Take(count)
                        .ToList();
                top1v1 =
                    db.Accounts.Where(x => x.SpringBattlePlayers.Any(y => y.SpringBattle.StartTime > lastMonth))
                        .OrderByDescending(x => x.Elo1v1)
                        .Take(count)
                        .ToList();
                lastRefresh = DateTime.UtcNow;
            }
        }

        bool CanJoin(Account acc, string channel)
        {
            if (channel == GlobalConst.ModeratorChannel) return acc.IsZeroKAdmin || acc.SpringieLevel > 2;
            else if (channel == GlobalConst.Top20Channel) return IsTop20(acc.AccountID);
            else if (clanChannels.ContainsKey(channel)) return acc.ClanID == clanChannels[channel].ClanID;
            else if (factionChannels.ContainsKey(channel) && acc.Level >= GlobalConst.FactionChannelMinLevel) return acc.FactionID == factionChannels[channel].FactionID;
            return true;
        }
    }
}