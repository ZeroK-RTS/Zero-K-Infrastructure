using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ZkData
{
    public class Secrets
    {
        public string GetNightwatchPassword(ZkDataContext db = null) => MiscVar.GetValue("NightwatchPassword");
        public string GetSteamWebApiKey(ZkDataContext db = null) => MiscVar.GetValue("SteamWebApiKey");
        public string GetGithubHookKey(ZkDataContext db = null) => MiscVar.GetValue("GithubHookKey");
        public string GetSteamBuildPassword(ZkDataContext db = null) => MiscVar.GetValue("SteamBuildPassword");
        public string GetGlacierSecretKey(ZkDataContext db = null) => MiscVar.GetValue("GlacierSecretKey");
    }
}
