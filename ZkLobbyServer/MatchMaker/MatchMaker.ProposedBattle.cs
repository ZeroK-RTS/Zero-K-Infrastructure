using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LobbyClient;
using PlasmaShared;
using ZkData;
using static ZeroKWeb.SpringieInterface.PartitionBalance;

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
            private bool hasParty;
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
                //Trace.TraceError("MM: proposed battle {0} adding {1}", string.Join(", ", Players.Select(x=>x.Name)), player.Name);
                //var minEloOrg = MinElo;
                //var maxEloOrg = MaxElo;
                if (player.Party != null)
                {
                    foreach (var p in allPlayers.Where(x => x.Party == player.Party))
                        if (!Players.Contains(p))
                        {
                            Players.Add(p);
                        }
                    MinElo = Math.Min(MinElo, GetPartyMaxElo(player.Party, allPlayers));
                    MaxElo = Math.Max(MaxElo, GetPartyMinElo(player.Party, allPlayers));

                    hasParty = true;
                    //Trace.TraceError("MM: added party {6} MinElo: {0}->{1} ({4}),  MaxElo: {2}->{3} ({5})", minEloOrg, MinElo, maxEloOrg, MaxElo, GetPartyMaxElo(player.Party, allPlayers), GetPartyMinElo(player.Party, allPlayers), player.Name);

                }
                else
                {
                    if (!Players.Contains(player))
                    {
                        Players.Add(player);
                        MinElo = Math.Min(MinElo, GetPlayerMaxElo(player));
                        MaxElo = Math.Max(MaxElo, GetPlayerMinElo(player));

                        //Trace.TraceError("MM: added player {6} MinElo: {0}->{1} ({4}),  MaxElo: {2}->{3} ({5})", minEloOrg, MinElo, maxEloOrg, MaxElo, GetPlayerMaxElo(player), GetPlayerMinElo(player), player.Name);
                    }
                }

            }

            public bool VerifyBalance(double minimumTeamWinChance, double minimum1v1WinChance)
            {
                try
                {
                    var minimumWinChance = minimumTeamWinChance;
                    if (QueueType.Mode != AutohostMode.Teams)
                        minimumWinChance = minimum1v1WinChance;

                    if (minimumWinChance <= 0.01) return true;
                    if (!QueueType.UseWinChanceLimit) return true;
                    minimumWinChance = minimumWinChance * QueueType.MinWinChanceMult + QueueType.MinWinChanceOffset;

                    var players = Players.Select(x => x.LobbyUser).Select(x => new PlayerItem(x.AccountID, x.EffectiveMmElo, x.Clan, x.PartyID)).ToList();
                    if (QueueType.UseCasualElo)
					{
						players = Players.Select(x => x.LobbyUser).Select(x => new PlayerItem(x.AccountID, x.EffectiveElo, x.Clan, x.PartyID)).ToList();
					}
                    return Balance(ZeroKWeb.SpringieInterface.Balancer.BalanceMode.Party, players).LowestWinChance > minimumWinChance;
                }
                catch (Exception ex)
                {
                    Trace.TraceError("MatchMaker error checking balance: \n{0}", ex);
                    return true;
                }
            }

            public bool CanBeAdded(PlayerEntry other, List<PlayerEntry> allPlayers, bool ignoreSizeLimit)
            {
                //Trace.TraceError("MM: proposed battle {0} checking {1}", string.Join(", ", Players.Select(x => x.Name)), other.Name);

                if (Players.Contains(other))
                {
                    //Trace.TraceError("MM: cannot add {0}, already added", other.Name);
                    return false;
                }
                if (owner.Party !=null && other.Party == owner.Party) return true; // always accept same party

                if (!other.GenerateWantedBattles(allPlayers, ignoreSizeLimit).Any(y => (y.Size == Size) && (y.QueueType == QueueType)))
                {
                    //Trace.TraceError("MM: cannot add {0}, does not want same game type", other.Name);
                    return false;
                }

                var width = owner.EloWidth * widthMultiplier;
                if (hasParty)
                    width = width * DynamicConfig.Instance.MmWidthReductionForParties;

                if (other.Party != null)
                {
                    if (!hasParty)
                        width = width * DynamicConfig.Instance.MmWidthReductionForParties;

                    if (!VerifyPartySizeFits(other.Party))
                    {
                        //Trace.TraceError("MM: cannot add party {0}, party size does not fit", other.Name);
                        return false;
                    }

                    if ((GetPartyMinElo(other.Party, allPlayers) - MinElo > width) || (MaxElo - GetPartyMaxElo(other.Party, allPlayers) > width))
                    {
                        //Trace.TraceError("MM: cannot add party {0}, {1} - {2} > {3} || {4} - {5} > {3}", other.Name, GetPartyMinElo(other.Party, allPlayers), MinElo, width, MaxElo, GetPartyMaxElo(other.Party, allPlayers));
                        return false;
                    }
                }
                else if ((GetPlayerMinElo(other) - MinElo > width) || (MaxElo - GetPlayerMaxElo(other) > width))
                {
                    //Trace.TraceError("MM: cannot add {0}, {1} - {2} > {3} || {4} - {5} > {3}", other.Name, GetPlayerMinElo(other), MinElo, width, MaxElo, GetPlayerMaxElo(other));
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

                if (QueueType.Mode != AutohostMode.GameChickens)
                {

                    var existingPartySizes =
                        Players.Where(x => x.Party != null)
                            .GroupBy(x => x.Party)
                            .Select(x => x.Key.UserNames.Count)
                            .OrderByDescending(x => x)
                            .ToList();
                    var maxTeamSize = Size/2;
                    var t1 = 0;
                    var t2 = 0;
                    foreach (var psize in existingPartySizes)
                        if (t1 + psize <= maxTeamSize) t1 += psize;
                        else if (t2 + psize <= maxTeamSize) t2 += psize;

                    if ((party.UserNames.Count + t1 > maxTeamSize) && (party.UserNames.Count + t2 > maxTeamSize)) return false; // cannot fit new party to still balance
                }

                return true;
            }
        }
    }
}
