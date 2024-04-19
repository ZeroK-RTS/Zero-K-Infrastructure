using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using GameAnalyticsSDK.Net;
using Octokit;
using PlasmaShared;
using ZkData;

namespace ChobbyLauncher
{
    public enum CrashType {
        Desync,
        Crash,
        LuaError,
        UserReport
    };

    public static class CrashReportHelper
    {
        private const string CrashReportsRepoOwner = "ZeroK-RTS";
        private const string CrashReportsRepoName = "CrashReports";

        private const int MaxInfologSize = 62000;
        private const string InfoLogLineStartPattern = @"(^\[t=\d+:\d+:\d+\.\d+\]\[f=-?\d+\] )";
        private const string InfoLogLineEndPattern = @"(\r?\n|\Z)";
        private sealed class GameFromLog
        {
            public int StartIdxInLog { get; set; }
            public string GameID { get; set; }
            public bool HasDesync { get => FirstDesyncIdxInLog.HasValue; }
            public int? FirstDesyncIdxInLog { get; set; }
            public List<string> GameStateFileNames { get; set; }

            //Perhaps in future versions, these could be added?
            //PlayerName
            //DemoFileName
            //MapName
            //ModName
        }

        private sealed class GameFromLogCollection
        {
            public IReadOnlyList<GameFromLog> Games { get; private set; }
            public GameFromLogCollection(IEnumerable<int> startIndexes)
            {
                Games = startIndexes.Select(idx => new GameFromLog { StartIdxInLog = idx }).ToArray();
            }

            private readonly struct GameStartList : IReadOnlyList<int>
            {
                private readonly GameFromLogCollection _this;
                public GameStartList(GameFromLogCollection v) => _this = v;

                int IReadOnlyList<int>.this[int index] => _this.Games[index].StartIdxInLog;

                int IReadOnlyCollection<int>.Count { get => _this.Games.Count; }

                IEnumerator<int> IEnumerable<int>.GetEnumerator() => _this.Games.Select(g => g.StartIdxInLog).GetEnumerator();

                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => ((IEnumerable<int>)this).GetEnumerator();
            }
            public IReadOnlyList<int> AsGameStartReadOnlyList() => new GameStartList(this);

            private GameFromLog GetGameForIndex(int idx)
            {
                //Equivalent to:
                //return Games.LastOrDefault(g => g.StartIdxInLog < idx);
                //but takes advantage of the fact that Games is sorted to have log rather than linear runtime.
                var lb = AsGameStartReadOnlyList().LowerBoundIndex(idx);
                return lb == 0 ? null : Games[lb - 1];
            }
            public void AddGameStateFileNames(IEnumerable<(int, string)> gameStateFileNames)
            {
                foreach (var file in gameStateFileNames)
                {
                    var game = GetGameForIndex(file.Item1);
                    if (game == null)
                    {
                        Trace.TraceWarning("[CrashReportHelper] Unable to match GameState file to Game");
                        continue;
                    }
                    if (game.GameStateFileNames == null)
                    {
                        game.GameStateFileNames = new List<string>();
                    }

                    game.GameStateFileNames.Add(file.Item2);
                }
            }
            public void AddDesyncs(IEnumerable<int> desyncs)
            {
                foreach (var desync in desyncs)
                {
                    var game = GetGameForIndex(desync);
                    if (game == null)
                    {
                        Trace.TraceWarning("[CrashReportHelper] Unable to match Desync to Game");
                        continue;
                    }
                    if (!game.HasDesync)
                    {
                        game.FirstDesyncIdxInLog = desync;
                    }
                }
            }
            public void AddGameIDs(IEnumerable<(int, string)> gameIDs)
            {
                foreach (var gameID in gameIDs)
                {
                    var game = GetGameForIndex(gameID.Item1);
                    if (game == null)
                    {
                        Trace.TraceWarning("[CrashReportHelper] Unable to match GameID to Game");
                        continue;
                    }
                    if (game.GameID != null)
                    {
                        Trace.TraceWarning("[CrashReportHelper] Found multiple GameIDs for Game");
                        continue;
                    }

                    game.GameID = gameID.Item2;
                }
            }
        }
        private static IReadOnlyList<TextTruncator.RegionOfInterest> MakeRegionsOfInterest(int stringLength, IEnumerable<int> pointsOfInterest, IReadOnlyList<int> regionBoundaries)
        {
            //Pre:
            //  pointsOfInterest is sorted
            //  regionBoundaries is sorted

            //Automatically adds a region at the start and end of the string, that can expand to cover the whole string.
            //For every element of pointsOfInterest, adds a region with StartLimit/EndLimit corresponding to the relevant regionBoundaries (or the start/end of the string)

            var result = new List<TextTruncator.RegionOfInterest>();

            result.Add(new TextTruncator.RegionOfInterest { PointOfInterest = 0, StartLimit = 0, EndLimit = stringLength });
            result.AddRange(pointsOfInterest.Select(poi => {
                var regionEndIndex = Utils.LowerBoundIndex(regionBoundaries, poi);
                return new TextTruncator.RegionOfInterest
                {
                    PointOfInterest = poi,
                    StartLimit = regionEndIndex == 0 ? 0 : regionBoundaries[regionEndIndex - 1],
                    EndLimit = regionEndIndex == regionBoundaries.Count ? stringLength : regionBoundaries[regionEndIndex],
                };
            }));
            result.Add(new TextTruncator.RegionOfInterest { PointOfInterest = stringLength, StartLimit = 0, EndLimit = stringLength });

            return result;
        }

        private static string EscapeMarkdownTableCell(string str) => str.Replace("\r", "").Replace("\n", " ").Replace("|", @"\|");
        private static string MakeDesyncGameTable(GameFromLogCollection gamesFromLog)
        {
            var tableEmpty = true;
            var sb = new StringBuilder();
            sb.AppendLine("\n\n|GameID|GameState File|");
            sb.AppendLine("|-|-|");

            foreach (var game in gamesFromLog.Games.Where(g => g.HasDesync && g.GameStateFileNames != null))
            {
                var gameIDString = EscapeMarkdownTableCell(game.GameID ?? "Unknown");
                foreach (var gameStateFileName in game.GameStateFileNames)
                {
                    tableEmpty = false;
                    sb.AppendLine($"|{gameIDString}|{EscapeMarkdownTableCell(gameStateFileName)}|");
                }
            }
            return tableEmpty ? string.Empty : sb.ToString();
        }

        private static async Task<Issue> ReportCrash(string infolog, CrashType type, string engine, string bugReportTitle, string bugReportDescription, GameFromLogCollection gamesFromLog)
        {
            try
            {
                var client = new GitHubClient(new ProductHeaderValue("chobbyla"))
                {
                    Credentials = new Credentials(GlobalConst.CrashReportGithubToken)
                };

                var infologTruncated = TextTruncator.Truncate(infolog, MaxInfologSize, MakeRegionsOfInterest(infolog.Length, gamesFromLog.Games.Where(g => g.HasDesync).Select(g => g.FirstDesyncIdxInLog.Value), gamesFromLog.AsGameStartReadOnlyList()));

                var desyncDebugInfo = MakeDesyncGameTable(gamesFromLog);

                var newIssueRequest = new NewIssue($"Spring {type} [{engine}] {bugReportTitle}")
                {
                    Body = $"{bugReportDescription}{desyncDebugInfo}"
                };
                var createdIssue = await client.Issue.Create(CrashReportsRepoOwner, CrashReportsRepoName, newIssueRequest);

                await client.Issue.Comment.Create(CrashReportsRepoOwner, CrashReportsRepoName, createdIssue.Number, $"infolog_full.txt (truncated):\n\n```{infologTruncated}```");

                return createdIssue;
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("[CrashReportHelper] Problem reporting a bug: {0}", ex);
            }
            return null;
        }

        //All infolog parsing is best-effort only.
        //The infolog file format does not have enough structure to guarantee that it is never misinterpreted.

        private static int[] ReadGameReloads(string logStr)
        {
            //[t=00:00:30.569367][f=-000001] [ReloadOrRestart] Spring "E:\Games\SteamLibrary\steamapps\common\Zero-K\engine\win64\105.1.1-2457-g8095d30\spring.exe" should be reloading
            //This happens whenever a new game is started, or when a game is exited and returns to lobby.

            try
            {
                return
                    Regex
                        .Matches(
                            logStr,
                            @"\[ReloadOrRestart\](?<=(?<s>^)\[t=\d+:\d+:\d+\.\d+\]\[f=-?\d+\] \[ReloadOrRestart\])",
                            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
                            TimeSpan.FromSeconds(30))
                        .Cast<Match>().Select(m => m.Groups["s"].Index)
                        .ToArray();
            }
            catch (RegexMatchTimeoutException)
            {
                Trace.TraceError("\"[CrashReportHelper] RegexMatchTimeoutException in ReadGameReloads");
                return Array.Empty<int>();
            }
        }
        private static (int, string)[] ReadGameStateFileNames(string logStr)
        {
            //See https://github.com/beyond-all-reason/spring/blob/f3ba23635e1462ae2084f10bf9ba777467d16090/rts/System/Sync/DumpState.cpp#L155

            //[t=00:22:43.353840][f=0003461] [DumpState] using dump-file "ClientGameState--749245531-[3461-3461].txt"
            try
            {
                return
                    Regex
                        .Matches(
                            logStr,
                            $@"(?<={InfoLogLineStartPattern})\[DumpState\] using dump-file ""(?<d>[^{Regex.Escape(System.IO.Path.DirectorySeparatorChar.ToString())}{Regex.Escape(System.IO.Path.AltDirectorySeparatorChar.ToString())}""]+)""{InfoLogLineEndPattern}",
                            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
                            TimeSpan.FromSeconds(30))
                        .Cast<Match>().Select(m => (m.Index, m.Groups["d"].Value)).Distinct()
                        .ToArray();
            }
            catch (RegexMatchTimeoutException)
            {
                Trace.TraceError("\"[CrashReportHelper] RegexMatchTimeoutException in ReadClientStateFileNames");
                return Array.Empty<(int, string)>();
            }
        }

        private static int[] ReadDesyncs(string logStr)
        {
            //[t=00:22:43.533864][f=0003461] Sync error for mankarse in frame 3451 (got 927a6f33, correct is 6b550dd1)

            //See ZkData.Account.IsValidLobbyName
            var accountNamePattern = @"[_[\]a-zA-Z0-9]{1,25}";
            try
            {
                return
                    Regex
                        .Matches(
                            logStr,
                            $@"Sync error for(?<={InfoLogLineStartPattern}Sync error for) {accountNamePattern} in frame \d+ \(got [a-z0-9]+, correct is [a-z0-9]+\){InfoLogLineEndPattern}",
                            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
                            TimeSpan.FromSeconds(30))
                        .Cast<Match>().Select(m => m.Index).ToArray();
            }
            catch (RegexMatchTimeoutException)
            {
                Trace.TraceError("\"[CrashReportHelper] RegexMatchTimeoutException in ReadDesyncs");
                return Array.Empty<int>();
            }
        }


        private static (int, string)[] ReadGameIDs(string logStr)
        {
            //[t=00:19:00.246149][f=-000001] GameID: 6065f665e92c7942def2c0c17c703e72

            try
            {
                return
                    Regex
                        .Matches(
                            logStr,
                            $@"GameID:(?<={InfoLogLineStartPattern}GameID:) (?<g>[0-9a-zA-Z]+){InfoLogLineEndPattern}",
                            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline,
                            TimeSpan.FromSeconds(30))
                        .Cast<Match>().Select(m => { var g = m.Groups["g"]; return (g.Index, g.Value); }).ToArray();
            }
            catch (RegexMatchTimeoutException)
            {
                Trace.TraceError("\"[CrashReportHelper] RegexMatchTimeoutException in ReadGameIDs");
                return Array.Empty<(int, string)>();
            }
        }

        public static void CheckAndReportErrors(string logStr, bool springRunOk, string bugReportTitle, string bugReportDescription, string engineVersion)
        {
            var gamesFromLog = new GameFromLogCollection(ReadGameReloads(logStr));

            gamesFromLog.AddGameStateFileNames(ReadGameStateFileNames(logStr));
            gamesFromLog.AddDesyncs(ReadDesyncs(logStr));
            gamesFromLog.AddGameIDs(ReadGameIDs(logStr));

            var syncError = gamesFromLog.Games.Any(g => g.HasDesync);
            if (syncError) Trace.TraceWarning("Sync error detected");

            var openGlFail = logStr.Contains("No OpenGL drivers installed.") ||
                             logStr.Contains("This stack trace indicates a problem with your graphic card driver") ||
                             logStr.Contains("Please go to your GPU vendor's website and download their drivers.") ||
                             logStr.Contains("minimum required OpenGL version not supported, aborting") ||
                             logStr.Contains("Update your graphic-card driver!");

            if (openGlFail)
            {
                Trace.TraceWarning("Outdated OpenGL detected");

                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    if (MessageBox.Show(
                        "You have outdated graphics card drivers!\r\nPlease try finding ones for your graphics card and updating them. \r\n\r\nWould you like to see our Linux graphics driver guide?",
                        "Outdated graphics card driver detected",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        Utils.OpenUrl("http://zero-k.info/mediawiki/index.php?title=Optimized_Graphics_Linux");
                    }
                }
                else
                {
                    MessageBox.Show("You have outdated graphics card drivers!\r\nPlease try finding ones for your graphics card and updating them.",
                        "Outdated graphics card driver detected",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }

            var luaErr = logStr.Contains("LUA_ERRRUN");
            bool crashOccured = (!springRunOk && !openGlFail) || syncError || luaErr;
            bool isUserReport = !string.IsNullOrEmpty(bugReportTitle);

            if (crashOccured || isUserReport)
            {
                /* Don't make a popup for user reports since the user already agreed by clicking the report button earlier.
                 * NB: benchmarks via Chobby also work by creating a user report. */
                if (isUserReport || MessageBox.Show("We would like to send crash/desync data to Zero-K repository, it can contain chatlogs. Do you agree?",
                    "Automated crash report",
                    MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    var crashType = isUserReport ? CrashType.UserReport
                                  : syncError    ? CrashType.Desync
                                  : luaErr       ? CrashType.LuaError
                                                 : CrashType.Crash;

                    var ret =
                        ReportCrash(
                            logStr,
                            crashType,
                            engineVersion,
                            bugReportTitle,
                            bugReportDescription,
                            gamesFromLog)
                        .GetAwaiter().GetResult();

                    if (ret != null)
                        try
                        {
                            Utils.OpenUrl(ret.HtmlUrl);
                        }
                        catch { }
                }

                try
                {
                    GameAnalytics.AddErrorEvent(EGAErrorSeverity.Critical, "Spring crash");
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error adding GA error event: {0}", ex);
                }
            }
        }
    }
}
