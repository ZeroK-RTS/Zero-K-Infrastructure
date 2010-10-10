#region using

using System;

#endregion

namespace PlanetWarsShared.Events
{
  [Serializable]
  public class AidSentEvent : Event
  {
    #region Properties

    public double Ammount;
    public string FromName;
    public string ToName;

    #endregion

    #region Constructors

    public AidSentEvent() {}

    public AidSentEvent(DateTime dateTime, Galaxy galaxy, string fromName, string toName, double ammount) : base(dateTime, galaxy)
    {
      FromName = fromName;
      ToName = toName;
      Ammount = ammount;
    }

    #endregion

    #region Overrides

    public override bool IsFactionRelated(string factionName)
    {
      return Galaxy.GetPlayer(FromName).FactionName == factionName;
    }

    public override bool IsHiddenFrom(string factionName)
    {
      return !IsFactionRelated(factionName);
    }

    public override bool IsPlanetRelated(int planetID)
    {
      return false;
    }

    public override bool IsPlayerRelated(string playerName)
    {
      return playerName == FromName || playerName == ToName;
    }

    public override string ToHtml()
    {
      return string.Format("{0} sent {1} credits to {2}", Player.ToHtml(FromName), Ammount, Player.ToHtml(ToName));
    }

    #endregion
  }
}