using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SpringDownloader
{
  public class SpringScriptWatcher
  {
    private const string scriptName = "script.txt";

    public SpringScriptWatcher(string springPath)
    {
      var watch = new FileSystemWatcher(springPath, scriptName) {EnableRaisingEvents = true};
      watch.Changed += watch_Changed;
    }

    public event EventHandler<SpringConfigWatcherEventArgs> ScriptChanged;

    private void InvokeScriptChanged(SpringConfigWatcherEventArgs e)
    {
      var scriptChangedHandler = ScriptChanged;
      if (scriptChangedHandler != null) scriptChangedHandler(this, e);
    }

    private void watch_Changed(object sender, FileSystemEventArgs e)
    {
      if (Utils.CanWrite(e.FullPath)) {
        try {
          string text = File.ReadAllText(e.FullPath);
          var si = ParseConfig(text);
          InvokeScriptChanged(new SpringConfigWatcherEventArgs(si));
        } catch (Exception ex) {
          Program.NotifyError(ex, "Error parsing spring script");
        }
      }
    }

    private static ScriptInfo ParseConfig(string text)
    {
      var si = new ScriptInfo();
      var lines = text.Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
      var mode = ParseMode.None;
      var players = new List<PlayerInfo>();
      int lastTeam = -1;

      foreach (var s in lines) {
        string line = s.Trim();
        if (line.StartsWith("[player", StringComparison.InvariantCultureIgnoreCase)) {
          mode = ParseMode.Player;
          var m = Regex.Match(line, "\\[player([0-9]+)", RegexOptions.IgnoreCase);
          if (m.Success) {
            int id = int.Parse(m.Groups[1].Value);
            var pi = new PlayerInfo(id);
            players.Add(pi);
          } else throw new ApplicationException("Error parsing " + scriptName);
        } else if (line.StartsWith("[team", StringComparison.InvariantCultureIgnoreCase)) {
          mode = ParseMode.Team;
          var m = Regex.Match(line, "\\[team([0-9]+)", RegexOptions.IgnoreCase);
          if (m.Success) lastTeam = int.Parse(m.Groups[1].Value);
          else throw new ApplicationException("Error parsing " + scriptName);
        } else {
          var m = Regex.Match(line, "([^=]+)=([^;]+)");
          if (m.Success) {
            string var = m.Groups[1].Value;
            string val = m.Groups[2].Value;
            if (mode == ParseMode.Player) {
              if (String.Compare(var, "team", true) == 0) players[players.Count - 1].Team = int.Parse(val);
              else if (String.Compare(var, "spectator", true) == 0) players[players.Count - 1].IsSpectator = (val == "1");
              else if (String.Compare(var, "name", true) == 0) {
                players[players.Count - 1].Name = val;
                if (players[players.Count - 1].Id == 0) si.Host = val;
              }
            } else if (mode == ParseMode.Team) if (String.Compare(var, "allyteam", true) == 0) for (int i = 0; i < players.Count; ++i) if (players[i].Team == lastTeam) players[i].Ally = int.Parse(val);
          }
        }
      }

      foreach (var pi in players) {
        if (!pi.IsSpectator) {
          var pp = new ScriptInfo.PlayerPair(pi.Ally, pi.Name);
          si.Players.Add(pp);
        }
      }
      return si;
    }

    #region Nested type: ParseMode

    private enum ParseMode
    {
      Player,
      Team,
      None
    } ;

    #endregion

    #region Nested type: PlayerInfo

    private class PlayerInfo
    {
      public readonly int Id = -1;
      public int Ally = -1;
      public bool IsSpectator;
      public string Name = "";
      public int Team = -1;

      public PlayerInfo(int id)
      {
        Id = id;
      }
    }

    #endregion

    #region Nested type: ScriptInfo

    public class ScriptInfo
    {
      public string Host;

      public List<PlayerPair> Players = new List<PlayerPair>();

      /// <summary>
      /// Gets parameters formated for p2p coordinator
      /// </summary>
      /// <returns></returns>
      public string[] GetParameterArray()
      {
        var pars = new List<string> {Host};
        foreach (var pair in Players) {
          pars.Add(pair.Ally.ToString());
          pars.Add(pair.Name);
        }
        return pars.ToArray();
      }

      #region Nested type: PlayerPair

      public class PlayerPair
      {
        public int Ally;
        public string Name;

        public PlayerPair(int ally, string name)
        {
          Ally = ally;
          Name = name;
        }
      }

      #endregion
    }

    #endregion

    #region Nested type: SpringConfigWatcherEventArgs

    public class SpringConfigWatcherEventArgs : EventArgs
    {
      public ScriptInfo ScriptInfo;

      public SpringConfigWatcherEventArgs(ScriptInfo scriptInfo)
      {
        ScriptInfo = scriptInfo;
      }
    }

    #endregion
  }
}