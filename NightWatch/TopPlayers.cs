using System;
using System.Collections.Generic;
using System.Linq;
using ZkData;

namespace NightWatch
{
    public class TopPlayers
    {
        const int RefreshMinutes = 60;
        const int TakeCount = 100;
        readonly List<int> exceptions = new List<int> { 5986, 45679, 5806 }; // licho, nightwatch, kingraptor

        DateTime lastRefresh = DateTime.MinValue;
        List<Account> top1v1 = new List<Account>();
        List<Account> topTeam = new List<Account>();


        public List<Account> GetTop1v1() {
            return top1v1.ToList();
        }

        public List<Account> GetTopTeam() {
            return topTeam.ToList();
        }

        public bool IsTop20(int lobbyID) {
            if (DateTime.UtcNow.Subtract(lastRefresh).TotalMinutes > RefreshMinutes) Refresh();

            if (topTeam.Take(20).Any(x => x.AccountID == lobbyID) || top1v1.Take(20).Any(x => x.AccountID == lobbyID) || exceptions.Contains(lobbyID)) return true;
            else return false;
        }

        public void Refresh()
        {
            var lastMonth = DateTime.UtcNow.AddMonths(-1);
            using (var db = new ZkDataContext()) {
                 
                topTeam =
                    db.Accounts.Where(x => x.SpringBattlePlayers.Any(y => y.SpringBattle.StartTime > lastMonth))
                      .OrderByDescending(x => x.Elo)
                      .Take(TakeCount)
                      .ToList();
                top1v1 =
                    db.Accounts.Where(x => x.SpringBattlePlayers.Any(y => y.SpringBattle.StartTime > lastMonth))
                      .OrderByDescending(x => x.Elo1v1)
                      .Take(TakeCount)
                      .ToList();
                lastRefresh = DateTime.UtcNow;
            }
        }
    }
}