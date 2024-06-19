using System.ComponentModel;

namespace ArenaOverhaul.ModSettings
{
    public enum WeaponPreference : byte
    {
        [Description("{=}None")]
        None = 0,
        [Description("{=}One handed weapons")]
        OneHanded = 1,
        [Description("{=}Two handed weapons")]
        TwoHanded = 2,
        [Description("{=}Polearms")]
        Polearm = 3,
        [Description("{=}Bows")]
        Bow = 4,
        [Description("{=}Crossbows")]
        Crossbow = 5,
        [Description("{=}Throwing weapons")]
        Throwing = 6,
    }
}