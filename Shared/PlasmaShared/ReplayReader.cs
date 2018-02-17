using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using ZkData;

namespace PlasmaShared
{
    public class ReplayReader
    {
        /// <summary>
        /// Procesed info about replay
        /// </summary>
        public class ReplayInfo
        {
            public string Engine { get; set; }
            public string Game { get; set; }
            public string Map { get; set; }
            public string StartScript { get; set; }
            public DateTime Date { get; set; }
            public int GameLengthRealtime { get; set; }
            public int GameLengthIngameTime { get; set; }
            public string GameID { get; set; }
            public List<PlayerEntry> Players { get; set; } = new List<PlayerEntry>();

            public class PlayerEntry
            {
                public bool IsSpectator;
                public bool IsBot;
                public string Name;
                public int? AllyTeam;
            }
        }


        /// <summary>
        /// Actual demo header as it appears in the demo file
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        struct DemoFileHeader
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string magic; // DEMOFILE_MAGIC
            public int version; // DEMOFILE_VERSION
            public int headerSize; // Size of the DemoFileHeader, minor version number.
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string versionString; // Spring version string, e.g. "0.75b2", "0.75b2+svn4123"
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] gameID; // Unique game identifier. Identical for each player of the game.
            public UInt64 unixTime; // Unix time when game was started.
            public int scriptSize; // Size of startscript.
            public int demoStreamSize; // Size of the demo stream.
            public int gameTime; // Total number of seconds game time.
            public int wallclockTime; // Total number of seconds wallclock time.
            public int numPlayers; // Number of players for which stats are saved. (this contains also later joined spectators!)
            public int playerStatSize; // Size of the entire player statistics chunk.
            public int playerStatElemSize; //< sizeof(CPlayer::Statistics)
            public int numTeams; // Number of teams for which stats are saved.
            public int teamStatSize; // Size of the entire team statistics chunk.
            public int teamStatElemSize; // sizeof(CTeam::Statistics)
            public int teamStatPeriod; // Interval (in seconds) between team stats.
            public int winningAllyTeamsSize; // The size of the vector of the winning ally teams
        }

        public ReplayInfo ReadReplayInfo(string path)
        {
            Stream stream;
            using (var fs = File.OpenRead(path))
            {
                // decompress if needed
                if (path.ToLower().EndsWith("sdfz")) stream = new GZipStream(fs, CompressionMode.Decompress);
                else stream = fs;

                var br = new BinaryReader(stream);
                var header = br.ReadStruct<DemoFileHeader>();

                var script = Encoding.UTF8.GetString(br.ReadBytes(header.scriptSize));
                var chunks = ScriptChunk.ChunkifyScript(script);

                var mapName = Regex.Match(script, "mapname=([^;]+)", RegexOptions.IgnoreCase).Groups[1].Value;
                var gameName = Regex.Match(script, "gametype=([^;]+)", RegexOptions.IgnoreCase).Groups[1].Value;


                List<ReplayInfo.PlayerEntry> players = new List<ReplayInfo.PlayerEntry>();
                foreach (var playerChunk in chunks.Where(x => x.Type == "player" || x.Type=="ai"))
                {
                    players.Add(new ReplayInfo.PlayerEntry()
                    {
                        Name = playerChunk["name"],
                        AllyTeam = chunks.Where(x=>x.Type =="team" && x.Id.ToString()== playerChunk["team"]).Select(x=> x["allyteam"]).FirstOrDefault().ToInt(),
                        IsSpectator = playerChunk["spectator"]=="1",
                        IsBot = playerChunk.Type=="ai"
                    });
                }
                
                var ret = new ReplayInfo()
                {
                    Engine = header.versionString,
                    Game = gameName,
                    Map = mapName,
                    StartScript = script,
                    Date = header.unixTime.UnixToDateTime(),
                    GameLengthRealtime = header.wallclockTime,
                    GameLengthIngameTime = header.gameTime,
                    GameID = header.gameID.ToHex(),
                    Players = players
                };
                return ret;

            }
        }

        


        public class ScriptChunk
        {
            public string Type;
            public int Id;
            public Dictionary<string,string> Values = new Dictionary<string, string>();

            public string this[string key] => Values?.Get(key);
            

            public ScriptChunk(List<string> scriptPart)
            {
                var match = Regex.Match(scriptPart[0], "\\[([^0-9]+)([0-9]*)\\]");
                Type = match.Groups[1].Value.ToLower();
                int.TryParse(match.Groups[2].Value ?? "", out Id);
                
                foreach (var line in scriptPart.Skip(1))
                {
                    var m = Regex.Match(line, "([^=]+)=([^;]+);");
                    if (m.Success)
                    {
                        Values[m.Groups[1].Value.ToLower().Trim()] = m.Groups[2].Value;
                    }
                }
            }

            public static List<ScriptChunk> ChunkifyScript(string script)
            {
                List<List<string>> chunks = new List<List<string>>();

                // chunkify
                List<string> chunkLines = new List<string>();
                foreach (var line in script.Split('\n').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)))
                {
                    if (line.ToLower().StartsWith("["))
                    {
                        if (chunkLines.Any())
                        {
                            chunks.Add(chunkLines);
                            chunkLines = new List<string>();
                        }
                    }
                    chunkLines.Add(line);
                }

                return chunks.Select(x => new ScriptChunk(x)).ToList();
            }

        }





    }
}
