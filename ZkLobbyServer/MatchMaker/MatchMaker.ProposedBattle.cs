using System;
using System.Collections.Generic;
using System.Linq;
using LobbyClient;

namespace ZkLobbyServer
{
    public partial class MatchMaker {
        public class ProposedBattle
        {
            private PlayerEntry owner;
            public List<PlayerEntry> Players = new List<PlayerEntry>();
            public int Size { get; private set; }
            public int MaxElo { get; private set; } = int.MinValue;
            public int MinElo { get; private set; } = int.MaxValue;
            public MatchMakerSetup.Queue QueueType { get; private set; }
            private double eloCutOffExponent;

            private double widthMultiplier;

            public ProposedBattle(int size, PlayerEntry initialPlayer, MatchMakerSetup.Queue queue, double eloCutOffExponent)
            {
                Size = size;
                owner = initialPlayer;
                QueueType = queue;
                this.eloCutOffExponent = eloCutOffExponent;
                widthMultiplier = Math.Max(1.0, 1.0 + (Size - 4) * 0.1);
                AddPlayer(initialPlayer);
            }

            public void AddPlayer(PlayerEntry player)
            {
                Players.Add(player);
                MinElo = Math.Min(MinElo, GetPlayerMaxElo(player));
                MaxElo = Math.Max(MaxElo, GetPlayerMinElo(player));
            }

            public bool CanBeAdded(PlayerEntry other)
            {
                if (!other.GenerateWantedBattles().Any(y => y.Size == Size && y.QueueType == QueueType)) return false;
                var width = owner.EloWidth * widthMultiplier;

                if ((GetPlayerMinElo(other) - MinElo > width) || (MaxElo - GetPlayerMaxElo(other) > width)) return false;

                return true;
            }

            private double CutOffFunc(double input)
            {
                if (input >= 1500) return Math.Round(1500.0 + Math.Pow(input - 1500.0, eloCutOffExponent));
                else return 1500.0 - Math.Pow(1500.0 - input, eloCutOffExponent);
            }

            private int GetPlayerMinElo(PlayerEntry entry)
            {
                return (int)Math.Round(CutOffFunc(entry.MinConsideredElo));
            }

            private int GetPlayerMaxElo(PlayerEntry entry)
            {
                return (int)Math.Round(CutOffFunc(entry.MaxConsideredElo));
            }

        }
    }
}