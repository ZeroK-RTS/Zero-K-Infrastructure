#region using

using System;
using System.Collections.Generic;
using System.ComponentModel;

#endregion

namespace LobbyClient
{
    public enum BattleStartPos
    {
        Fixed = 0,
        Random = 1,
        Choose = 2
    }

    public enum BattleEndCondition
    {
        Continues = 0,
        Ends = 1,
        Lineage = 2
    }

    public class BattleDetails: ICloneable
    {
        BattleStartPos startPos = BattleStartPos.Choose;

        public static BattleDetails Default = new BattleDetails();

        [Category("Game rules")]
        [Description("Starting position")]
        public BattleStartPos StartPos { get { return startPos; } set { startPos = value; } }

        public string GetParamList()
        {
            return string.Format("GAME/hosttype=SPRINGIE\tGAME/StartPosType={0}\t", (int)startPos);
        }

        /// <summary>
        /// parses itself from source tags
        /// </summary>
        /// <param Name="source"></param>
        public void Parse(string source, IDictionary<string, string> modOptions)
        {
            foreach (var pair in source.Split('\t'))
            {
                var arg = pair.Split(new[] { '=' }, 2);
                if (arg.Length == 2)
                {
                    switch (arg[0])
                    {
                        case "game/startpostype":
                            StartPos = (BattleStartPos)int.Parse(arg[1]);
                            break;
                        default:
                            if (arg[0].ToLower().StartsWith("game/modoptions/") || arg[0].ToLower().StartsWith("game\\modoptions\\"))
                            {
                                var val = arg[0].Substring(16);
                                if (modOptions.ContainsKey(val)) modOptions[val] = arg[1];
                                else modOptions.Add(val, arg[1]);
                            }
                            break;
                    }
                }
            }
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    } ;
}