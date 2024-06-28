using ArenaOverhaul.ModSettings;
using ArenaOverhaul.Tournament;

using Bannerlord.ButterLib.Common.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace ArenaOverhaul.CampaignBehaviors.BehaviorManagers
{
    public class AOArenaBehaviorManager
    {
        private ArenaPracticeMode _practiceMode;
        private bool _isPlayerPrePractice = false;
        private bool _isAfterPracticeTalk = false;
        internal bool _enteredPracticeFightFromMenu;

        private CharacterObject? _chosenCharacter;

        private int _chosenLoadout = -1;
        private int _chosenLoadoutStage = 1;

        internal static Dictionary<Hero, HeroPracticeSettings>? _companionPracticeSettings;
        internal static List<CharacterObject>? _lastPlayerRelatedCharacterList;

        public ArenaPracticeMode PracticeMode => _practiceMode;
        public bool IsPlayerPrePractice { get => _isPlayerPrePractice; internal set => _isPlayerPrePractice = value; }
        public bool IsAfterPracticeTalk { get => _isAfterPracticeTalk; internal set => _isAfterPracticeTalk = value; }

        public CharacterObject? ChosenCharacter { get => _chosenCharacter; internal set => _chosenCharacter = value; }
        public int ChosenLoadout { get => _chosenLoadout; internal set => _chosenLoadout = value; }
        public int ChosenLoadoutStage { get => _chosenLoadoutStage; internal set => _chosenLoadoutStage = value; }

        public Dictionary<Hero, (int ChosenLoadoutStage, int ChosenLoadout)> CompanionLoadouts { get; private set; } = [];
        internal Dictionary<CultureObject, List<WeaponLoadoutInfo>> WeaponLoadoutInformation { get; private set; } = [];

        public static AOArenaBehaviorManager? Instance { get; internal set; }

        [SaveableProperty(1)]
        public Dictionary<Town, PrizeItemInfo?> TournamentPrizes { get; private set; } = [];

        public void ResetCompanionChoice()
        {
            _chosenCharacter = null;
        }

        public void ResetWeaponChoice()
        {
            _chosenLoadout = -1;
            _chosenLoadoutStage = 1;
        }

        public void SetStandardPracticeMode() => SetPracticeMode(ArenaPracticeMode.Standard);
        public void SetExpansivePracticeMode() => SetPracticeMode(ArenaPracticeMode.Expansive);
        public void SetParryPracticeMode() => SetPracticeMode(ArenaPracticeMode.Parry);
        public void SetTeamPracticeMode() => SetPracticeMode(ArenaPracticeMode.Team);

        public void SetPracticeMode(ArenaPracticeMode mode)
        {
            _practiceMode = mode;
            ResetCompanionChoice();
            ResetWeaponChoice();
        }

        public bool CheckAffordabilityForNextPracticeRound(out TextObject? explanation) => CheckAffordabilityForNextPracticeRound(PracticeMode, ChosenLoadout >= 0 ? ChosenLoadoutStage : 0, out explanation);

        public bool CheckAffordabilityForNextPracticeRound(ArenaPracticeMode practiceMode, int playerLoadoutStage, out TextObject? explanation, bool isMenuCall = false)
        {
            var weaponLoadoutChoiceCost = GetWeaponLoadoutChoiceCost(practiceMode);
            PrepareCompanionLoadouts(practiceMode, isMenuCall);

            int matchPrice = GetPracticeModeChoiceCost(practiceMode);
            int playerLoadoutPrice = (isMenuCall ? (ChosenLoadout >= 0 && ChosenLoadoutStage > 0 ? ChosenLoadoutStage : 0) : playerLoadoutStage) * weaponLoadoutChoiceCost;
            int companionLoadoutPrice = GetCompanionLoadoutsPrice(practiceMode, weaponLoadoutChoiceCost);

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
                            case WeaponClass.OneHandedPolearm or WeaponClass.TwoHandedPolearm or WeaponClass.LowGripPolearm when itemSlot.Item.PrimaryWeapon.SwingDamage <= 5:
                                return false;
                        }
                    }
                }
                return true;
            }).ToList();
        }

        public void PayForPracticeMatch()
        {
            int weaponLoadoutChoiceCost = GetWeaponLoadoutChoiceCost();
            PrepareCompanionLoadouts(PracticeMode, _enteredPracticeFightFromMenu);

            int matchPrice = GetPracticeModeChoiceCost();
            int playerLoadoutPrice = ChosenLoadout >= 0 ? ChosenLoadoutStage * weaponLoadoutChoiceCost : 0;
            int companionLoadoutPrice = GetCompanionLoadoutsPrice(PracticeMode, weaponLoadoutChoiceCost);

            int totalPrice = matchPrice + playerLoadoutPrice + companionLoadoutPrice;
            if (totalPrice > 0)
            {
                GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, totalPrice);
            }
        }

        public void PrepareCompanionLoadouts(ArenaPracticeMode practiceMode, bool isMenuCall = false)
        {
            CompanionLoadouts = [];

            var settlement = Settlement.CurrentSettlement;
            var settlementCulture = settlement?.MapFaction?.Culture ?? settlement?.Culture;

            GetAvailableLoadoutInfo(settlementCulture);
            if (settlementCulture is null || _companionPracticeSettings is null || !WeaponLoadoutInformation.TryGetValue(settlementCulture, out var cultureLoadoutsInformation))
            {
                return;
            }

            //Player
            if (isMenuCall && ChosenLoadout < 0)
            {
                GetHeroLoadout(cultureLoadoutsInformation, Hero.MainHero, out var playerLoadoutStage, out var playerLoadoutIndex);
                if (playerLoadoutStage > 0 && playerLoadoutIndex >= 0)
                {
                    ChosenLoadoutStage = playerLoadoutStage;
                    ChosenLoadout = playerLoadoutIndex;
                }
            }

            //Companions
            var companionParticipants = GetPlayerRelatedParticipantCharacters(practiceMode, GetTotalParticipantsCount(practiceMode)).Where(x => x.IsHero).Select(x => x.HeroObject).ToList();
            if (practiceMode == ArenaPracticeMode.Team)
            {
                companionParticipants = _lastPlayerRelatedCharacterList!.Where(x => x.IsHero).Select(x => x.HeroObject).ToList();
            }
            foreach (var companion in companionParticipants)
            {
                GetHeroLoadout(cultureLoadoutsInformation, companion, out var loadoutStage, out var loadoutIndex);
                if (loadoutStage > 0 && loadoutIndex >= 0)
                {
                    CompanionLoadouts[companion] = (loadoutStage, loadoutIndex);
                }
            }
        }

        private static void GetHeroLoadout(List<WeaponLoadoutInfo> cultureLoadoutsInformation, Hero companion, out int loadoutStage, out int loadoutIndex)
        {
            loadoutStage = -1; loadoutIndex = -1;
            if (!_companionPracticeSettings!.TryGetValue(companion, out var heroSettings) || !heroSettings.EnableLoadoutChoice)
            {
                return;
            }

            var scoredLoadouts = cultureLoadoutsInformation
                .Select(x => (x.LoadoutStage, x.LoadoutIndex, LoadoutScore: x.GetScore(companion, [heroSettings.FirstPriorityWeapons, heroSettings.SecondPriorityWeapons, heroSettings.ThirdPriorityWeapons], out var weaponPreferenceIsMet), PreferenceIsMet: weaponPreferenceIsMet))
                .Where(x => x.PreferenceIsMet || !heroSettings.OnlyPriorityLoadouts).ToList();

            if (scoredLoadouts.Count <= 0)
            {
                return;
            }

            (loadoutStage, loadoutIndex, _, _) = heroSettings.PrioritizeExpensiveEquipment
                ? scoredLoadouts.OrderByDescending(x => x.LoadoutScore).ThenByDescending(x => x.LoadoutStage).First()
                : scoredLoadouts.OrderBy(x => x.LoadoutStage).ThenByDescending(x => x.LoadoutScore).First();
        }

        private void GetAvailableLoadoutInfo(CultureObject? settlementCulture)
        {
            if (settlementCulture is null || (WeaponLoadoutInformation.TryGetValue(settlementCulture, out var cultureLoadoutsInformation) && cultureLoadoutsInformation != null))
            {
                return;
            }

            List<WeaponLoadoutInfo> listOfExistingLoadouts = [];
            int practiceLoadoutStages = Settings.Instance!.PracticeLoadoutStages;
            for (int practiceStage = 1; practiceStage <= practiceLoadoutStages; practiceStage++)
            {
                CharacterObject? characterObject = Game.Current.ObjectManager.GetObject<CharacterObject>("weapon_practice_stage_" + practiceStage.ToString() + "_" + settlementCulture.StringId.ToLower());
                if (characterObject is null)
                {
                    continue;
                }

                var battleEquipments = characterObject.BattleEquipments.ToList();
                for (int i = 0; i < battleEquipments.Count; i++)
                {
                    string[] itemIdArr = new string[4];
                    int loadout = i;
                    int equipmentStage = practiceStage;

                    int oneHandedScore = 0;
                    int twoHandedScore = 0;
                    int polearmScore = 0;
                    int bowScore = 0;
                    int crossbowScore = 0;
                    int throwingScore = 0;

                    for (int x = 0; x < 4; x++)
                    {
                        EquipmentElement equipmentFromSlot = battleEquipments[i].GetEquipmentFromSlot((EquipmentIndex) x);
                        if (equipmentFromSlot.Item != null)
                        {
                            itemIdArr[x] = equipmentFromSlot.Item.StringId;
                            if (equipmentFromSlot.Item.WeaponComponent.PrimaryWeapon is WeaponComponentData weaponData)
                            {
                                if (weaponData.RelevantSkill == DefaultSkills.OneHanded)
                                {
                                    oneHandedScore += weaponData.SwingDamage + weaponData.ThrustDamage / 2 + (weaponData.IsShield ? weaponData.MaxDataValue / 5 : 0);
                                }
                                else if (weaponData.RelevantSkill == DefaultSkills.TwoHanded)
                                {
                                    twoHandedScore += weaponData.SwingDamage + weaponData.ThrustDamage / 2;
                                }
                                else if (weaponData.RelevantSkill == DefaultSkills.Polearm)
                                {
                                    polearmScore += weaponData.SwingDamage + weaponData.ThrustDamage;
                                }
                                else if (weaponData.RelevantSkill == DefaultSkills.Bow)
                                {
                                    bowScore += weaponData.MissileDamage + (weaponData.IsAmmo ? weaponData.MaxDataValue : 0);
                                }
                                else if (weaponData.RelevantSkill == DefaultSkills.Crossbow)
                                {
                                    crossbowScore += weaponData.MissileDamage + (weaponData.IsAmmo ? weaponData.MaxDataValue : 0);
                                }
                                else if (weaponData.RelevantSkill == DefaultSkills.Throwing)
                                {
                                    throwingScore += (weaponData.MaxDataValue > 0 ? weaponData.MaxDataValue * weaponData.MissileDamage / 4 : weaponData.MissileDamage) + (weaponData.SwingDamage + weaponData.ThrustDamage) / 4;
                                }
                            }
                        }
                    }
                    string itemIDs = string.Join("_", itemIdArr.Where(s => !string.IsNullOrEmpty(s)));
                    var loadoutEntry = new WeaponLoadoutInfo(itemIDs, equipmentStage, loadout, oneHandedScore, twoHandedScore, polearmScore, bowScore, crossbowScore, throwingScore);

                    if (!listOfExistingLoadouts.Any(x => x.ItemIDs == loadoutEntry.ItemIDs && x.LoadoutStage == loadoutEntry.LoadoutStage))
                    {
                        listOfExistingLoadouts.Add(loadoutEntry);
                    }
                }
            }

            WeaponLoadoutInformation[settlementCulture] = listOfExistingLoadouts;
        }

        private int GetCompanionLoadoutsPrice(ArenaPracticeMode practiceMode, int weaponLoadoutChoiceCost)
        {
            int companionLoadoutPrice = CompanionLoadouts.Values.Sum(x => x.ChosenLoadoutStage > 0 && x.ChosenLoadout >= 0 ? x.ChosenLoadoutStage * weaponLoadoutChoiceCost : 0);
            return companionLoadoutPrice;
        }

        internal static List<CharacterObject> GetAvailableHeroes()
        {
            List<CharacterObject> characterObjectList = [];
            foreach (TroopRosterElement troopRosterElement in Hero.MainHero.PartyBelongedTo.Party.MemberRoster.GetTroopRoster())
            {
                if (troopRosterElement.Character == Hero.MainHero.CharacterObject || (Instance?.ChosenCharacter is not null && troopRosterElement.Character == Instance.ChosenCharacter))
                {
                    continue;
                }
                else if (!characterObjectList.Contains(troopRosterElement.Character) && troopRosterElement.Character.IsHero && !troopRosterElement.Character.HeroObject.IsWounded)
                {
                    characterObjectList.Add(troopRosterElement.Character);
                }
            }
            return characterObjectList;
        }

        internal static List<CharacterObject> GetPlayerRelatedParticipantCharacters(ArenaPracticeMode practiceMode, int maxParticipantCount)
        {
            List<CharacterObject> characterObjectList = [];
            if (practiceMode is not ArenaPracticeMode.Expansive and not ArenaPracticeMode.Team)
            {
                _lastPlayerRelatedCharacterList = [];
                return characterObjectList;
            }

            int num1 = 0;
            int num2 = 0;
            int num3 = 0;
            maxParticipantCount = practiceMode != ArenaPracticeMode.Team ? maxParticipantCount * 2 / 3 : maxParticipantCount / GetAITeamsCount(practiceMode);
            if (characterObjectList.Count < maxParticipantCount)
            {
                int num4 = maxParticipantCount - characterObjectList.Count;
                foreach (TroopRosterElement troopRosterElement in Hero.MainHero.PartyBelongedTo.Party.MemberRoster.GetTroopRoster())
                {
                    if (troopRosterElement.Character == Hero.MainHero.CharacterObject || (Instance?.ChosenCharacter is not null && troopRosterElement.Character == Instance.ChosenCharacter))
                    {
                        continue;
                    }
                    else if (!characterObjectList.Contains(troopRosterElement.Character) && troopRosterElement.Character.IsHero && !troopRosterElement.Character.HeroObject.IsWounded)
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
            _lastPlayerRelatedCharacterList = new(characterObjectList);
            return practiceMode == ArenaPracticeMode.Expansive ? characterObjectList : [];
        }

        #region Settings by Practice Mode

        public int GetPracticeModeChoiceCost() => GetPracticeModeChoiceCost(PracticeMode);

        public static int GetPracticeModeChoiceCost(ArenaPracticeMode practiceMode)
        {
            return practiceMode switch
            {
                ArenaPracticeMode.Parry => Settings.Instance!.ParryPracticeSetupCost,
                ArenaPracticeMode.Team => Settings.Instance!.TeamPracticeSetupCost,
                _ => 0,
            };
        }

        internal static int GetTotalParticipantsCount(ArenaPracticeMode practiceMode)
        {
            return practiceMode switch
            {
                ArenaPracticeMode.Standard => Settings.Instance!.PracticeTotalParticipants, //30
                ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeTotalParticipants, //90
                ArenaPracticeMode.Parry => Settings.Instance!.ParryPracticeTotalParticipants, //15
                ArenaPracticeMode.Team => Settings.Instance!.TeamPracticeTotalParticipants, //300
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
                ArenaPracticeMode.Team => Settings.Instance!.TeamPracticeActiveParticipants, //30
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
                ArenaPracticeMode.Team => Settings.Instance!.TeamPracticeActiveParticipantsMinimum, //20
                _ => throw new NotImplementedException(),
            };
        }

        internal static int GetInitialParticipantsCount(ArenaPracticeMode practiceMode)
        {
            return practiceMode switch
            {
                ArenaPracticeMode.Standard => Settings.Instance!.PracticeInitialParticipants, //6
                ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeInitialParticipants, //7
                ArenaPracticeMode.Parry => Settings.Instance!.ParryPracticeInitialParticipants, //1
                ArenaPracticeMode.Team => Settings.Instance!.TeamPracticeInitialParticipants, //20
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
                ArenaPracticeMode.Team => Settings.Instance!.TeamPracticeAITeamsCount, //4
                _ => throw new NotImplementedException(),
            };
            return settingValue > 0 ? settingValue : GetActiveOpponentCount(practiceMode);
        }

        internal static uint GetAITeamColor(ArenaPracticeMode practiceMode, int teamIndex)
        {
            if (practiceMode != ArenaPracticeMode.Team)
            {
                return uint.MaxValue;
            }

            //TODO: make settings for team colors with Team Practice header
            var colorIndex = teamIndex switch
            {
                0 => Settings.Instance!.TeamPracticeTeamOneColor,
                1 => Settings.Instance!.TeamPracticeTeamTwoColor,
                2 => Settings.Instance!.TeamPracticeTeamThreeColor,
                3 => Settings.Instance!.TeamPracticeTeamFourColor,
                4 => Settings.Instance!.TeamPracticeTeamFiveColor,
                5 => Settings.Instance!.TeamPracticeTeamSixColor,
                _ => -1
            };

            return colorIndex >= 0 ? BannerManager.GetColor(colorIndex) : uint.MaxValue;
        }

        internal bool IsAgentSwitchingAllowed() => IsAgentSwitchingAllowed(PracticeMode);

        internal static bool IsAgentSwitchingAllowed(ArenaPracticeMode practiceMode)
        {
            return practiceMode switch
            {
                ArenaPracticeMode.Standard => Settings.Instance!.EnablePracticeAgentSwitching,
                ArenaPracticeMode.Expansive => Settings.Instance!.EnableExpansivePracticeAgentSwitching,
                ArenaPracticeMode.Parry => Settings.Instance!.EnableParryPracticeAgentSwitching,
                ArenaPracticeMode.Team => Settings.Instance!.EnableTeamPracticeAgentSwitching,
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
                ArenaPracticeMode.Team => Settings.Instance!.TeamPracticeEnableLoadoutChoice,
                _ => throw new NotImplementedException(),
            };
        }

        public int GetWeaponLoadoutChoiceCost() => GetWeaponLoadoutChoiceCost(PracticeMode);

        public int GetWeaponLoadoutChoiceCost(ArenaPracticeMode practiceMode)
        {
            return practiceMode switch
            {
                ArenaPracticeMode.Standard => Settings.Instance!.PracticeLoadoutChoiceCost,
                ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeLoadoutChoiceCost,
                ArenaPracticeMode.Parry => Settings.Instance!.ParryPracticeLoadoutChoiceCost,
                ArenaPracticeMode.Team => Settings.Instance!.TeamPracticeLoadoutChoiceCost,
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
                ArenaPracticeMode.Team => Settings.Instance!.TeamPracticeEquipment.SelectedValue.EnumValue,
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
                ArenaPracticeMode.Team => Settings.Instance!.TeamPracticeEquipment.SelectedIndex,
                _ => throw new NotImplementedException(),
            };
            return practiceEquipmentSetting switch
            {
                1 => MissionMode.Tournament,
                _ => MissionMode.Battle,
            };
        }

        public float GetPracticeExperienceRate()
        {
            return PracticeMode switch
            {
                ArenaPracticeMode.Standard => Settings.Instance!.PracticeExperienceRate,
                ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeExperienceRate,
                ArenaPracticeMode.Parry => Settings.Instance!.ParryPracticeExperienceRate,
                ArenaPracticeMode.Team => Settings.Instance!.TeamPracticeExperienceRate,
                _ => throw new NotImplementedException(),
            };
        }

        #endregion Settings by Practice Mode

        internal void Sync()
        {
            CompanionLoadouts ??= [];
            WeaponLoadoutInformation ??= [];
        }

        internal record class WeaponLoadoutInfo(string ItemIDs, int LoadoutStage, int LoadoutIndex, int OneHandedScore, int TwoHandedScore, int PolearmScore, int BowScore, int CrossbowScore, int ThrowingScore)
        {
            public int GetScore(Hero hero, WeaponPreference[] priorities, out bool weaponPreferenceIsMet)
            {
                weaponPreferenceIsMet = false;
                return
                    GetWeightedScore(OneHandedScore, WeaponPreference.OneHanded, hero, priorities, ref weaponPreferenceIsMet) +
                    GetWeightedScore(TwoHandedScore, WeaponPreference.TwoHanded, hero, priorities, ref weaponPreferenceIsMet) +
                    GetWeightedScore(PolearmScore, WeaponPreference.Polearm, hero, priorities, ref weaponPreferenceIsMet) +
                    GetWeightedScore(BowScore, WeaponPreference.Bow, hero, priorities, ref weaponPreferenceIsMet) +
                    GetWeightedScore(CrossbowScore, WeaponPreference.Crossbow, hero, priorities, ref weaponPreferenceIsMet) +
                    GetWeightedScore(ThrowingScore, WeaponPreference.Throwing, hero, priorities, ref weaponPreferenceIsMet);
            }

            private static int GetWeightedScore(int score, WeaponPreference weaponGroupToAccess, Hero hero, WeaponPreference[] priorities, ref bool weaponPreferenceIsMet)
            {
                int weightedScore = score * GetWeight(weaponGroupToAccess, hero, priorities, out var groupWasPrioritized);
                if (weightedScore > 0 && groupWasPrioritized)
                {
                    weaponPreferenceIsMet = true;
                }
                return weightedScore;
            }

            private static int GetWeight(WeaponPreference weaponGroupToAccess, Hero hero, WeaponPreference[] priorities, out bool groupWasPrioritized)
            {
                groupWasPrioritized = false;
                int count = priorities.Length;
                for (int i = 0; i < count; i++)
                {
                    if (weaponGroupToAccess == priorities[i])
                    {
                        groupWasPrioritized = true;
                        return 1000 + (count - i) * 100; //(int) Math.Pow(10, count - i + 2);
                    }
                }
                return GetWeightBySkill(weaponGroupToAccess, hero);
            }

            private static int GetWeightBySkill(WeaponPreference weaponGroupToAccess, Hero hero)
            {
                var oneHandedSV = hero.GetSkillValue(DefaultSkills.OneHanded);
                var twoHandedSV = hero.GetSkillValue(DefaultSkills.TwoHanded);
                var polearmSV = hero.GetSkillValue(DefaultSkills.Polearm);
                var bowSV = hero.GetSkillValue(DefaultSkills.Bow);
                var crossbowSV = hero.GetSkillValue(DefaultSkills.Crossbow);
                var throwingSV = hero.GetSkillValue(DefaultSkills.Throwing);

                var weaponGroupSV = weaponGroupToAccess switch
                {
                    WeaponPreference.OneHanded => oneHandedSV,
                    WeaponPreference.TwoHanded => twoHandedSV,
                    WeaponPreference.Polearm => polearmSV,
                    WeaponPreference.Bow => bowSV,
                    WeaponPreference.Crossbow => crossbowSV,
                    WeaponPreference.Throwing => throwingSV,
                    _ => 0,
                };

                List<int> list = [oneHandedSV, twoHandedSV, polearmSV, bowSV, crossbowSV, throwingSV];
                list.Sort();

                return 100 * (weaponGroupSV - list.First()) / (list.Last() - list.First());
            }
        }
    }

    [Flags]
    public enum ArenaPracticeMode
    {
        Standard = 1,
        Expansive = 2,
        Parry = 4,
        Team = 8,

        ExceptParry = Standard | Expansive | Team,
        All = Standard | Expansive | Parry | Team,
    }
}