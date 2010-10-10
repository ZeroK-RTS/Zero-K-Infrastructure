#region using

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using PlasmaShared.UnitSyncLib;
using Springie.Client;

#endregion

namespace Springie.SpringNamespace
{
    public class ConfigMaker
    {
        public static void Generate(string filename, Battle b, int autoHostPort, out List<Battle.GrPlayer> players)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var s = new StringBuilder();

            s.AppendLine("[GAME]");
            s.AppendLine("{");
            s.AppendFormat("  Mapname={0};\n", b.Map.Name);

            if (b.Details.StartPos == BattleStartPos.Choose) s.AppendFormat("  StartPosType=2;\n");
            else s.AppendFormat("  StartPosType=3;\n"); // hack for random/fixed

            s.AppendFormat("  GameType={0};\n", b.Mod.Name);
            s.AppendFormat("  ModHash={0};\n", (uint)b.Mod.Checksum);
            s.AppendFormat("  MapHash={0};\n", (uint)b.Map.Checksum);
            s.AppendFormat("  AutohostPort={0};\n", autoHostPort);
            s.AppendLine();
            s.AppendFormat("  HostIP={0};\n", "127.0.0.1");
            s.AppendFormat("  HostPort={0};\n", b.HostPort);
            //s.AppendFormat("  MinSpeed={0};\n", 1);
            //s.AppendFormat("  MaxSpeed={0};\n", 1);
            s.AppendLine();
            s.AppendFormat("  MyPlayerNum={0};\n", 0);

            //List<Battle.GrPlayer> players;
            List<Battle.GrTeam> teams;
            List<Battle.GrAlly> alliances;

            b.GroupData(out players, out teams, out alliances);

            var bots = teams.Where(x => x.bot != null).Select(x => x.bot).ToList();
            s.AppendLine();
            s.AppendFormat("  NumPlayers={0};\n", players.Count);
            s.AppendFormat("  NumUsers={0};\n", players.Count + bots.Count);
            s.AppendFormat("  NumTeams={0};\n", teams.Count);
            s.AppendFormat("  NumAllyTeams={0};\n", alliances.Count);
            s.AppendLine();

            // PLAYERS
            for (var i = 0; i < players.Count; ++i)
            {
                var u = players[i].user;
                s.AppendFormat("  [PLAYER{0}]\n", i);
                s.AppendLine("  {");
                s.AppendFormat("     name={0};\n", u.name);

                s.AppendFormat("     Spectator={0};\n", u.IsSpectator ? 1 : 0);
                if (!u.IsSpectator) s.AppendFormat("     team={0};\n", u.TeamNumber);

                User tu;
                if (Program.main.Tas.GetExistingUser(u.name, out tu))
                {
                    s.AppendFormat("     Rank={0};\n", tu.rank);
                    s.AppendFormat("     CountryCode={0};\n", tu.country);
                }
                s.AppendLine("  }");
            }

            // AI's
            for (var i = 0; i < bots.Count; i++)
            {
                var split = bots[i].aiLib.Split('|');
                s.AppendFormat("  [AI{0}]\n", i);
                s.AppendLine("  {");
                s.AppendFormat("    ShortName={0};\n", split[0]);
                s.AppendFormat("    Version={0};\n", split.Length > 1 ? split[1] : "");
                s.AppendFormat("    Team={0};\n", bots[i].TeamNumber);
                s.AppendFormat("    Host={0};\n", players.FindIndex(x => x.user.name == bots[i].owner));
                s.AppendLine("    IsFromDemo=0;");
                s.AppendLine("    [Options]");
                s.AppendLine("    {");
                s.AppendLine("    }");
                s.AppendLine("  }\n");
            }

            var r = new Random();
            var positions = b.Map.Positions;
            var tpos = new List<StartPos>();
            if (b.Details.StartPos == BattleStartPos.Random)
            {
                var org = new List<StartPos>(positions);
                while (org.Count > 0)
                {
                    var t = org[r.Next(org.Count)];
                    org.Remove(t);
                    tpos.Add(t);
                }
            }

            // TEAMS
            s.AppendLine();
            for (var i = 0; i < teams.Count; ++i)
            {
                s.AppendFormat("  [TEAM{0}]\n", i);
                s.AppendLine("  {");
                s.AppendFormat("     TeamLeader={0};\n", teams[i].leader);
                var u = teams[i].bot ?? players[teams[i].leader].user;
                s.AppendFormat("     AllyTeam={0};\n", u.AllyNumber);
                s.AppendFormat("     RGBColor={0:F5} {1:F5} {2:F5};\n",
                               (u.TeamColor & 255)/255.0,
                               ((u.TeamColor >> 8) & 255)/255.0,
                               ((u.TeamColor >> 16) & 255)/255.0);
                s.AppendFormat("     Side={0};\n", b.Mod.Sides[u.Side]);
                s.AppendFormat("     Handicap={0};\n", 0);
                StartPos? pos = null;
                if (b.Details.StartPos == BattleStartPos.Random)
                {
                    if (tpos != null && tpos.Count() > i) pos = tpos.Skip(i).First();
                }
                else if (b.Details.StartPos == BattleStartPos.Fixed) if (positions != null && positions.Length > i) pos = positions[i];
                if (pos != null)
                {
                    s.AppendFormat("      StartPosX={0};\n", pos.Value.x);
                    s.AppendFormat("      StartPosZ={0};\n", pos.Value.z);
                }
                s.AppendLine("  }");
            }

            // ALLYS
            s.AppendLine();
            for (var i = 0; i < alliances.Count; ++i)
            {
                s.AppendFormat("[ALLYTEAM{0}]\n", i);
                s.AppendLine("{");
                s.AppendFormat("     NumAllies={0};\n", 0);
                double left, top, right, bottom;
                alliances[i].rect.ToFractions(out left, out top, out right, out bottom);
                s.AppendFormat("     StartRectLeft={0};\n", left);
                s.AppendFormat("     StartRectTop={0};\n", top);
                s.AppendFormat("     StartRectRight={0};\n", right);
                s.AppendFormat("     StartRectBottom={0};\n", bottom);
                s.AppendLine("}");
            }

            s.AppendLine();
            s.AppendFormat("  NumRestrictions={0};\n", b.DisabledUnits.Count);
            s.AppendLine("  [RESTRICT]");
            s.AppendLine("  {");
            for (var i = 0; i < b.DisabledUnits.Count; ++i)
            {
                s.AppendFormat("    Unit{0}={1};\n", i, b.DisabledUnits[i]);
                s.AppendFormat("    Limit{0}=0;\n", i);
            }
            s.AppendLine("  }");

            s.AppendLine("  [modoptions]");
            s.AppendLine("  {");
            foreach (var o in b.Mod.Options)
            {
                if (o.Type != OptionType.Section)
                {
                    var v = o.Default;
                    if (b.ModOptions.ContainsKey(o.Key)) v = b.ModOptions[o.Key];
                    s.AppendFormat("    {0}={1};\n", o.Key, v);
                }
            }
            s.AppendLine("  }");

            s.AppendLine("}");

            var f = File.CreateText(filename);
            f.Write(s.ToString());
            f.Flush();
            f.Close();
        }
    }
}