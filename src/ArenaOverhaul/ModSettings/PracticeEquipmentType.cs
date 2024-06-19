using System.ComponentModel;

namespace ArenaOverhaul.ModSettings
{
    public enum PracticeEquipmentType : byte
    {
        [Description("{=}Practice clothes")]
        PracticeClothes = 0,
        [Description("{=}Tournament armor")]
        TournamentArmor = 1,
        [Description("{=}Civilian equipment")]
        CivilianEquipment = 2,
        [Description("{=}Battle equipment")]
        BattleEquipment = 3
    }
}