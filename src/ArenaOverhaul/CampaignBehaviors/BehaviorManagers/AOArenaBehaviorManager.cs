using ArenaOverhaul.ModSettings;
using ArenaOverhaul.Tournament;

using System.Collections.Generic;

using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;
using TaleWorlds.Localization;
using TaleWorlds.Core;
using System;
using System.Linq;
using Bannerlord.ButterLib.Common.Helpers;
using TaleWorlds.CampaignSystem.Roster;
using SandBox.Missions.MissionLogics.Arena;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem.Party;

namespace ArenaOverhaul.CampaignBehaviors.BehaviorManagers
{
    public class AOArenaBehaviorManager
    {
        private ArenaPracticeMode _practiceMode;
        private int _chosenLoadout = -1;
        private int _chosenLoadoutStage = 1;

        internal static Dictionary<Hero, HeroPracticeSettings>? _companionPracticeSettings;

        public ArenaPracticeMode PracticeMode => _practiceMode;
        public int ChosenLoadout { get => _chosenLoadout; internal set => _chosenLoadout = value; }
        public int ChosenLoadoutStage { get => _chosenLoadoutStage; internal set => _chosenLoadoutStage = value; }

        public static AOArenaBehaviorManager? Instance { get; internal set; }

        [SaveableProperty(1)]
        public Dictionary<Town, PrizeItemInfo?> TournamentPrizes { get; private set; } = [];

        public void ResetWeaponChoice()
        {
            _chosenLoadout = -1;
            _chosenLoadoutStage = 1;
        }

        public void SetStandardPracticeMode() => SetPracticeMode(ArenaPracticeMode.Standard);

        public void SetExpansivePracticeMode() => SetPracticeMode(ArenaPracticeMode.Expansive);

        public void SetParryPracticeMode() => SetPracticeMode(ArenaPracticeMode.Parry);

        public void SetPracticeMode(ArenaPracticeMode mode)
        {
            _practiceMode = mode;
            ResetWeaponChoice();
        }

        public float GetPracticeExperienceRate()
        {
            return PracticeMode switch
            {
                ArenaPracticeMode.Standard => Settings.Instance!.PracticeExperienceRate,
                ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeExperienceRate,
                ArenaPracticeMode.Parry => Settings.Instance!.ParryPracticeExperienceRate,
                _ => throw new NotImplementedException(),
            };
        }

        public int GetPracticeModeChoiceCost() => GetPracticeModeChoiceCost(PracticeMode);

        public int GetPracticeModeChoiceCost(ArenaPracticeMode practiceMode)
        {
            return practiceMode switch
            {
                ArenaPracticeMode.Parry => Settings.Instance!.ParryPracticeSetupCost,
                _ => 0,
            };
        }

        public bool CheckAffordabilityForNextPracticeRound(out TextObject? explanation) => CheckAffordabilityForNextPracticeRound(PracticeMode, ChosenLoadout >= 0 ? ChosenLoadoutStage * GetWeaponLoadoutChoiceCost() : 0, out explanation);

        public bool CheckAffordabilityForNextPracticeRound(ArenaPracticeMode practiceMode, int playerLoadoutPrice, out TextObject? explanation)
        {
            int matchPrice = GetPracticeModeChoiceCost(practiceMode);
            int companionLoadoutPrice = 200000;
            int totalPrice = matchPrice + playerLoadoutPrice + companionLoadoutPrice;
            if (totalPrice > 0)
            {
                var arrangementExpenses = new TextObject("{=}{?PRACTICE_CHOICE_HAS_PRICE}{NEW_LINE} - Fee for a special type of practice in the amount of {PRACTICE_CHOICE_PRICE}{GOLD_ICON}.{?}{\\?}", new()
                {
                    ["PRACTICE_CHOICE_HAS_PRICE"] = (matchPrice > 0 ? 1 : 0).ToString(),
                    ["PRACTICE_CHOICE_PRICE"] = matchPrice.ToString(),
                    ["NEW_LINE"] = Environment.NewLine
                });
                var playerLoadoutExpenses = new TextObject("{=}{?WEAPON_CHOICE_HAS_PRICE}{NEW_LINE} - Fee for using custom weapon set in the amount of {WEAPON_CHOICE_PRICE}{GOLD_ICON}.{?}{\\?}", new()
                {
                    ["WEAPON_CHOICE_HAS_PRICE"] = (playerLoadoutPrice > 0 ? 1 : 0).ToString(),
                    ["WEAPON_CHOICE_PRICE"] = playerLoadoutPrice.ToString(),
                    ["NEW_LINE"] = Environment.NewLine
                });
                var companionLoadoutExpenses = new TextObject("{=}{?COMPANION_WEAPON_CHOICES_HAVE_PRICE}{NEW_LINE} - Fee for the use of a custom set of weapons by your companions in the amount of {COMPANION_WEAPON_CHOICES_PRICE}{GOLD_ICON} for all of them.{?}{\\?}", new()
                {
                    ["COMPANION_WEAPON_CHOICES_HAVE_PRICE"] = (companionLoadoutPrice > 0 ? 1 : 0).ToString(),
                    ["COMPANION_WEAPON_CHOICES_PRICE"] = companionLoadoutPrice.ToString(),
                    ["NEW_LINE"] = Environment.NewLine
                });
                var expensesBreakdown = new TextObject("{=}Arranging a practice round based on the requested set of rules and preferences will cost you a total of {TOTAL_EXPENSES}{GOLD_ICON}.{NEW_LINE}{NEW_LINE}This consists of:{ARRANGEMENT_EXPENSES}{PLAYER_LOADOUT_EXPENSES}{COMPANION_LOADOUT_EXPENSES}", new()
                {
                    ["TOTAL_EXPENSES"] = totalPrice.ToString(),
                    ["ARRANGEMENT_EXPENSES"] = arrangementExpenses,
                    ["PLAYER_LOADOUT_EXPENSES"] = playerLoadoutExpenses,
                    ["COMPANION_LOADOUT_EXPENSES"] = companionLoadoutExpenses,
                    ["NEW_LINE"] = Environment.NewLine
                });

                if (Hero.MainHero.Gold < totalPrice)
                {
                    explanation = new TextObject("{=EPjU6L6kg}You don't have enough gold!{NEW_LINE}{NEW_LINE}{EXPENSES_BREAKDOWN}", new()
                    {
                        ["EXPENSES_BREAKDOWN"] = expensesBreakdown,
                        ["NEW_LINE"] = Environment.NewLine
                    });
                    return false;
                }
                else
                {
                    explanation = new TextObject("{=9REfw5FN6}{EXPENSES_BREAKDOWN}{NEW_LINE}{NEW_LINE}Currently you can afford {AFFORDABLE_ROUNDS} {?AFFORDABLE_ROUNDS.PLURAL_FORM}practice rounds{?}practice round{\\?} at this price.", new()
                    {
                        ["EXPENSES_BREAKDOWN"] = expensesBreakdown,
                        ["NEW_LINE"] = Environment.NewLine
                    });
                    LocalizationHelper.SetNumericVariable(explanation, "AFFORDABLE_ROUNDS", Hero.MainHero.Gold / totalPrice);
                    return true;
                }
            }
            explanation = null;
            return true;
        }

        public List<Equipment> FilterAvailableWeapons(List<Equipment> loadoutList)
        {
            return FilterAvailableWeapons(loadoutList, PracticeMode);
        }

        public static List<Equipment> FilterAvailableWeapons(List<Equipment> loadoutList, ArenaPracticeMode practiceMode)
        {
            if (practiceMode != ArenaPracticeMode.Parry)
            {
                return loadoutList;
            }
            return loadoutList.Where(x =>
            {
                for (int index = 0; index < 5; ++index)
                {
                    var itemSlot = x[index];
                    if (!itemSlot.IsEmpty)
                    {
                        switch (itemSlot.Item.PrimaryWeapon.WeaponClass)
                        {
                            case > WeaponClass.LowGripPolearm:
                            case WeaponClass.OneHandedPolearm or WeaponClass.TwoHandedPolearm or WeaponClass.LowGripPolearm when itemSlot.Item.PrimaryWeapon.SwingDamage <= 1:
                                return false;
                        }
                    }
                }
                return true;
            }).ToList();
        }

        public void PayForPracticeMatch()//PayForWeaponLoadout()
        {
            int matchPrice = GetPracticeModeChoiceCost();
            int playerLoadoutPrice = ChosenLoadout >= 0 ? ChosenLoadoutStage * GetWeaponLoadoutChoiceCost() : 0;

            int totalPrice = matchPrice + playerLoadoutPrice;
            if (totalPrice > 0)
            {
                GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, totalPrice);
            }
        }

        public void PrepareCompanionLoadouts()
        {

        }

        public int GetWeaponLoadoutChoiceCost()
        {
            return PracticeMode switch
            {
                ArenaPracticeMode.Standard => Settings.Instance!.PracticeLoadoutChoiceCost,
                ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeLoadoutChoiceCost,
                ArenaPracticeMode.Parry => Settings.Instance!.ParryPracticeLoadoutChoiceCost,
                _ => throw new NotImplementedException(),
            };
        }

        public bool IsWeaponChoiceAllowed()
        {
            return PracticeMode switch
            {
                ArenaPracticeMode.Standard => Settings.Instance!.PracticeEnableLoadoutChoice,
                ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeEnableLoadoutChoice,
                ArenaPracticeMode.Parry => Settings.Instance!.ParryPracticeEnableLoadoutChoice,
                _ => throw new NotImplementedException(),
            };
        }

        public PracticeEquipmentType GetParticipantArmorType()
        {
            return PracticeMode switch
            {
                ArenaPracticeMode.Standard => Settings.Instance!.PracticeEquipment.SelectedValue.EnumValue,
                ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeEquipment.SelectedValue.EnumValue,
                ArenaPracticeMode.Parry => Settings.Instance!.ParryPracticeEquipment.SelectedValue.EnumValue,
                _ => throw new NotImplementedException(),
            };
        }

        public MissionMode GetArenaPracticeMissionMode()
        {
            var practiceEquipmentSetting = PracticeMode switch
            {
                ArenaPracticeMode.Standard => Settings.Instance!.PracticeEquipment.SelectedIndex,
                ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeEquipment.SelectedIndex,
                ArenaPracticeMode.Parry => Settings.Instance!.ParryPracticeEquipment.SelectedIndex,
                _ => throw new NotImplementedException(),
            };
            return practiceEquipmentSetting switch
            {
                1 => MissionMode.Tournament,
                _ => MissionMode.Battle,
            };
        }

        internal static List<CharacterObject> GetPlayerRelatedParticipantCharacters(ArenaPracticeMode practiceMode, int maxParticipantCount)
        {
            List<CharacterObject> characterObjectList = [];
            if (practiceMode is not ArenaPracticeMode.Expansive)
            {
                return characterObjectList;
            }

            int num1 = 0;
            int num2 = 0;
            int num3 = 0;
            maxParticipantCount = maxParticipantCount * 2 / 3;
            if (characterObjectList.Count < maxParticipantCount)
            {
                int num4 = maxParticipantCount - characterObjectList.Count;
                foreach (TroopRosterElement troopRosterElement in Hero.MainHero.PartyBelongedTo.Party.MemberRoster.GetTroopRoster())
                {
                    if (troopRosterElement.Character == Hero.MainHero.CharacterObject)
                    {
                        continue;
                    }
                    else if (!characterObjectList.Contains(troopRosterElement.Character) && troopRosterElement.Character.IsHero)
                    {
                        characterObjectList.Add(troopRosterElement.Character);
                    }
                    else if (!characterObjectList.Contains(troopRosterElement.Character) && troopRosterElement.Character.Tier == 3 && num4 * 0.400000005960464 > num1)
                    {
                        characterObjectList.Add(troopRosterElement.Character);
                        ++num1;
                    }
                    else if (!characterObjectList.Contains(troopRosterElement.Character) && troopRosterElement.Character.Tier == 4 && num4 * 0.400000005960464 > num2)
                    {
                        characterObjectList.Add(troopRosterElement.Character);
                        ++num2;
                    }
                    else if (!characterObjectList.Contains(troopRosterElement.Character) && troopRosterElement.Character.Tier == 5 && num4 * 0.200000002980232 > num3)
                    {
                        characterObjectList.Add(troopRosterElement.Character);
                        ++num3;
                    }
                    if (characterObjectList.Count >= maxParticipantCount)
                    {
                        break;
                    }
                }
            }
            return characterObjectList;
        }

        internal static int GetInitialParticipantsCount(ArenaPracticeMode practiceMode)
        {
            return practiceMode switch
            {
                ArenaPracticeMode.Standard => Settings.Instance!.PracticeInitialParticipants, //6
                ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeInitialParticipants, //7
                ArenaPracticeMode.Parry => Settings.Instance!.ParryPracticeInitialParticipants, //1
                _ => throw new NotImplementedException(),
            };
        }

        internal static int GetTotalParticipantsCount(ArenaPracticeMode practiceMode)
        {
            return practiceMode switch
            {
                ArenaPracticeMode.Standard => Settings.Instance!.PracticeTotalParticipants, //30
                ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeTotalParticipants, //90
                ArenaPracticeMode.Parry => Settings.Instance!.ParryPracticeTotalParticipants, //15
                _ => throw new NotImplementedException(),
            };
        }

        internal static int GetActiveOpponentCount(ArenaPracticeMode practiceMode)
        {
            return practiceMode switch
            {
                ArenaPracticeMode.Standard => Settings.Instance!.PracticeActiveParticipants, //6
                ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeActiveParticipants, //9
                ArenaPracticeMode.Parry => Settings.Instance!.ParryPracticeActiveParticipants, //1
                _ => throw new NotImplementedException(),
            };
        }

        internal static int GetMinimumActiveOpponentCount(ArenaPracticeMode practiceMode)
        {
            return practiceMode switch
            {
                ArenaPracticeMode.Standard => Settings.Instance!.PracticeActiveParticipantsMinimum, //2
                ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeActiveParticipantsMinimum, //4
                ArenaPracticeMode.Parry => Settings.Instance!.ParryPracticeActiveParticipantsMinimum, //1
                _ => throw new NotImplementedException(),
            };
        }

        internal static int GetAITeamsCount(ArenaPracticeMode practiceMode)
        {
            var settingValue = practiceMode switch
            {
                ArenaPracticeMode.Standard => Settings.Instance!.PracticeAITeamsCount, //0
                ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeAITeamsCount, //0
                ArenaPracticeMode.Parry => Settings.Instance!.ParryPracticeAITeamsCount, //1
                _ => throw new NotImplementedException(),
            };
            return settingValue > 0 ? settingValue : GetActiveOpponentCount(practiceMode);
        }
    }

    [Flags]
    public enum ArenaPracticeMode
    {
        Standard = 1,
        Expansive = 2,
        Parry = 4,

        ExceptParry = Standard | Expansive,
        All = Standard | Expansive | Parry,
    }
}