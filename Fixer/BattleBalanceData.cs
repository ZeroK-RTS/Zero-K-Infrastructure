using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZkData;

namespace Fixer
{
    public class BattleBalanceData
    {
        public static int MAX_TEAMSIZE = 12;
        int battleID;
        double t1Avg;
        double t2Avg;
        double t1Variance;
        double t2Variance;
        List<double> t1Elo;
        List<double> t2Elo;
        List<string> t1Names;
        List<string> t2Names;
        static string header = GetStringHeader();

        public BattleBalanceData(int battleID)
        {
            ZkDataContext db = new ZkDataContext();
            SpringBattle game = db.SpringBattles.FirstOrDefault(x => x.SpringBattleID == battleID);
            this.battleID = battleID;
            this.t1Elo = new List<double>();
            this.t2Elo = new List<double>();
            this.t1Names = new List<string>();
            this.t2Names = new List<string>();
            double t1Sum = 0;
            double t2Sum = 0;
            int t1Count = 0;
            int t2Count = 0;
            foreach (SpringBattlePlayer player in game.SpringBattlePlayers.Where(x=> !x.IsSpectator))
            {
                if (player.IsInVictoryTeam)
                {
                    t1Sum += player.Account.EffectiveElo;
                    this.t1Elo.Add(Math.Floor(player.Account.EffectiveElo+0.5));
                    this.t1Names.Add(player.Account.Name);
                    t1Count++;
                }
                else
                {
                    t2Sum += player.Account.EffectiveElo;
                    this.t2Elo.Add(Math.Floor(player.Account.EffectiveElo + 0.5));
                    this.t2Names.Add(player.Account.Name);
                    t2Count++;
                }
            }
            this.t1Avg = Math.Floor(t1Sum / t1Count + 0.5);
            this.t2Avg = Math.Floor(t2Sum / t2Count + 0.5);
            this.t1Variance = Math.Floor(Variance(this.t1Elo, this.t1Avg) + 0.5);
            this.t2Variance = Math.Floor(Variance(this.t2Elo, this.t2Avg) + 0.5);
        }

        public static string GetStringHeader()
        {
            List<string> strings = new List<string>();
            strings.Add("BattleID");
            strings.Add("WinAvg");
            strings.Add("LoseAvg");
            strings.Add("WinVariance");
            strings.Add("LoseVariance");
            for (int i=1; i<=MAX_TEAMSIZE; i++)
            {
                strings.Add("WinElo"+i);
            }
            for (int i=1; i<=MAX_TEAMSIZE; i++)
            {
                strings.Add("LoseElo"+i);
            }
            for (int i=1; i<=MAX_TEAMSIZE; i++)
            {
                strings.Add("WinName"+i);
            }
            for (int i=1; i<=MAX_TEAMSIZE; i++)
            {
                strings.Add("LoseName"+i);
            }
            return string.Join(",", strings);
        }

        public static double Variance(List<double> values, double mean)
        {
            double variance = 0;
 
            foreach(double value in values)
            {
                variance += Math.Pow((value - mean), 2);
            }
 
            return variance / values.Count;
        }

        public string WriteGameLine()
        {
            List<string> strings = new List<string>();
            strings.Add(battleID.ToString());
            strings.Add(t1Avg.ToString());
            strings.Add(t2Avg.ToString());
            strings.Add(t1Variance.ToString());
            strings.Add(t2Variance.ToString());
            for (int i=0; i<MAX_TEAMSIZE; i++)
            {
                if (i < t1Elo.Count) strings.Add(t1Elo[i].ToString());
                else strings.Add("");
            }
            for (int i=0; i<MAX_TEAMSIZE; i++)
            {
                if (i < t2Elo.Count) strings.Add(t2Elo[i].ToString());
                else strings.Add("");
            }
            for (int i=0; i<MAX_TEAMSIZE; i++)
            {
                if (i < t1Names.Count) strings.Add(t1Names[i].ToString());
                else strings.Add("");
            }
            for (int i=0; i<MAX_TEAMSIZE; i++)
            {
                if (i < t2Names.Count) strings.Add(t2Names[i].ToString());
                else strings.Add("");
            }
            return string.Join(",", strings);
        }

        public static void PrintBattleData(List<SpringBattle> battles)
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"ZKGameData.csv"))
            {
                file.WriteLine(header);
                foreach (SpringBattle battle in battles)
                {
                    BattleBalanceData data = new BattleBalanceData(battle.SpringBattleID);
                    file.WriteLine(data.WriteGameLine());
                }
            }
        }

        public static void AnalyseBalance()
        {
            ZkDataContext db = new ZkDataContext();
            var games = db.SpringBattles.Where(x => DateTime.Now - x.StartTime < TimeSpan.FromDays(60) && !x.IsFfa && !x.IsMission && !x.HasBots && x.PlayerCount >= 6).ToList();
            Console.WriteLine(games.Count);
            List<SpringBattle> games2 = new List<SpringBattle>();
            int numProcessed = 0;
            foreach (SpringBattle game in games)
            {
                bool anyInvalidPlayers = false;
                int count = 0;
                foreach (SpringBattlePlayer player in game.SpringBattlePlayers.Where(x => !x.IsSpectator))
                {
                    if (player.Account.EloWeight < 5)
                    {
                        anyInvalidPlayers = true;
                        break;
                    }
                    else count++;
                }
                if (!anyInvalidPlayers && count >= 6 && count <= 24) games2.Add(game);
                numProcessed++;
                if (numProcessed % 50 == 0) Console.WriteLine("{0} of {1} selected", games2.Count, numProcessed);
            }

            Console.WriteLine(games2.Count);
            BattleBalanceData.PrintBattleData(games2);
        }
    }
}
