using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using LobbyClient;
using ZkData;

namespace NightWatch
{
    public class NwSteamHandler
    {
        SteamWebApi steamApi;

        public NwSteamHandler(TasClient tas, string webApiKey)
        {
            steamApi = new SteamWebApi(GlobalConst.SteamAppID, webApiKey);
            tas.Said += (sender, args) =>
            {
                if (args.Place == SayPlace.User && args.UserName != tas.UserName && args.Text.StartsWith("!linksteam"))
                {
                    var token = args.Text.Substring(11);
                    User user;
                    if (tas.ExistingUsers.TryGetValue(args.UserName, out user))
                    {

                        Utils.StartAsync(() =>
                        {
                            Thread.Sleep(2000); // steam is slow to get the ticket from client .. wont verify if its checked too soon
                            var steamID = steamApi.WebValidateAuthToken(token);
                            var info = steamApi.WebGetPlayerInfo(steamID);

                            using (var db = new ZkDataContext())
                            {
                                var acc = db.Accounts.Find(user.AccountID);
                                acc.SteamID = steamID;
                                acc.SteamName = info.personaname;
                                db.SubmitAndMergeChanges();
                                tas.Extensions.PublishAccountData(acc);
                            }
                        });
                    }


                }

            };
        }

    }
}
