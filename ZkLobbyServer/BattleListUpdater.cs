using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using LobbyClient;

namespace ZkLobbyServer
{
    public class BattleListUpdater
    {
        private const int UpdateIntervalSeconds = 10;
        private ZkLobbyServer server;

        private HashSet<Tuple<int, int, int>> storedCounts = new HashSet<Tuple<int, int, int>>();
        private Timer timer;

        public BattleListUpdater(ZkLobbyServer server)
        {
            this.server = server;
            timer = new Timer(UpdateIntervalSeconds*1000);
            timer.Elapsed += TimerOnElapsed;
            timer.Start();
        }

        private async void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            try
            {
                timer.Stop();
                var counts =
                    new HashSet<Tuple<int, int, int>>(
                        server.Battles.Values.Where(x => x != null).Select(x => Tuple.Create(x.BattleID, x.NonSpectatorCount, x.SpectatorCount)));

                foreach (var c in counts)
                    if (!storedCounts.Contains(c))
                        await
                            server.Broadcast(new BattleUpdate()
                            {
                                Header = new BattleHeader() { BattleID = c.Item1, PlayerCount = c.Item2, SpectatorCount = c.Item3 }
                            });

                storedCounts = counts;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error in playerlist updater: {0}", ex);
            }

            finally
            {
                timer.Start();
            }
        }
    }
}