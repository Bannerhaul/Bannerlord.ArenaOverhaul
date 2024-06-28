using MCM.Common;

namespace ArenaOverhaul.ModSettings
{
    public class HeroPracticeSettings
    {
        public bool EnableLoadoutChoice { get; set; } = Settings.Instance!.EnableLoadoutChoice;
        public bool OnlyPriorityLoadouts { get; set; } = Settings.Instance!.OnlyPriorityLoadouts;
        public bool PrioritizeExpensiveEquipment { get; set; } = Settings.Instance!.PrioritizeExpensiveEquipment;

        public WeaponPreference FirstPriorityWeapons => FirstPriorityWeaponsDropdown.SelectedValue.EnumValue;
        public WeaponPreference SecondPriorityWeapons => SecondPriorityWeaponsDropdown.SelectedValue.EnumValue;
        public WeaponPreference ThirdPriorityWeapons => ThirdPriorityWeaponsDropdown.SelectedValue.EnumValue;

        internal Dropdown<DropdownEnumItem<WeaponPreference>> FirstPriorityWeaponsDropdown { get; set; } = GetNewDropdown();
        internal Dropdown<DropdownEnumItem<WeaponPreference>> SecondPriorityWeaponsDropdown { get; set; } = GetNewDropdown();
        internal Dropdown<DropdownEnumItem<WeaponPreference>> ThirdPriorityWeaponsDropdown { get; set; } = GetNewDropdown();

        private static Dropdown<DropdownEnumItem<WeaponPreference>> GetNewDropdown() => new(DropdownEnumItem<WeaponPreference>.SetDropdownListFromEnum(), (int) WeaponPreference.None);
    }
}