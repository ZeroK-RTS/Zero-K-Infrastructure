using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Principal;

namespace ZkData
{
  partial class Account: IPrincipal, IIdentity
  {
    public double EloInvWeight { get { return GlobalConst.EloWeightMax + 1 - EloWeight; } }
    /// <summary>
    /// Aggregate admin rights - either lobby or ZK admin
    /// </summary>
    public bool IsAdmin { get { return IsLobbyAdministrator || IsZeroKAdmin; } }

    partial void OnXPChanged()
    {
      CheckLevelUp();
    }


    public void CheckLevelUp()
    {
      if (XP > GetXpForLevel(Level + 1))
      {
        Level++;
        try {
          AuthServiceClient.SendLobbyMessage(this,
                                             string.Format(
                                               "Congratulations! You just leveled up to level {0}. spring://http://zero-k.info/Users.mvc/{1}",
                                               Level,
                                               Name));
        } catch (Exception ex)
        {
          Trace.TraceError("Error sending level up lobby message: {0}", ex);
        }
      }
    }

    public static int GetXpForLevel(int level)
    {
      return level*80 + 20*level*level;
    }

    partial void OnCreated()
    {
      FirstLogin = DateTime.UtcNow;
      Elo = 1500;
      EloWeight = 1;
    }

    partial void OnNameChanging(string value)
    {
      if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(value))
      {
        List<string> aliases = null;
        if (!string.IsNullOrEmpty(Aliases)) aliases = new List<string>(Aliases.Split(','));
        else aliases = new List<string>();

        if (!aliases.Contains(Name)) aliases.Add(Name);
        Aliases = string.Join(",", aliases.ToArray());
      }
    }

    public string AuthenticationType { get { return "LobbyServer"; } }
    public bool IsAuthenticated { get { return true; } }

    public bool IsInRole(string role)
    {
      if (role == "admin" || role == "moderator") return IsAdmin;
      if (role == "user") return true;
      else return string.IsNullOrEmpty(role);
    }

    public IIdentity Identity { get { return this; } }
  }
}