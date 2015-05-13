using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
          Shortcut = "ZK",
          FullName = "Zero-K",
          Channel = "zk",
          IsPrimary = true,
          Regex = new Regex("Zero-K.*"),
          RapidTag = "zk:stable",
        },
        //new GameInfo
        //{
        //  Shortcut = "dota",
        //  FullName = "Zero-K DOTA",
        //  Channel = "zk",
        //  Regex = new Regex("Zero-K DOTA.*"),
        //  RapidTag = "zkdota:stable",
        //},
        new GameInfo { Shortcut = "S44", FullName = "Spring: 1944", Channel = "s44", Regex = new Regex("Spring: 1944.*"), RapidTag = "s44:stable", },
        new GameInfo { Shortcut = "Cursed", FullName = "The Cursed", Channel = "cursed", Regex = new Regex("The Cursed.*"), RapidTag = "thecursed:latest", },
        new GameInfo { Shortcut = "Evo", FullName = "Evolution RTS", Channel = "evolution", Regex = new Regex("Evolution RTS.*"), RapidTag = "evo:stable", },
        new GameInfo { Shortcut = "SWIW", FullName = "Star Wars: Imperial Winter", Channel = "swiw", Regex = new Regex("Imperial Winter.*"), RapidTag = "swiw:stable", },
        new GameInfo { Shortcut = "TECHA", FullName = "Tech Annihilation", Channel = "techa", Regex = new Regex("Tech Annihilation.*"), RapidTag = "techa:stable", },
        //new GameInfo { Shortcut = "Gundam", FullName = "Gundam", Channel = "gundam", Regex = new Regex("Gundam.*"), RapidTag = "gundam:latest", },
        new GameInfo { Shortcut = "KP", FullName = "Kernel Panic", Channel = "kp", Regex = new Regex("Kernel.*Panic.*"), RapidTag = "kp:stable", },
        new GameInfo { Shortcut = "OTE", FullName = "On The Edge", Channel = "ote", Regex = new Regex("OTE.*"), RapidTag = "ote:stable", },
        new GameInfo { Shortcut = "NOTA", FullName = "NOTA", Channel = "nota", Regex = new Regex("NOTA.*"), RapidTag = "nota:latest", },
        new GameInfo { Shortcut = "XTA", FullName = "XTA", Channel = "xta", Regex = new Regex("XTA.*"), RapidTag = "xta:stable", },
        new GameInfo { Shortcut = "BA", FullName = "Balanced Annihilation", Channel = "ba", Regex = new Regex("Balanced Annihilation.*"), RapidTag = "ba:stable", },
        new GameInfo { Shortcut = "CT", FullName = "Conflict Terra", Channel = "ct", Regex = new Regex("Conflict Terra.*"), RapidTag = "ct:stable", },
        //new GameInfo { Shortcut = "SA", FullName = "Supreme Annihilation", Channel = "sa", Regex = new Regex("Supreme Annihilation.*"), RapidTag = "sa:latest", },
      }.ToList();

    


    public static GameInfo GetDefaultGame()
    {
      return List.First(x => x.IsPrimary);
    }

    public static GameInfo GetGame(string modName)
    {
      return List.FirstOrDefault(x => x.Regex.IsMatch(modName));
    }
  }
}