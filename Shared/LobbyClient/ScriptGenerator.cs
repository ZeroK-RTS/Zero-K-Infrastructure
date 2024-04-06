using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LobbyClient.Legacy;
using PlasmaShared;
using ZkData.UnitSyncLib;

namespace LobbyClient
{
    public class ScriptGenerator
    {
        public const int MaxAllies = 16;

        /// <summary>
        /// GEnerates script for connecting to game
        /// </summary>
        /// <returns></returns>
        public static string GenerateConnectScript(SpringBattleContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[GAME]");
            sb.AppendLine("{");
            sb.AppendFormat("HostIP={0};\n", context.IpAddress);
            sb.AppendFormat("HostPort={0};\n", context.Port);
            sb.AppendLine("IsHost=0;");
            sb.AppendFormat("MyPlayerName={0};\n", context.MyUserName);
            sb.AppendFormat("MyPasswd={0};\n", context.MyPassword ?? context.MyPassword);
            sb.AppendLine("}");
            return sb.ToString();
        }


        /// <summary>
        /// Generates script for hosting a game
        /// </summary>
        public static string GenerateHostScript(SpringBattleContext context, int loopbackListenPort)
        {
            var previousCulture = Thread.CurrentThread.CurrentCulture;
            try {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                var script = new StringBuilder();

                script.AppendLine("[GAME]");
                script.AppendLine("{");

                script.AppendFormat("  ZkSearchTag={0};\n", Guid.NewGuid());
                script.AppendFormat("  Mapname={0};\n", context.LobbyStartContext.Map);

                script.AppendFormat("  StartPosType={0};\n", context.LobbyStartContext.IsMission ? 3 : 2);

                script.AppendFormat("  GameType={0};\n", context.LobbyStartContext.Mod);
                script.AppendFormat("  ModHash=1;\n");
                script.AppendFormat("  MapHash=1;\n");
                
                // send desync to server
                script.AppendFormat("  DumpGameStateOnDesync=1;\n");

                // set the "connecting to: xyz" message during early engine load
                script.AppendFormat("  ShowServerName={0};\n", context.LobbyStartContext.Title.Replace(';', ' '));

                if (loopbackListenPort >0) script.AppendFormat("  AutohostPort={0};\n", loopbackListenPort);
                script.AppendLine();
                script.AppendFormat("  HostIP={0};\n", context.IpAddress);
                script.AppendFormat("  HostPort={0};\n", context.Port);
                //script.AppendFormat("  SourcePort={0};\n", 8300);
                script.AppendFormat("  IsHost=1;\n");
                script.AppendLine();

                if (!string.IsNullOrEmpty(context.MyUserName)) script.AppendFormat("  MyPlayerName={0};\n", context.MyUserName);
                if (!string.IsNullOrEmpty(context.MyPassword) || !string.IsNullOrEmpty(context.MyUserName)) script.AppendFormat("  MyPasswd={0};\n", context.MyPassword??context.MyUserName);

                GeneratePlayerSection(script, context);

                return script.ToString();
            } finally {
                Thread.CurrentThread.CurrentCulture = previousCulture;
            }
        }

        static void GeneratePlayerSection(StringBuilder script, SpringBattleContext setup)
        {
            // ordinary battle stuff

            var userNum = 0;
            var teamNum = 0;
            var aiNum = 0;

            foreach (var u in setup.LobbyStartContext.Players)
            {
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                setup?.LobbyStartContext?.UserParameters.TryGetValue(u.Name, out parameters);

                ScriptAddUser(script, userNum, u, teamNum, parameters);

                if (!u.IsSpectator) {
                    ScriptAddTeam(script, teamNum, userNum, u.AllyID);
                    teamNum++;
                }

                foreach (var b in setup.LobbyStartContext.Bots.Where(x => x.Owner == u.Name)) {
                    ScriptAddBot(script, aiNum, teamNum, userNum, b.BotAI, b.BotName);
                    aiNum++;
                    ScriptAddTeam(script, teamNum, userNum, b.AllyID);
                    teamNum++;
                }
                userNum++;
            }

            // add unowned bots to last player
            foreach (var b in setup.LobbyStartContext.Bots.Where(x => !setup.LobbyStartContext.Players.Any(y=>y.Name == x.Owner)))
            {
                ScriptAddBot(script, aiNum, teamNum, userNum-1, b.BotAI, b.BotName);
                aiNum++;
                ScriptAddTeam(script, teamNum, userNum-1, b.AllyID);
                teamNum++;
            }


            // ALLIANCES AND START BOXES
            var startboxes = new StringBuilder();
            startboxes.Append("return { ");
            script.AppendLine();
            
            /* Fill all slots, not just those that are used (i.e. don't do
             * something like "foreach allyteam in LobbyStartContext...").
             * This is because Spring will squash allyTeamIDs so that they
             * start from 0, which can ruin specific setups on FFA maps.
             * For example if you want to play Mordor vs Gondor on Mearth, 
             * you need to skip allyteam 0, which is Shire, and have players
             * on allyteams 1 and 2; but if there is no "fake" allyteam 0
             * then Spring will squash the 1/2 into 0/1 (which will result
             * in the Mordor team playing as Shire, and Gondor as Mordor). */
            for (var allyNumber = 0; allyNumber < MaxAllies; allyNumber++) {
                script.AppendFormat("[ALLYTEAM{0}]\n", allyNumber);
                script.AppendLine("{");
                script.AppendLine("     NumAllies=0;");
                script.AppendLine("}");
            }

            startboxes.Append("}");
            script.AppendLine();

            script.AppendLine("  [MODOPTIONS]");
            script.AppendLine("  {");

            script.AppendFormat("    startboxes={0};\n", startboxes.ToString());

            // write final options to script
            foreach (var kvp in setup?.LobbyStartContext?.ModOptions) script.AppendFormat("    {0}={1};\n", kvp.Key, kvp.Value);
            
            script.AppendLine("  }");


            // write map options to script
            script.AppendLine("  [MAPOPTIONS]");
            script.AppendLine("  {");
            foreach (var kvp in setup?.LobbyStartContext?.MapOptions) script.AppendFormat("    {0}={1};\n", kvp.Key, kvp.Value);
            script.AppendLine("  }");

            
            script.AppendLine("}");            
        }


        static void ScriptAddBot(StringBuilder script, int aiNum, int teamNum, int userNum, string botAI, string botName)
        {
            // AI
            var split = botAI.Split('|');
            script.AppendFormat("  [AI{0}]\n", aiNum);
            script.AppendLine("  {");
            script.AppendFormat("    Name={0};\n", botName);
            script.AppendFormat("    ShortName={0};\n", split[0]);
            script.AppendFormat("    Version={0};\n", split.Length > 1 ? split[1] : "");
                //having no value is better. Related file: ResolveSkirmishAIKey() at Spring/ExternalAI/IAILibraryManager.cpp 
            script.AppendFormat("    Team={0};\n", teamNum);
            script.AppendFormat("    Host={0};\n", userNum);
            script.AppendLine("    IsFromDemo=0;");
            script.AppendLine("    [Options]");
            script.AppendLine("    {");
            script.AppendLine("    }");
            script.AppendLine("  }\n");
        }

        static void ScriptAddTeam(StringBuilder script, int teamNum, int userNum, int allyID)
        {
            // BOT TEAM
            script.AppendFormat("  [TEAM{0}]\n", teamNum);
            script.AppendLine("  {");
            script.AppendFormat("     TeamLeader={0};\n", userNum);
            script.AppendFormat("     AllyTeam={0};\n", allyID);
            script.AppendFormat("     Handicap={0};\n", 0);
            script.AppendLine("  }");
        }

        static void ScriptAddUser(StringBuilder script, int userNum, PlayerTeam pteam, int teamNum,
                                  Dictionary<string,string> customParameters)
        {
            // PLAYERS
            script.AppendFormat("  [PLAYER{0}]\n", userNum);
            script.AppendLine("  {");
            script.AppendFormat("     Name={0};\n", pteam.Name);
            script.AppendFormat("     Spectator={0};\n", pteam.IsSpectator ? 1 : 0);
            if (!pteam.IsSpectator) script.AppendFormat("     Team={0};\n", teamNum);

            if (pteam.ScriptPassword != null) script.AppendFormat("     Password={0};\n", pteam.ScriptPassword);

            if (customParameters != null) foreach (var kvp in customParameters) script.AppendFormat("     {0}={1};\n", kvp.Key, kvp.Value);
            script.AppendLine("  }");
        }
    }
}
