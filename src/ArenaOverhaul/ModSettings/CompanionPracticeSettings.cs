using HarmonyLib.BUTR.Extensions;

using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.FluentBuilder;
using MCM.Common;

using System;
using System.Collections.Generic;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace ArenaOverhaul.ModSettings
{
    internal static class CompanionPracticeSettings
    {
        //Group order does not work if group does not have any properties
        private const string HeadingFamilyMembers = "{=}Family Members";
        private const string HeadingCompanions = "{=}Companions";

        private static Version? CurrentVersion => typeof(CompanionPracticeSettings).Assembly.GetName().Version;

        private static string SettingsId => $"{nameof(CompanionPracticeSettings)}_v{CurrentVersion?.ToString(1)}";

        private static string SettingsName => $"{new TextObject("{=}AO Companion Practice Settings")} {CurrentVersion!.ToString(3)}";

        public static ISettingsBuilder AddCompanionPracticeSettings(Dictionary<Hero, HeroPracticeSettings> heroPracticeSettings)
        {
            var builder = BaseSettingsBuilder.Create(SettingsId, SettingsName)!
                .SetFormat("json2")
                .SetFolderName("AO Companion Practice Settings");

            var clanMembers = Hero.FindAll(x => x.IsAlive && !x.IsChild && x.Clan == Clan.PlayerClan && x != Hero.MainHero).OrderBy(x => x.Name.ToString()).ToList();

            AddClanHeroes(builder, HeadingFamilyMembers, clanMembers.Where(x => !x.IsWanderer).ToList(), 10);
            AddClanHeroes(builder, HeadingCompanions, clanMembers.Where(x => x.IsWanderer).ToList(), 20);

            return builder;

            //Local methods
            void BuildForHero(ISettingsPropertyGroupBuilder builder, Hero hero, int groupOrder)
            {
                HeroPracticeSettings settingContainer;
                if (heroPracticeSettings.TryGetValue(hero, out var practiceSettings))
                {
                    settingContainer = practiceSettings;
                }
                else
                {
                    heroPracticeSettings[hero] = new();
                    settingContainer = heroPracticeSettings[hero];
                }
                builder
                    .AddBool($"{hero.StringId}_enable_loadout_choice", "{=}Enable loadout choice", new PropertyRef(SymbolExtensions2.GetPropertyInfo((HeroPracticeSettings x) => x.EnableLoadoutChoice)!, settingContainer),
                        propBuilder => propBuilder.SetRequireRestart(false).SetOrder(0).SetHintText("{=}When this option is enabled, the corresponding companion hero is allowed to choose weapons for arena practice matches, and you will have to pay the usual fee for this. Otherwise, the companion will use random weapons like everyone else. Default is True."))
                    .AddBool($"{hero.StringId}_prioritize_expensive_equipment", "{=}Prioritize expensive equipment", new PropertyRef(SymbolExtensions2.GetPropertyInfo((HeroPracticeSettings x) => x.PrioritizeExpensiveEquipment)!, settingContainer),
                        propBuilder => propBuilder.SetRequireRestart(false).SetOrder(1).SetHintText("{=}When this option is enabled, the corresponding companion hero will select weapons for arena practice matches, prioritizing better and more expensive equipment. Otherwise, the least expensive set of weapons that meets the given preferences will be selected. Default is False."))
                    .AddDropdown($"{hero.StringId}_weapons_first_priority", "{=}First priority", settingContainer.FirstPriorityWeaponsDropdown.SelectedIndex,
                        new PropertyRef(SymbolExtensions2.GetPropertyInfo((HeroPracticeSettings x) => x.FirstPriorityWeaponsDropdown)!, settingContainer),
                        propBuilder => propBuilder.SetRequireRestart(false).SetOrder(10).SetHintText("{=}Specify the weapon skill that has the highest priority for the corresponding hero companion to train. This companion will always choose a weapon loadout containing a weapon based on that skill, if available. Default is [None]."))
                    .AddDropdown($"{hero.StringId}_weapons_second_priority", "{=}Second priority", settingContainer.SecondPriorityWeaponsDropdown.SelectedIndex,
                        new PropertyRef(SymbolExtensions2.GetPropertyInfo((HeroPracticeSettings x) => x.SecondPriorityWeaponsDropdown)!, settingContainer),
                        propBuilder => propBuilder.SetRequireRestart(false).SetOrder(11).SetHintText("{=}Specify the weapon skill that has the second highest priority for the corresponding hero companion to train. This companion will choose a weapon loadout containing a weapon based on this skill if it is available and there are no higher priority alternatives. Default is [None]."))
                    .AddDropdown($"{hero.StringId}_weapons_third_priority", "{=}Third priority", settingContainer.ThirdPriorityWeaponsDropdown.SelectedIndex,
                        new PropertyRef(SymbolExtensions2.GetPropertyInfo((HeroPracticeSettings x) => x.ThirdPriorityWeaponsDropdown)!, settingContainer),
                        propBuilder => propBuilder.SetRequireRestart(false).SetOrder(12).SetHintText("{=}Specify the weapon skill that has the third highest priority for the corresponding hero companion to train. This companion will choose a weapon loadout containing a weapon based on this skill if it is available and there are no higher priority alternatives. Default is [None]."))
                    .SetGroupOrder(groupOrder);
            }

            void BuildDefaultPreset(ISettingsPresetBuilder builder)
            {
                clanMembers.ForEach(hero =>
                {
                    //var defaultSettingContainer = new HeroPracticeSettings();
                    heroPracticeSettings[hero] = new();
                    var defaultSettingContainer = heroPracticeSettings[hero];
                    builder
                        .SetPropertyValue($"{hero.StringId}_enable_loadout_choice", defaultSettingContainer.EnableLoadoutChoice)
                        .SetPropertyValue($"{hero.StringId}_prioritize_expensive_equipment", defaultSettingContainer.PrioritizeExpensiveEquipment)
                        .SetPropertyValue($"{hero.StringId}_weapons_first_priority", defaultSettingContainer.FirstPriorityWeaponsDropdown)
                        .SetPropertyValue($"{hero.StringId}_weapons_second_priority", defaultSettingContainer.SecondPriorityWeaponsDropdown)
                        .SetPropertyValue($"{hero.StringId}_weapons_third_priority", defaultSettingContainer.ThirdPriorityWeaponsDropdown);
                });
            }

            void AddClanHeroes(ISettingsBuilder builder, string heading, List<Hero> heroes, int groupOrder)
            {
                builder.CreateGroup(heading, gb => gb.SetGroupOrder(groupOrder));
                int i = 0;
                foreach (var hero in heroes)
                {
                    builder.CreateGroup($"{heading}/{hero.Name}", spgb => BuildForHero(spgb, hero, i++));
                }
                builder.CreatePreset(BaseSettings.DefaultPresetId, BaseSettings.DefaultPresetName, BuildDefaultPreset);
            }
        }
    }
}