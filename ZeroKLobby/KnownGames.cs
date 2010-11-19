using System.Collections.Generic;
using System.Linq;
using ZeroKLobby.MicroLobby;

namespace ZeroKLobby
{
  public static class KnownGames
  {
    public static List<GameInfo> List =
      new List<GameInfo>
      {
        new GameInfo
        {
          Shortcut = "CA",
          FullName = "Complete Annihilation",
          Channel = "ca",
          IsPrimary = true,
          Regex = "Complete Annihilation.*",
          RapidTag = "zk:stable",
        },
        new GameInfo
        { Shortcut = "BA", FullName = "Balanced Annihilation", Channel = "ba", Regex = "Balanced Annihilation.*", RapidTag = "ba:latest", },
        new GameInfo { Shortcut = "NOTA", FullName = "NOTA", Channel = "nota", Regex = "NOTA.*", RapidTag = "nota:latest", },
        new GameInfo { Shortcut = "SA", FullName = "Supreme Annihilation", Channel = "sa", Regex = "Supreme Annihilation.*", RapidTag = "sa:latest", },
        new GameInfo { Shortcut = "S44", FullName = "Spring: 1944", Channel = "s44", Regex = "Spring: 1944.*", RapidTag = "s44:latest", },
        new GameInfo { Shortcut = "Cursed", FullName = "The Cursed", Channel = "cursed", Regex = "The Cursed.*", RapidTag = "thecursed:latest", },
        new GameInfo { Shortcut = "XTA", FullName = "XTA", Channel = "xta", Regex = "XTA.*", RapidTag = "xta:latest", },
        new GameInfo { Shortcut = "evo", FullName = "Evolution RTS", Channel = "evolution", Regex = "Evolution RTS.*", RapidTag = "evo:test", },
        new GameInfo { Shortcut = "KP", FullName = "Kernel Panic", Channel = "kp", Regex = "Kernel.*Panic.*", RapidTag = "kp:stable", },
        new GameInfo { Shortcut = "CT", FullName = "Conflict Terra", Channel = "ct", Regex = "Conflict Terra.*", RapidTag = "ct:stable", },
      }.ToList();


    public static GameInfo GetDefaultGame()
    {
      return List.First(x => x.IsPrimary);
    }
  }
}