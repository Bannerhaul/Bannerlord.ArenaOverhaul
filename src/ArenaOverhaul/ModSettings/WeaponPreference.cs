using System.ComponentModel;

namespace ArenaOverhaul.ModSettings
{
    public enum WeaponPreference : byte
    {
        [Description("{=6SD77Zo1C}None")]
        None = 0,
        [Description("{=7OL4eTdan}One handed weapons")]
        OneHanded = 1,
        [Description("{=rvCVOGi0s}Two handed weapons")]
        TwoHanded = 2,
        [Description("{=bDnJRrfRH}Polearms")]
        Polearm = 3,
        [Description("{=KZt7cDhKn}Bows")]
        Bow = 4,
        [Description("{=s0JLnYAxx}Crossbows")]
        Crossbow = 5,
        [Description("{=CvVDmEhou}Throwing weapons")]
        Throwing = 6,
    }
}