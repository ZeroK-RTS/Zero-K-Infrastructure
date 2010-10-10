#region using

using System;
using System.Xml.Serialization;

#endregion

namespace PlanetWarsShared.Events
{
    [Serializable, XmlInclude(typeof (BattleEvent)), XmlInclude(typeof (MapChangedEvent)),
     XmlInclude(typeof (PlanetNameChangedEvent)), XmlInclude(typeof (PlayerRegisteredEvent)),
     XmlInclude(typeof (RankNameChangedEvent)), XmlInclude(typeof (AidSentEvent)),
     XmlInclude(typeof (PlayerRankChangedEvent)), XmlInclude(typeof (PlanetOwnerChangedEvent))]
    public abstract class Event
    {
        #region Properties

        [XmlIgnore]
        public Galaxy Galaxy { get; set; }

        public int Round { get; set; }

        public DateTime Time { get; set; }
        
        public int Turn { get; set; }

        public string TimeOffset(Player p)
        {
            return TimeOffset(p, false);
        }

        public string TimeOffset(Player p, bool shortTime)
        {
            if (p != null)
            {
                DateTime newTime = DateTime.SpecifyKind(Time, DateTimeKind.Utc);
                string time = shortTime ? TimeZoneInfo.ConvertTimeFromUtc(newTime, p.LocalTimeZone).ToShortTimeString() : TimeZoneInfo.ConvertTimeFromUtc(newTime, p.LocalTimeZone).ToString();
                return time + " (" + p.LocalTimeZone.BaseUtcOffset.ToString().TrimEnd('0').TrimEnd(':') +")"; 
            }
            return shortTime ? "" + Time.ToShortTimeString() : "" + Time; 
            
        }

        #endregion

        #region Constructors

        protected Event() {}

        protected Event(DateTime dateTime, Galaxy galaxy)
        {
            Time = dateTime.ToUniversalTime();
            Galaxy = galaxy;
            Turn = galaxy.Turn;
            Round = galaxy.Round;
        }

        #endregion

        #region Public methods

        public abstract bool IsFactionRelated(string factionName);

        public virtual bool IsHiddenFrom(string factionName)
        {
            return false;
        }

        public abstract bool IsPlanetRelated(int planetID);
        public abstract bool IsPlayerRelated(string playerName);

        public abstract string ToHtml();

        #endregion
    }
}
