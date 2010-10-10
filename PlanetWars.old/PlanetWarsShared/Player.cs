#region using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;
using PlanetWarsShared.Springie;

#endregion

namespace PlanetWarsShared
{
    [Serializable, DebuggerDisplay("{Name} ({FactionName})")]
    public class Player : IPlayer
    {
        #region Properties

        public double Elo = 1500;
        public double Elo1v1 = 1500;
        public int Defeats { get; set; }
        public Rank Rank { get; set; }

        public double RankPoints { get; set; }
        public int Victories { get; set; }
        public bool HasChangedMap { get; set; }
        public string Title { get; set; }

        public int MeasuredVictories { get; set; }
        public int MeasuredDefeats { get; set; }

        public List<PurchaseData> PurchaseHistory { get; set;}
        public List<KeyValuePair<DateTime, double>> IncomeHistory { get; set;}

        /// <summary>
        /// number of times this player was a general
        /// </summary>
        public int Clout { get; set; }

        public string FactionName { get; set; }

        #endregion

        #region Constructors

        public Player(string name, string factionName)
        {
            Name = name;
            FactionName = factionName;
            Rank = Rank.Commander;
            Title = Rank.CommanderInChief.ToString();
            RankOrder = int.MaxValue;
            ReminderRoundInitiative = ReminderRoundInitiative.Defense | ReminderRoundInitiative.Offense;
            ReminderLevel = ReminderLevel.MyPlanet;
            ReminderEvent = ReminderEvent.OnBattlePreparing;
            PurchaseHistory = new List<PurchaseData>();
            //LocalTimeZoneSerialized = TimeZoneInfo.Local.ToSerializedString();
            LocalTimeZoneSerialized = TimeZoneInfo.Local.Id;
            IncomeHistory = new List<KeyValuePair<DateTime, double>>();
        }

        public Player() : this("", "") {}

        #endregion

        public List<Award> Awards = new List<Award>();

        public double SentMetal { get; set; }
        public double RecievedMetal { get; set; }

        public string Description { get; set; }

        double metalEarned;
        public double MetalEarned
        {
            get { return metalEarned; }
            set
            {
                IncomeHistory.Add(new KeyValuePair<DateTime, double>(DateTime.Now, value));
                metalEarned = value;
            }
        }

        public double MetalSpent { get; set; }

        public double MetalAvail
        {
            get { return MetalEarned - MetalSpent; }
        }

        [XmlIgnore]
        public TimeZoneInfo LocalTimeZone
        {
            get
            {
                if (string.IsNullOrEmpty(LocalTimeZoneSerialized)) {
                    return TimeZoneInfo.Local;
                }
                try {
                    return TimeZoneInfo.FindSystemTimeZoneById(LocalTimeZoneSerialized);
                } catch (TimeZoneNotFoundException) {
                    return TimeZoneInfo.Local;
                }
            }
            set { LocalTimeZoneSerialized = value.Id; }
        }

        public string LocalTimeZoneSerialized { get; set; }

        #region IPlayer Members

        [XmlIgnore]
        public string RankText
        {
            get { return Rank == Rank.CommanderInChief ? Title : Rank.ToString(); }
        }

        public string Name { get; set; }

        /// <summary>
        /// 0 = highest ranking player in given team
        /// </summary>
        public int RankOrder { get; set; }

        [XmlIgnore]
        public bool IsCommanderInChief
        {
            get { return Rank == Rank.CommanderInChief; }
        }

        public ReminderEvent ReminderEvent { get; set; }

        public ReminderLevel ReminderLevel { get; set; }

        public ReminderRoundInitiative ReminderRoundInitiative { get; set; }

        #endregion

        public static int CompareTo(Player a, Player b)
        {
            if (a.RankPoints > b.RankPoints) {
                return 1;
            } else if (a.RankPoints < b.RankPoints) {
                return -1;
            } else {
                return (a.Victories + a.Defeats).CompareTo(b.Victories + b.Defeats);
            }
        }

        public override string ToString()
        {
            return ToHtml(Name);
        }

        public static string ToHtml(string name)
        {
            return string.Format("<a href='player.aspx?name={0}'>{0}</a>", name);
        }
    }
}