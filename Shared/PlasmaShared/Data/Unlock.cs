using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZkData
{
  partial class Unlock
  {
      [NotMapped]
    public string LabelColor
    {
      get
      {
        switch (UnlockType)
        {
          case UnlockTypes.Chassis:
            return "#00FF00";
          case UnlockTypes.Module:
            return "#00FFFF";
          case UnlockTypes.Unit:
            return "#FFFF00";
          case UnlockTypes.Weapon:
            return "#FF0000";
          case UnlockTypes.Decoration:
            return "#FF00FF";
        }
        return "#FFFFFF";
      }
    }

      [NotMapped]
    public string ImageUrl
    {
      get { return string.Format("http://zero-k.googlecode.com/svn/trunk/mods/zk/unitpics/{0}.png", Code); }
    }
  }
}