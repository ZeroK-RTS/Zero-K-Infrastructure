using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace NightWatch
{
    public class NwSteamHandler
    {
        SteamInterface steamApi;

        public NwSteamHandler(TasClient tas, string webApiKey)
        {
            steamApi = new SteamInterface(GlobalConst.SteamAppID, webApiKey);
            tas.Said += (sender, args) =>
            {
                if (args.Place == TasSayEventArgs.Places.Normal && args.UserName != tas.UserName && args.Text.StartsWith("!linksteam"))
                {
                    var token = args.Text.Substring(11);
                    User user;
                    if (tas.ExistingUsers.TryGetValue(args.UserName, out user))
                    {

                        Utils.StartAsync(() =>
                        {
                            var steamID = steamApi.WebValidateAuthToken(token);
                            var info = steamApi.WebGetPlayerInfo(steamID);
                            
                            using (var db = new ZkDataContext())
                            {
                                var acc = Account.AccountByLobbyID(db, user.LobbyID);
                                acc.SteamID = steamID;
                                acc.SteamName = info.personaname;
                                db.SubmitAndMergeChanges();
                            }


                        });
                    }


                }

            };
        }

    }
}
