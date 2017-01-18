using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LobbyClient;
using ZkData;

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
                var minEloOrg = MinElo;
                var maxEloOrg = MaxElo;
                if (player.Party != null)
                {
                    foreach (var p in allPlayers.Where(x => x.Party == player.Party))
                        if (!Players.Contains(p))
                        {
                            Trace.TraceError("MM: adding {0} to proposed battle", p);
                            Players.Add(p);
                        }
                    MinElo = Math.Min(MinElo, GetPartyMaxElo(player.Party, allPlayers));
                    MaxElo = Math.Max(MaxElo, GetPartyMinElo(player.Party, allPlayers));

                    Trace.TraceError("MM: added party MinElo: {0}->{1} ({4}),  MaxElo: {2}->{3} ({5})", minEloOrg, MinElo, maxEloOrg, MaxElo, GetPartyMaxElo(player.Party, allPlayers), GetPartyMinElo(player.Party, allPlayers));

                }
                else
                {
                    if (!Players.Contains(player))
                    {
                        Players.Add(player);
                        Trace.TraceError("MM: adding {0} to proposed battle", player);
                        MinElo = Math.Min(MinElo, GetPlayerMaxElo(player));
                        MaxElo = Math.Max(MaxElo, GetPlayerMinElo(player));

                        Trace.TraceError("MM: added player MinElo: {0}->{1} ({4}),  MaxElo: {2}->{3} ({5})", minEloOrg, MinElo, maxEloOrg, MaxElo, GetPlayerMaxElo(player), GetPlayerMinElo(player));
                    }
                }

            }

            
            public bool CanBeAdded(PlayerEntry other, List<PlayerEntry> allPlayers)
            {
                if (Players.Contains(other)) return false;
                if (owner.Party !=null && other.Party == owner.Party) return true; // always accept same party

                if (!other.GenerateWantedBattles(allPlayers).Any(y => (y.Size == Size) && (y.QueueType == QueueType)))
                {
                    Trace.TraceError("MM: cannot add {0}, does not want same game type", other.Name);
                    return false;
                }
                var width = owner.EloWidth * widthMultiplier;

                if (other.Party != null)
                {
                    if (!VerifyPartySizeFits(other.Party))
                    {
                        Trace.TraceError("MM: cannot add party {0}, party size does not fit", other.Name);
                        return false;
                    }

                    if ((GetPartyMinElo(other.Party, allPlayers) - MinElo > width) || (MaxElo - GetPartyMaxElo(other.Party, allPlayers) > width))
                    {
                        Trace.TraceError("MM: cannot add party {0}, {1} - {2} > {3} || {4} - {5} > {3}", other.Name, GetPartyMinElo(other.Party, allPlayers), MinElo, width, MaxElo, GetPartyMaxElo(other.Party, allPlayers));
                        return false;
                    }
                }
                else if ((GetPlayerMinElo(other) - MinElo > width) || (MaxElo - GetPlayerMaxElo(other) > width))
                {
                    Trace.TraceError("MM: cannot add {0}, {1} - {2} > {3} || {4} - {5} > {3}", other.Name, GetPlayerMinElo(other), MinElo, width, MaxElo, GetPlayerMaxElo(other));
                    return false;
                }

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

            private int GetPartyMaxElo(PartyManager.Party party, List<PlayerEntry> players)
            {
                return (int)Math.Round(players.Where(x => x.Party == party).Select(GetPlayerMaxElo).Average());
            }

            private int GetPartyMinElo(PartyManager.Party party, List<PlayerEntry> players)
            {
                return (int)Math.Round(players.Where(x => x.Party == party).Select(GetPlayerMinElo).Average());
            }


            private int GetPlayerMinElo(PlayerEntry entry)
            {
                return (int)Math.Round(CutOffFunc(entry.MinConsideredElo));
            }

            private bool VerifyPartySizeFits(PartyManager.Party party)
            {
                if (party.UserNames.Count + Players.Count > Size) return false;

                var existingPartySizes =
                    Players.Where(x => x.Party != null).GroupBy(x => x.Party).Select(x => x.Key.UserNames.Count).OrderByDescending(x => x).ToList();
                var maxTeamSize = Size / 2;
                var t1 = 0;
                var t2 = 0;
                foreach (var psize in existingPartySizes)
                    if (t1 + psize <= maxTeamSize) t1 += psize;
                    else if (t2 + psize <= maxTeamSize) t2 += psize;

                if ((party.UserNames.Count + t1 > maxTeamSize) && (party.UserNames.Count + t2 > maxTeamSize)) return false; // cannot fit new party to still balance
                return true;
            }
        }
    }
}