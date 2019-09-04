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
        public string GetNightwatchPassword() => MiscVar.GetValue("NightwatchPassword");
        public string GetSteamWebApiKey() => MiscVar.GetValue("SteamWebApiKey");
        public string GetGithubHookKey() => MiscVar.GetValue("GithubHookKey");
        public string GetSteamBuildPassword() => MiscVar.GetValue("SteamBuildPassword");
        public string GetGlacierSecretKey() => MiscVar.GetValue("GlacierSecretKey");

        public string GetNightwatchDiscordToken() => MiscVar.GetValue("NightwatchDiscordToken");
    }
}
