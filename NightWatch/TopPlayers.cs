using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZkData;

namespace NightWatch
{
    public class TopPlayers
    {
        List<Account> top1v1 = new List<Account>();
        List<Account> topTeam = new List<Account>();

        List<int> exceptions = new List<int>() { 5986, 45679 }; // licho, nightwatch

        const int TakeCount = 100;
        const int RefreshMinutes = 60;

        DateTime lastRefresh = DateTime.MinValue;
        
        
        public void Refresh() {
            using (var db = new ZkDataContext()) {
                topTeam = db.Accounts.OrderByDescending(x => x.Elo).Take(TakeCount).ToList();
                top1v1 = db.Accounts.OrderByDescending(x => x.Elo1v1).Take(TakeCount).ToList();
                lastRefresh = DateTime.UtcNow;
            }
        }

        public List<Account> GetTopTeam() {
            return topTeam.ToList();
        }

        public List<Account> GetTop1v1() {
            return top1v1.ToList();
        }

        public bool IsTop20(int lobbyID) {
            if (DateTime.UtcNow.Subtract(lastRefresh).TotalMinutes > RefreshMinutes) Refresh();

            if (topTeam.Take(20).Any(x => x.LobbyID == lobbyID) || top1v1.Take(20).Any(x => x.AccountID == lobbyID) || exceptions.Contains(lobbyID)) return true;
            else return false;
        }

    }
}
