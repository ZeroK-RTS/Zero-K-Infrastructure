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
using BattleRect = PlasmaShared.BattleRect;

namespace LobbyClient
{
    public class ScriptGenerator
    {
        /// <summary>
        /// GEnerates script for connecting to game
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string GenerateConnectScript(string host, int port, string userName, string password)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[GAME]");
            sb.AppendLine("{");
            sb.AppendFormat("HostIP={0};\n", host);
            sb.AppendFormat("HostPort={0};\n", port);
            sb.AppendLine("IsHost=0;");
            sb.AppendFormat("MyPlayerName={0};\n", userName);
            sb.AppendFormat("MyPasswd={0};\n", password ?? userName);
            sb.AppendLine("}");
            return sb.ToString();
        }


        /// <summary>
        /// Generates script for hosting a game
        /// </summary>
        public static string GenerateHostScript(BattleContext startContext, SpringBattleStartSetup startSetup, int loopbackListenPort,
                                                string zkSearchTag, string host, int port, string myname = null, string mypassword = null)
        {
            var previousCulture = Thread.CurrentThread.CurrentCulture;
            try {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                var script = new StringBuilder();

                script.AppendLine("[GAME]");
                script.AppendLine("{");

                script.AppendFormat("  ZkSearchTag={0};\n", zkSearchTag);
                script.AppendFormat("  Mapname={0};\n", startContext.Map);

                script.AppendFormat("  StartPosType={0};\n", startContext.IsMission ? 3 : 2);

                script.AppendFormat("  GameType={0};\n", startContext.Mod);
                script.AppendFormat("  ModHash=1;\n");
                script.AppendFormat("  MapHash=1;\n");

                script.AppendFormat("  AutohostPort={0};\n", loopbackListenPort);
                script.AppendLine();
                script.AppendFormat("  HostIP={0};\n", host);
                script.AppendFormat("  HostPort={0};\n", port);
                //script.AppendFormat("  SourcePort={0};\n", 8300);
                script.AppendFormat("  IsHost=1;\n");
                script.AppendLine();

                if (!string.IsNullOrEmpty(myname)) script.AppendFormat("  MyPlayerName={0};\n", myname);
                if (!string.IsNullOrEmpty(mypassword) || !string.IsNullOrEmpty(myname)) script.AppendFormat("  MyPasswd={0};\n", mypassword??myname);

                GeneratePlayerSection(script, startContext, startSetup);

                return script.ToString();
            } finally {
                Thread.CurrentThread.CurrentCulture = previousCulture;
            }
        }

        static void GeneratePlayerSection(StringBuilder script, BattleContext startContext, SpringBattleStartSetup setup)
        {
            // ordinary battle stuff

            var userNum = 0;
            var teamNum = 0;
            var aiNum = 0;

            foreach (var u in startContext.Players) {
                ScriptAddUser(script, userNum, u, teamNum, setup.UserParameters.FirstOrDefault(x => x.LobbyID == u.LobbyID));

                if (!u.IsSpectator) {
                    ScriptAddTeam(script, teamNum, userNum, u.AllyID);
                    teamNum++;
                }

                foreach (var b in startContext.Bots.Where(x => x.Owner == u.Name)) {
                    ScriptAddBot(script, aiNum, teamNum, userNum, b.BotAI, b.BotName);
                    aiNum++;
                    ScriptAddTeam(script, teamNum, userNum, b.AllyID);
                    teamNum++;
                }
                userNum++;
            }

            // ALLIANCES AND START BOXES
            var startboxes = new StringBuilder();
            startboxes.Append("return { ");
            script.AppendLine();
            for (var allyNumber = 0; allyNumber < Spring.MaxAllies; allyNumber++) {
                script.AppendFormat("[ALLYTEAM{0}]\n", allyNumber);
                script.AppendLine("{");
                script.AppendLine("     NumAllies=0;");
                BattleRect rect;
                if (startContext.Rectangles!=null && startContext.Rectangles.TryGetValue(allyNumber, out rect)) {
                    double left = 0, top = 0, right = 1, bottom = 1;
                    rect.ToFractions(out left, out top, out right, out bottom);
                    startboxes.AppendFormat(CultureInfo.InvariantCulture, "[{0}] = ", allyNumber);
                    startboxes.Append("{ ");
                    startboxes.AppendFormat(CultureInfo.InvariantCulture, "{0}, {1}, {2}, {3}", left, top, right, bottom);
                    startboxes.Append(" }, ");
                }
                script.AppendLine("}");
            }

            startboxes.Append("}");
            script.AppendLine();

            script.AppendLine("  [MODOPTIONS]");
            script.AppendLine("  {");

            script.AppendFormat("    startboxes={0};\n", startboxes.ToString());

            var options = new Dictionary<string, string>(startContext.ModOptions);

            // replace/add custom modoptions from startsetup (if they exist)
            if (setup != null && setup.ModOptions != null) foreach (var entry in setup.ModOptions) options[entry.Key] = entry.Value;

            // write final options to script
            foreach (var kvp in options) script.AppendFormat("    {0}={1};\n", kvp.Key, kvp.Value);

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
                                  SpringBattleStartSetup.UserCustomParameters customParameters)
        {
            // PLAYERS
            script.AppendFormat("  [PLAYER{0}]\n", userNum);
            script.AppendLine("  {");
            script.AppendFormat("     Name={0};\n", pteam.Name);
            script.AppendFormat("     Spectator={0};\n", pteam.IsSpectator ? 1 : 0);
            if (!pteam.IsSpectator) script.AppendFormat("     Team={0};\n", teamNum);

            if (pteam.ScriptPassword != null) script.AppendFormat("     Password={0};\n", pteam.ScriptPassword);

            if (customParameters != null) foreach (var kvp in customParameters.Parameters) script.AppendFormat("     {0}={1};\n", kvp.Key, kvp.Value);
            script.AppendLine("  }");
        }
    }
}