using System;
using System.Diagnostics;
using System.Linq;
using LobbyClient;
using ZkData;
using ZeroKWeb;

namespace ZkData
{
    ///  <summary>
    ///      Used to verify user accounts; also sends lobby messages via Nightwatch
    ///  </summary>
    public class AuthServiceClient
    {
        //static IAuthService channel;

        static AuthServiceClient() {}

        public static Account VerifyAccountHashed(string login, string passwordHash)
        {
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(passwordHash)) return null;
            var db = new ZkDataContext();
            var acc = Account.AccountVerify(db, login, passwordHash);
            return acc;
        }

        public static Account VerifyAccountPlain(string login, string password)
        {
            return VerifyAccountHashed(login, Utils.HashLobbyPassword(password));
        }
    }
}