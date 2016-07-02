using System.ComponentModel;

namespace ZkData
{
  public enum  UnlockTypes
  {
    Unit=0,
    Chassis = 1,
    Weapon = 2,
    Module = 3,
    Decoration = 4,
    [Description("Man. Wep")]
    WeaponManualFire = 5,
    [Description("Flex Wep")]
    WeaponBoth = 6,
  }
}
