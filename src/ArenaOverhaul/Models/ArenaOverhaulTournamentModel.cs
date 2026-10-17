using ArenaOverhaul.CampaignBehaviors.BehaviorManagers;
using ArenaOverhaul.Helpers;
using ArenaOverhaul.ModSettings;

using System;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace ArenaOverhaul.Models
{
    public class ArenaOverhaulTournamentModel : TournamentModel
    {
        private readonly TournamentModel _previouslyAssignedModel;

        public ArenaOverhaulTournamentModel(TournamentModel previouslyAssignedModel)
        {
            _previouslyAssignedModel = previouslyAssignedModel;
        }

        public override Equipment GetParticipantArmor(CharacterObject participant)
        {
            var practiceEquipmentSetting = AOArenaBehaviorManager.Instance!.GetParticipantArmorType();
            var practiceEquipment = practiceEquipmentSetting switch
            {
                PracticeEquipmentType.PracticeClothes => GetRandomPracticeClothes(),
                PracticeEquipmentType.CivilianEquipment => participant.RandomCivilianEquipment,
                PracticeEquipmentType.BattleEquipment => participant.RandomBattleEquipment,
                _ => null,
            };

            var equipment = Mission.Current.Mode != MissionMode.Tournament ? practiceEquipment : null;
            return equipment ?? _previouslyAssignedModel.GetParticipantArmor(participant);
        }

        public override TournamentGame CreateTournament(Town town) => _previouslyAssignedModel.CreateTournament(town);

        public override int GetInfluenceReward(Hero winner, Town town) => _previouslyAssignedModel.GetInfluenceReward(winner, town);

        public override int GetNumLeaderboardVictoriesAtGameStart() => _previouslyAssignedModel.GetNumLeaderboardVictoriesAtGameStart();

        public override int GetRenownReward(Hero winner, Town town) => _previouslyAssignedModel.GetRenownReward(winner, town);

        public override (SkillObject skill, int xp) GetSkillXpGainFromTournament(Town town) => _previouslyAssignedModel.GetSkillXpGainFromTournament(town);

        public override float GetTournamentEndChance(TournamentGame tournament) => _previouslyAssignedModel.GetTournamentEndChance(tournament);

        public override float GetTournamentSimulationScore(CharacterObject character) => _previouslyAssignedModel.GetTournamentSimulationScore(character);

        public override float GetTournamentStartChance(Town town) => _previouslyAssignedModel.GetTournamentStartChance(town);

#if v1313
        public override MBList<ItemObject> GetRegularRewardItems(Town town, int regularRewardMinValue, int regularRewardMaxValue)
        {
            // Apply prize scaling if enabled
            int scaledMinValue = Settings.Instance!.EnableTournamentPrizeScaling 
                ? regularRewardMinValue + GetRenownRegularRewardItemValueIncrease(true) 
                : regularRewardMinValue;
            int scaledMaxValue = Settings.Instance!.EnableTournamentPrizeScaling 
                ? regularRewardMaxValue + GetRenownRegularRewardItemValueIncrease() 
                : regularRewardMaxValue;

            MBList<ItemObject> cultureMatchingItems = new();
            MBList<ItemObject> otherItems = new();

            CultureObject? townCulture = town.Culture;
            int culturalPrizesSelectedIndex = Settings.Instance!.CultureRestrictedTournamentPrizes.SelectedIndex;
            CultureObject? requiredCulture = culturalPrizesSelectedIndex >= 1 ? townCulture : null;

            foreach (ItemObject itemObject in Items.All)
            {
                if (itemObject.Value > scaledMinValue && itemObject.Value < scaledMaxValue 
                    && !itemObject.NotMerchandise 
                    && (itemObject.IsCraftedWeapon || itemObject.IsMountable || itemObject.ArmorComponent != null) 
                    && !itemObject.IsCraftedByPlayer)
                {
                    // Culture matching logic
                    bool cultureMatches = itemObject.Culture == townCulture;
                    bool noCultureRestriction = requiredCulture is null || itemObject.Culture is null 
                        || !itemObject.Culture.CanHaveSettlement || itemObject.Culture == requiredCulture;

                    if (cultureMatches)
                    {
                        cultureMatchingItems.Add(itemObject);
                    }
                    else if (noCultureRestriction)
                    {
                        otherItems.Add(itemObject);
                    }
                }
            }

            // Add banner items
            foreach (ItemObject itemObject in Campaign.Current.Models.BannerItemModel.GetPossibleRewardBannerItems())
            {
                if (itemObject.BannerComponent.BannerLevel == 1 || itemObject.BannerComponent.BannerLevel == 2)
                {
                    cultureMatchingItems.Add(itemObject);
                }
            }

            // If no culture-matching items, use other items
            if (cultureMatchingItems.IsEmpty())
            {
                cultureMatchingItems.AddRange(otherItems);
            }

            return cultureMatchingItems;
        }

        public override MBList<ItemObject> GetEliteRewardItems(Town town, int eliteRewardMinValue, int eliteRewardMaxValue)
        {
            MBList<ItemObject> eliteItems = new();

            // Check if prize scaling should use enhanced elite items
            if (Settings.Instance!.EnableTournamentPrizeScaling && Clan.PlayerClan.Tier >= 2)
            {
                CultureObject? townCulture = town.Culture;
                int culturalPrizesSelectedIndex = Settings.Instance!.CultureRestrictedTournamentPrizes.SelectedIndex;
                CultureObject? requiredCulture = culturalPrizesSelectedIndex >= 1 ? townCulture : null;

                // Pick standard high-tier items with cultural restrictions
                var itemObjectCandidates = requiredCulture != null
                    ? Items.All
                        .Where(itemObject => IsSuitablePrize(itemObject, requiredCulture))
                        .GroupBy(itemObject => itemObject.Type)
                        .SelectMany(group => group.OrderByDescending(item => item.Value).Take(10))
                        .ToList()
                    : Items.All
                        .Where(itemObject => IsSuitablePrize(itemObject, null))
                        .GroupBy(itemObject => (itemObject.Type, itemObject.Culture))
                        .SelectMany(group => group.OrderByDescending(item => item.Value).Take(5))
                        .ToList();

                if (itemObjectCandidates.Count > 0)
                {
                    eliteItems.AddRange(itemObjectCandidates);
                }

                // Add unique weapons and armors (stricter culture restrictions)
                requiredCulture = culturalPrizesSelectedIndex >= 2 ? townCulture : null;
                eliteItems.AddRange(Items.All.Where(item => IsUniqueWeapon(item, requiredCulture)));
                eliteItems.AddRange(Items.All.Where(item => IsUniqueArmor(item, requiredCulture)));

                // Add unique mounts (strictest culture restrictions)
                requiredCulture = culturalPrizesSelectedIndex == 3 ? townCulture : null;
                eliteItems.AddRange(Items.All.Where(item => IsUniqueMount(item, requiredCulture)));

                eliteItems.Sort((x, y) => x.Value.CompareTo(y.Value));
            }
            else
            {
                // Use vanilla elite items (hardcoded list)
                eliteItems = _previouslyAssignedModel.GetEliteRewardItems(town, eliteRewardMinValue, eliteRewardMaxValue);

                // Apply culture filter if enabled
                if (Settings.Instance!.CultureRestrictedTournamentPrizes.SelectedIndex >= 1 && town.Culture is CultureObject townCulture)
                {
                    bool ignoreMounts = Settings.Instance!.CultureRestrictedTournamentPrizes.SelectedIndex == 2;
                    FilterByCulture(eliteItems, townCulture, ignoreMounts);
                }
            }

            return eliteItems;
        }
#endif

        /* Prize scaling service methods */
        private static int GetRenownRegularRewardItemValueIncrease(bool isForMinValue = false)
        {
            int playerRenown = MathHelper.GetSoftCappedValue(Clan.PlayerClan.Renown);
            return Clan.PlayerClan.Tier switch
            {
                0 => isForMinValue ? 0 : 0,
                1 => isForMinValue ? 400 : 0,
                2 => isForMinValue ? 1400 : 1000,
                3 => isForMinValue ? 1400 : 2000,
                4 => isForMinValue ? 2400 : 3000,
                5 => isForMinValue ? 2400 : 4000,
                _ => isForMinValue ? 3400 : 5000 + Math.Max(playerRenown - (Campaign.Current.Models.ClanTierModel is DefaultClanTierModel clanTierModel ? FieldAccessHelper.DCTMTierLowerRenownLimitsByRef()[6] : 6000), 0) * 10
            };
        }

        private static int GetMaxItemValueForElitePrize() =>
            Clan.PlayerClan.Tier switch
            {
                < 3 => 50000,
                3 => 200000,
                4 => 300000,
                5 => 400000,
                _ => 500000,
            };

        private static bool IsSuitablePrize(ItemObject itemObject, CultureObject? requiredCulture) =>
            (itemObject.Tier is ItemObject.ItemTiers.Tier5 or ItemObject.ItemTiers.Tier6)
            && itemObject.Value <= GetMaxItemValueForElitePrize()
            && (requiredCulture is null || itemObject.Culture is null || itemObject.Culture == requiredCulture || !itemObject.Culture.CanHaveSettlement)
            && !itemObject.NotMerchandise
            && (itemObject.IsCraftedWeapon || itemObject.IsMountable || itemObject.ArmorComponent != null)
            && !itemObject.IsCraftedByPlayer;

        private static bool IsUniqueItemObject(ItemObject itemObject, CultureObject? requiredCulture) =>
            itemObject.NotMerchandise && (int)itemObject.Tier >= 2 && !itemObject.IsCraftedByPlayer
            && (requiredCulture is null || itemObject.Culture is null || itemObject.Culture == requiredCulture || !itemObject.Culture.CanHaveSettlement);

        private static bool IsUniqueWeapon(ItemObject itemObject, CultureObject? requiredCulture) =>
            IsUniqueItemObject(itemObject, requiredCulture)
            && itemObject.IsCraftedWeapon
            && !itemObject.StringId.EndsWith("_blunt")
            && !itemObject.StringId.StartsWith("practice_")
            && !itemObject.StringId.StartsWith("peasant_");

        private static bool IsUniqueArmor(ItemObject itemObject, CultureObject? requiredCulture) =>
            IsUniqueItemObject(itemObject, requiredCulture)
            && itemObject.ArmorComponent != null
            && !itemObject.StringId.StartsWith("dummy_")
            && itemObject.StringId != "celtic_frost" && itemObject.StringId != "saddle_of_aeneas" && itemObject.StringId != "fortunas_choice";

        private static bool IsUniqueMount(ItemObject itemObject, CultureObject? requiredCulture) => 
            IsUniqueItemObject(itemObject, requiredCulture) && itemObject.IsMountable;

        private static void FilterByCulture(MBList<ItemObject> list, CultureObject townCulture, bool ignoreMounts = false, bool ignoreEverythingButBanners = false)
        {
            if (list.Count > 0)
            {
                var filteredList = list.Where(x => x.Culture is null || !x.Culture.CanHaveSettlement || x.Culture == townCulture || (ignoreMounts && x.IsMountable) || (ignoreEverythingButBanners && !x.HasBannerComponent)).ToList();
                filteredList.Sort((x, y) => x.Value.CompareTo(y.Value));
                list.Clear();
                list.AddRange(filteredList);
            }
        }

        /* Equipment service methods */

        private static Equipment? GetRandomPracticeClothes()
        {
            if (CampaignMission.Current is not { } misson || misson.Mode != MissionMode.Battle || Settlement.CurrentSettlement is not { } settlement || AOArenaBehaviorManager.Instance!.PracticeMode != ArenaPracticeMode.Team)
            {
                return null;
            }

            var settlementCultureId = settlement.MapFaction?.Culture?.StringId ?? "empire";
            var dummyCharacter = Game.Current.ObjectManager.GetObject<CharacterObject>("gear_team_practice_dummy_" + settlementCultureId);
            return dummyCharacter?.RandomBattleEquipment;
        }
    }
}