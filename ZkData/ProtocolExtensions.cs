using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LobbyClient;

namespace ZkData
{
    public static class ProtocolExt
    {
        public static void PublishAccountData(this ProtocolExtension proto, Account acc)
        {
            var tas = proto.tas;
            if (acc != null && tas.ExistingUsers.ContainsKey(acc.Name))
            {
                var data = new Dictionary<string, string>
                {
                    { ProtocolExtension.Keys.Level.ToString(), acc.Level.ToString() },
                    { ProtocolExtension.Keys.EffectiveElo.ToString(), ((int)acc.EffectiveElo).ToString() },
                    { ProtocolExtension.Keys.Faction.ToString(), acc.Faction != null ? acc.Faction.Shortcut : "" },
                    { ProtocolExtension.Keys.Clan.ToString(), acc.Clan != null ? acc.Clan.Shortcut : "" },
                    { ProtocolExtension.Keys.Avatar.ToString(), acc.Avatar },
                    { ProtocolExtension.Keys.SpringieLevel.ToString(), acc.GetEffectiveSpringieLevel().ToString() },
                };
                if (acc.SteamID != null) data.Add(ProtocolExtension.Keys.SteamID.ToString(), acc.SteamID.ToString());
                if (!string.IsNullOrEmpty(acc.SteamName) && acc.SteamName != acc.Name) data.Add(ProtocolExtension.Keys.DisplayName.ToString(), acc.SteamName);

                if (acc.IsZeroKAdmin) data.Add(ProtocolExtension.Keys.ZkAdmin.ToString(), "1");

                if (acc.PunishmentsByAccountID.Any(x => !x.IsExpired && x.BanMute)) data.Add(ProtocolExtension.Keys.BanMute.ToString(), "1");
                if (acc.PunishmentsByAccountID.Any(x => !x.IsExpired && x.BanLobby)) data.Add(ProtocolExtension.Keys.BanLobby.ToString(), "1");

                tas.Extensions.Publish(acc.Name, data);

                // if (acc.PunishmentsByAccountID.Any(x => x.BanExpires > DateTime.UtcNow && x.BanLobby)) tas.AdminKickFromLobby(acc.Name, "Banned");
                var penalty = acc.PunishmentsByAccountID.FirstOrDefault(x => x.BanExpires > DateTime.UtcNow && x.BanLobby);
                if (penalty != null) tas.AdminKickFromLobby(acc.Name, string.Format("Banned until {0}, reason: {1}", penalty.BanExpires, penalty.Reason));
            }
        }


    }
}
