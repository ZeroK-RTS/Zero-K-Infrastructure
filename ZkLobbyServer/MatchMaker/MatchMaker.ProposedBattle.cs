using System;
using System.Collections.Generic;
using System.Linq;
using LobbyClient;

namespace ZkLobbyServer
{
    public partial class MatchMaker
    {
        public class ProposedBattle
        {
            private double eloCutOffExponent;
            private PlayerEntry owner;
            public List<PlayerEntry> Players = new List<PlayerEntry>();

            private double widthMultiplier;
            public int MaxElo { get; private set; } = int.MinValue;
            public int MinElo { get; private set; } = int.MaxValue;
            public MatchMakerSetup.Queue QueueType { get; private set; }
            public int Size { get; private set; }

            public ProposedBattle(int size,
                PlayerEntry initialPlayer,
                MatchMakerSetup.Queue queue,
                double eloCutOffExponent,
                List<PlayerEntry> allPlayers)
            {
                Size = size;
                owner = initialPlayer;
                QueueType = queue;
                this.eloCutOffExponent = eloCutOffExponent;
                widthMultiplier = Math.Max(1.0, 1.0 + (Size - 4) * 0.1);
                AddPlayer(initialPlayer, allPlayers);
            }

            public void AddPlayer(PlayerEntry player, List<PlayerEntry> allPlayers)
            {
                if (player.Party != null)
                {
                    foreach (var p in allPlayers.Where(x => x.Party == player.Party))
                        if (!Players.Contains(p))
                        {
                            Players.Add(p);
                            MinElo = Math.Min(MinElo, GetPlayerMaxElo(p));
                            MaxElo = Math.Max(MaxElo, GetPlayerMinElo(p));
                        }
                }
                else
                {
                    if (!Players.Contains(player))
                    {
                        Players.Add(player);
                        MinElo = Math.Min(MinElo, GetPlayerMaxElo(player));
                        MaxElo = Math.Max(MaxElo, GetPlayerMinElo(player));
                    }
                }
            }

            public bool CanBeAdded(PlayerEntry other, List<PlayerEntry> allPlayers)
            {
                if (Players.Contains(other)) return false;
                if (other.Party == owner.Party) return true; // always accept same party

                if (!other.GenerateWantedBattles(allPlayers).Any(y => (y.Size == Size) && (y.QueueType == QueueType))) return false;
                var width = owner.EloWidth * widthMultiplier;

                if (other.Party != null)
                {
                    if (!VerifyPartySizeFits(other)) return false;

                    foreach (var p in allPlayers.Where(x => x.Party == other.Party)) if ((GetPlayerMinElo(p) - MinElo > width) || (MaxElo - GetPlayerMaxElo(p) > width)) return false;
                }
                else if ((GetPlayerMinElo(other) - MinElo > width) || (MaxElo - GetPlayerMaxElo(other) > width)) return false;

                return true;
            }

            private double CutOffFunc(double input)
            {
                if (input >= 1500) return Math.Round(1500.0 + Math.Pow(input - 1500.0, eloCutOffExponent));
                else return 1500.0 - Math.Pow(1500.0 - input, eloCutOffExponent);
            }

            private int GetPlayerMaxElo(PlayerEntry entry)
            {
                return (int)Math.Round(CutOffFunc(entry.MaxConsideredElo));
            }

            private int GetPlayerMinElo(PlayerEntry entry)
            {
                return (int)Math.Round(CutOffFunc(entry.MinConsideredElo));
            }

            private bool VerifyPartySizeFits(PlayerEntry other)
            {
                var existingPartySizes =
                    Players.Where(x => x.Party != null).GroupBy(x => x.Party).Select(x => x.Key.UserNames.Count).OrderByDescending(x => x).ToList();
                var maxTeamSize = Size / 2;
                var t1 = 0;
                var t2 = 0;
                foreach (var psize in existingPartySizes)
                    if (t1 + psize <= maxTeamSize) t1 += psize;
                    else if (t2 + psize <= maxTeamSize) t2 += psize;

                if ((other.Party.UserNames.Count + t1 > maxTeamSize) && (other.Party.UserNames.Count + t2 > maxTeamSize)) return false; // cannot fit new party to still balance
                return true;
            }
        }
    }
}