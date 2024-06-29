using System.ComponentModel;

namespace ArenaOverhaul.ModSettings
{
    public enum PracticeEquipmentType : byte
    {
        [Description("{=z6lpZ65aK}Practice clothes")]
        PracticeClothes = 0,
        [Description("{=o5AcCZgOn}Tournament armor")]
        TournamentArmor = 1,
        [Description("{=sBnb12g7c}Civilian equipment")]
        CivilianEquipment = 2,
        [Description("{=kKvvYWhMH}Battle equipment")]
        BattleEquipment = 3
    }
}