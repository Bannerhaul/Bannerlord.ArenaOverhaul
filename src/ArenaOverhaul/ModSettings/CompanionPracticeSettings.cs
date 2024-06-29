using ArenaOverhaul.CampaignBehaviors.BehaviorManagers;

using HarmonyLib.BUTR.Extensions;

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
        private const string HeadingClanLeader = "{=NcXn6Ow7O}1. Clan Leader";
        private const string HeadingFamilyMembers = "{=NowOsng0A}2. Family Members";
        private const string HeadingCompanions = "{=vTyduFjuF}3. Companions";

        private static Version? CurrentVersion => typeof(CompanionPracticeSettings).Assembly.GetName().Version;

        private static string SettingsId => $"{nameof(CompanionPracticeSettings)}_v{CurrentVersion?.ToString(1)}";

        private static string SettingsName => $"{new TextObject("{=fFlUJjUl0}AO Hero Practice Settings")} {CurrentVersion!.ToString(3)}";

        public static ISettingsBuilder AddCompanionPracticeSettings(Dictionary<Hero, HeroPracticeSettings> heroPracticeSettings)
        {
            var builder = BaseSettingsBuilder.Create(SettingsId, SettingsName)!
                .SetFormat("json2")
                .SetFolderName("AO Hero Practice Settings");

            var clanMembers = Hero.FindAll(x => x.IsAlive && !x.IsChild && x.Clan == Clan.PlayerClan && x != Hero.MainHero).OrderBy(x => x.Name.ToString()).ToList();

            AddClanHeroes(builder, HeadingClanLeader, [Hero.MainHero], 2);
            AddClanHeroes(builder, HeadingFamilyMembers, clanMembers.Where(x => !x.IsWanderer).ToList(), 2);
            AddClanHeroes(builder, HeadingCompanions, clanMembers.Where(x => x.IsWanderer).ToList(), 3);

            builder.CreatePreset(BaseSettings.DefaultPresetId, BaseSettings.DefaultPresetName, BuildDefaultPreset);

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
                GetSettingNamesAndHints(hero, out var enableLoadoutChoiceText, out var enableLoadoutChoiceHint,
                    out var onlyPriorityLoadoutsText, out var onlyPriorityLoadoutsHint,
                    out var prioritizeExpensiveEquipmentText, out var prioritizeExpensiveEquipmentHint,
                    out var firstPriorityText, out var firstPriorityHint, out var secondPriorityText,
                    out var secondPriorityHint, out var thirdPriorityText, out var thirdPriorityHint);
                builder
                    .AddBool($"{hero.StringId}_enable_loadout_choice", enableLoadoutChoiceText, new PropertyRef(SymbolExtensions2.GetPropertyInfo((HeroPracticeSettings x) => x.EnableLoadoutChoice)!, settingContainer),
                        propBuilder => propBuilder.SetRequireRestart(false).SetOrder(0).SetHintText(enableLoadoutChoiceHint))
                    .AddBool($"{hero.StringId}_only_priority_loadouts", onlyPriorityLoadoutsText, new PropertyRef(SymbolExtensions2.GetPropertyInfo((HeroPracticeSettings x) => x.OnlyPriorityLoadouts)!, settingContainer),
                        propBuilder => propBuilder.SetRequireRestart(false).SetOrder(1).SetHintText(onlyPriorityLoadoutsHint))
                    .AddBool($"{hero.StringId}_prioritize_expensive_equipment", prioritizeExpensiveEquipmentText, new PropertyRef(SymbolExtensions2.GetPropertyInfo((HeroPracticeSettings x) => x.PrioritizeExpensiveEquipment)!, settingContainer),
                        propBuilder => propBuilder.SetRequireRestart(false).SetOrder(2).SetHintText(prioritizeExpensiveEquipmentHint))
                    .AddDropdown($"{hero.StringId}_weapons_first_priority", firstPriorityText, settingContainer.FirstPriorityWeaponsDropdown.SelectedIndex,
                        new PropertyRef(SymbolExtensions2.GetPropertyInfo((HeroPracticeSettings x) => x.FirstPriorityWeaponsDropdown)!, settingContainer),
                        propBuilder => propBuilder.SetRequireRestart(false).SetOrder(10).SetHintText(firstPriorityHint))
                    .AddDropdown($"{hero.StringId}_weapons_second_priority", secondPriorityText, settingContainer.SecondPriorityWeaponsDropdown.SelectedIndex,
                        new PropertyRef(SymbolExtensions2.GetPropertyInfo((HeroPracticeSettings x) => x.SecondPriorityWeaponsDropdown)!, settingContainer),
                        propBuilder => propBuilder.SetRequireRestart(false).SetOrder(11).SetHintText(secondPriorityHint))
                    .AddDropdown($"{hero.StringId}_weapons_third_priority", thirdPriorityText, settingContainer.ThirdPriorityWeaponsDropdown.SelectedIndex,
                        new PropertyRef(SymbolExtensions2.GetPropertyInfo((HeroPracticeSettings x) => x.ThirdPriorityWeaponsDropdown)!, settingContainer),
                        propBuilder => propBuilder.SetRequireRestart(false).SetOrder(12).SetHintText(thirdPriorityHint))
                    .SetGroupOrder(groupOrder);
            }

            void BuildDefaultPreset(ISettingsPresetBuilder builder)
            {
                clanMembers.ForEach(hero =>
                {
                    var defaultSettingContainer = new HeroPracticeSettings();
                    builder
                        .SetPropertyValue($"{hero.StringId}_enable_loadout_choice", defaultSettingContainer.EnableLoadoutChoice)
                        .SetPropertyValue($"{hero.StringId}_only_priority_loadouts", defaultSettingContainer.OnlyPriorityLoadouts)
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
            }
        }

        internal static void RegisterCompanionPracticeSettings()
        {
            var builder = AddCompanionPracticeSettings(AOArenaBehaviorManager._companionPracticeSettings!);
            SubModule.PerSaveSettings = builder.BuildAsPerSave();
            SubModule.PerSaveSettings?.Register();
        }

        internal static void UnregisterCompanionPracticeSettings()
        {
            var oldSettings = SubModule.PerSaveSettings;
            oldSettings?.Unregister();
            SubModule.PerSaveSettings = null;

            AOArenaBehaviorManager._companionPracticeSettings = null;
        }

        private static void GetSettingNamesAndHints(Hero hero, out string enableLoadoutChoiceText, out string enableLoadoutChoiceHint, out string onlyPriorityLoadoutsText, out string onlyPriorityLoadoutsHint, out string prioritizeExpensiveEquipmentText, out string prioritizeExpensiveEquipmentHint, out string firstPriorityText, out string firstPriorityHint, out string secondPriorityText, out string secondPriorityHint, out string thirdPriorityText, out string thirdPriorityHint)
        {
            if (hero != Hero.MainHero)
            {
                enableLoadoutChoiceText = "{=fL485YX24}Enable loadout choice";
                enableLoadoutChoiceHint = "{=QLQYQTctQ}When this option is enabled, corresponding hero is allowed to choose weapons for arena practice matches, and you will have to pay the usual fee for this. Otherwise, they will use random weapons like everyone else. Default is set in global mod settings.";

                onlyPriorityLoadoutsText = "{=zC7CG8kwF}Only priority loadouts";
                onlyPriorityLoadoutsHint = "{=ieRmdET16}When this option is enabled, corresponding hero will only choose practice weapons if any weapon preference is set and at least one matching weapon loadout is available in the arena. Otherwise, they will use random weapons like everyone else. Default is set in global mod settings.";

                prioritizeExpensiveEquipmentText = "{=cWdhDpsOS}Prioritize expensive equipment";
                prioritizeExpensiveEquipmentHint = "{=YwbIZExy1}When this option is enabled, corresponding hero will select weapons for arena practice matches, prioritizing better and more expensive equipment. Otherwise, the least expensive set of weapons that meets the given preferences will be selected. Default is set in global mod settings.";

                firstPriorityText = "{=G2IdbqB8p}First priority";
                firstPriorityHint = "{=ZWh07gP41}Specify the weapon skill that has the highest priority for the corresponding hero to train. This hero will always choose a weapon loadout containing a weapon based on that skill, if available. Default is [None].";

                secondPriorityText = "{=bcgL8E4Ol}Second priority";
                secondPriorityHint = "{=Yideeiww5}Specify the weapon skill that has the second highest priority for the corresponding hero to train. This hero will choose a weapon loadout containing a weapon based on this skill if it is available and there are no higher priority alternatives. Default is [None].";

                thirdPriorityText = "{=t45rwWZc5}Third priority";
                thirdPriorityHint = "{=lHiP6Z2Qo}Specify the weapon skill that has the third highest priority for the corresponding hero to train. This hero will choose a weapon loadout containing a weapon based on this skill if it is available and there are no higher priority alternatives. Default is [None].";
            }
            else
            {
                enableLoadoutChoiceText = "{=kbL2QnpuF}Enable automatic loadout choice";
                enableLoadoutChoiceHint = "{=vGIOM104P}When this option is enabled, the weapon loadout will be automatically selected for the player based on other preferences set here when entering any arena practice mode from the menu. Default is set in global mod settings.";

                onlyPriorityLoadoutsText = "{=zC7CG8kwF}Only priority loadouts";
                onlyPriorityLoadoutsHint = "{=JidAUNLNo}When this option is enabled, automatic weapon loadout will only happen for player if any weapon preference is set and at least one matching weapon loadout is available in the arena. Otherwise, they will use random weapons as usual. Default is set in global mod settings.";

                prioritizeExpensiveEquipmentText = "{=cWdhDpsOS}Prioritize expensive equipment";
                prioritizeExpensiveEquipmentHint = "{=Wse8QGJyQ}When this option is enabled, better and more expensive equipment will be prefered for automatic weapon loadout selection for player. Otherwise, the least expensive set of weapons that meets the given preferences will be selected. Default is set in global mod settings.";

                firstPriorityText = "{=G2IdbqB8p}First priority";
                firstPriorityHint = "{=DOGp7LBDe}Specify the weapon skill that has the highest priority for the player character to train. A weapon loadout containing a weapon based on that skill will always be chosen, if available. Default is [None].";

                secondPriorityText = "{=bcgL8E4Ol}Second priority";
                secondPriorityHint = "{=bhSUfmKir}Specify the weapon skill that has the second highest priority for the player character to train. A weapon loadout containing a weapon based on this skill will be chosen if it is available and there are no higher priority alternatives. Default is [None].";

                thirdPriorityText = "{=t45rwWZc5}Third priority";
                thirdPriorityHint = "{=IP7JZx9bb}Specify the weapon skill that has the third highest priority for the player character to train. A weapon loadout containing a weapon based on this skill will be chosen if it is available and there are no higher priority alternatives. Default is [None].";
            }
        }
    }
}