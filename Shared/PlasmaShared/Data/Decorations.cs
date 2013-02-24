using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZkData
{
  public enum DecorationIconTypes
  {
    Fixed = 0,
    Faction = 1,
    Clan = 2,
    Avatar = 3,
    Custom = 4,
  }

  public enum DecorationIconPositions
  {
      Overhead = 0,
      Back = 1,
      Chest = 2,
      Shoulders = 3,
  }

  partial class CommanderDecoration
  {
    static string[] iconPositions = { "overhead", "back", "chest", "shoulders" };

    public static string GetIconPosition(CommanderDecorationIcon decoration)
    {
        int num = decoration.IconPosition;
        return iconPositions[num];
    }

    public static string GetIconPosition(Unlock decoration)
    {
        return GetIconPosition(decoration.CommanderDecorationIcon);
    }

    public static string GetIconPosition(CommanderDecoration decoration)
    {
        return GetIconPosition(decoration.Unlock.CommanderDecorationIcon);
    }
  }
}
