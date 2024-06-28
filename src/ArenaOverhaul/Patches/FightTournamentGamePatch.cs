using ArenaOverhaul.Helpers;
using ArenaOverhaul.ModSettings;
using ArenaOverhaul.Tournament;

using HarmonyLib;
using HarmonyLib.BUTR.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace ArenaOverhaul.Patches
{
    [HarmonyPatch(typeof(FightTournamentGame))]
    public static class FightTournamentGamePatch
    {
        private static readonly MethodInfo? miTournamentPrizeGetter = AccessTools2.PropertyGetter(typeof(TournamentGame), "Prize");
        private static readonly MethodInfo? miItemObjectNameGetter = AccessTools2.PropertyGetter(typeof(ItemObject), "Name");

        private static readonly MethodInfo? miGetRegularRewardItemMinValue = AccessTools.Method(typeof(FightTournamentGamePatch), "GetRegularRewardItemMinValue");
        private static readonly MethodInfo? miGetRegularRewardItemMaxValue = AccessTools.Method(typeof(FightTournamentGamePatch), "GetRegularRewardItemMaxValue");
        private static readonly MethodInfo? miShouldPrizeBeRerolled = AccessTools.Method(typeof(FightTournamentGamePatch), "ShouldPrizeBeRerolled");

        private static readonly MethodInfo? miGetPrizeItemName = AccessTools.Method(typeof(TournamentRewardManager), "GetPrizeItemName");

        private static readonly FightTournamentApplicantManager _applicantManager = new FightTournamentApplicantManager();

        [HarmonyPrefix]
        [HarmonyPatch("GetParticipantCharacters")]
        public static bool GetParticipantCharactersPrefix(FightTournamentGame __instance, ref MBList<CharacterObject> __result, Settlement settlement, bool includePlayer = true)
        {
            __result = _applicantManager.GetParticipantCharacters(__instance, settlement, includePlayer);
            return false;
        }

        [HarmonyTranspiler]
        [HarmonyPatch("GetTournamentPrize")]
        public static IEnumerable<CodeInstruction> GetTournamentPrizeTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = new(instructions);
            int numberOfEdits = 0;
            int ldarg2Index = 0;
            int continueIndex = 0;
            for (int i = 2; i < codes.Count; ++i)
            {
                if (numberOfEdits == 0 && codes[i].opcode == OpCodes.Bne_Un_S && codes[i - 1].opcode == OpCodes.Ldloc_0 && codes[i - 2].opcode == OpCodes.Ldarg_2)
                {
                    ldarg2Index = i - 2;
                    continueIndex = i;
                    ++numberOfEdits;
                    break;
                }
            }

            //Logging
            const int RequiredNumberOfEdits = 1;
            if (ldarg2Index == 0 || continueIndex == 0 || numberOfEdits < RequiredNumberOfEdits || miShouldPrizeBeRerolled is null)
            {
                LoggingHelper.LogNoHooksIssue(
                    codes, numberOfEdits, RequiredNumberOfEdits, __originalMethod,
                    [
                        (nameof(ldarg2Index), ldarg2Index),
                        (nameof(continueIndex), continueIndex),
                    ],
                    [
                        (nameof(miShouldPrizeBeRerolled), miShouldPrizeBeRerolled)
                    ]);
            }
            if (ldarg2Index > 0 && continueIndex > 0)
            {
                codes[continueIndex].opcode = OpCodes.Brtrue;
                codes.InsertRange(continueIndex, [new CodeInstruction(opcode: OpCodes.Call, operand: miShouldPrizeBeRerolled)]);
            }
            else
            {
                MessageHelper.ErrorMessage("Harmony transpiler for FightTournamentGame. GetTournamentPrize could not find code hooks for applying reroll settings!");
            }

            return codes.AsEnumerable();
        }

        [HarmonyTranspiler]
        [HarmonyPatch("GetMenuText")]
        public static IEnumerable<CodeInstruction> GetMenuTextTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = new(instructions);
            int numberOfEdits = 0;
            int prizeItemNameStartIndex1 = 0;
            int prizeItemNameEndIndex1 = 0;
            int prizeItemNameStartIndex2 = 0;
            int prizeItemNameEndIndex2 = 0;
            if (miTournamentPrizeGetter != null && miItemObjectNameGetter != null)
            {
                for (int i = 2; i < codes.Count; ++i)
                {
                    if (numberOfEdits == 0 && codes[i].LoadsConstant("TOURNAMENT_PRIZE") && codes[i + 1].opcode == OpCodes.Ldarg_0 && codes[i + 2].Calls(miTournamentPrizeGetter) && codes[i + 3].Calls(miItemObjectNameGetter))
                    {
                        prizeItemNameStartIndex1 = i + 2;
                        prizeItemNameEndIndex1 = i + 3;
                        ++numberOfEdits;
                    }
                    else if (numberOfEdits == 1 && codes[i].LoadsConstant("TOURNAMENT_PRIZE") && codes[i + 1].opcode == OpCodes.Ldarg_0 && codes[i + 2].Calls(miTournamentPrizeGetter) && codes[i + 3].Calls(miItemObjectNameGetter))
                    {
                        prizeItemNameStartIndex2 = i + 2;
                        prizeItemNameEndIndex2 = i + 3;
                        ++numberOfEdits;
                        break;
                    }
                }
            }

            //Logging
            const int RequiredNumberOfEdits = 2;
            if (prizeItemNameStartIndex1 == 0 || prizeItemNameEndIndex1 == 0 || prizeItemNameStartIndex2 == 0 || prizeItemNameEndIndex2 == 0 || numberOfEdits < RequiredNumberOfEdits || miGetPrizeItemName is null)
            {
                LoggingHelper.LogNoHooksIssue(
                    codes, numberOfEdits, RequiredNumberOfEdits, __originalMethod,
                    [
                        (nameof(prizeItemNameStartIndex1), prizeItemNameStartIndex1),
                        (nameof(prizeItemNameEndIndex1), prizeItemNameEndIndex1),
                        (nameof(prizeItemNameStartIndex2), prizeItemNameStartIndex2),
                        (nameof(prizeItemNameEndIndex2), prizeItemNameEndIndex2)
                    ],
                    [
                        (nameof(miTournamentPrizeGetter), miTournamentPrizeGetter),
                        (nameof(miItemObjectNameGetter), miItemObjectNameGetter),
                        (nameof(miGetPrizeItemName), miGetPrizeItemName)
                    ]);
            }
            if (prizeItemNameStartIndex1 > 0 && prizeItemNameEndIndex1 > 0 && prizeItemNameStartIndex2 > 0 && prizeItemNameEndIndex2 > 0)
            {
                SetPrizeItemName(codes, prizeItemNameStartIndex2, prizeItemNameEndIndex2);
                SetPrizeItemName(codes, prizeItemNameStartIndex1, prizeItemNameEndIndex1);
            }
            else
            {
                MessageHelper.ErrorMessage("Harmony transpiler for FightTournamentGame. GetMenuText could not find code hooks for stating correct prize item name!");
            }

            return codes.AsEnumerable();

            //local methods
            static void SetPrizeItemName(List<CodeInstruction> codes, int prizeItemNameStartIndex, int prizeItemNameEndIndex)
            {
                codes.RemoveRange(prizeItemNameStartIndex, prizeItemNameEndIndex - prizeItemNameStartIndex + 1);
                codes.InsertRange(prizeItemNameStartIndex, [new CodeInstruction(opcode: OpCodes.Call, operand: miGetPrizeItemName)]);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("GetTournamentPrize")]
        public static void GetTournamentPrizePostfix(FightTournamentGame __instance, ref ItemObject? __result)
        {
            if (!Settings.Instance!.EnableHighQualityPrizes || TournamentRewardManager.CurrentPrizeHasRegisteredModifier(__instance.Town, __result))
            {
                return;
            }
            var itemModifier = TournamentRewardManager.GetRandomItemModifier(__result);
            TournamentRewardManager.RegisterPrizeModifier(__instance.Town, __result, itemModifier);
        }

        [HarmonyTranspiler]
        [HarmonyPatch("CachePossibleRegularRewardItems")]
        public static IEnumerable<CodeInstruction> CachePossibleRegularRewardItemsTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new(instructions);
            int num = 0;
            for (int i = 0; i < codes.Count; ++i)
            {
                if (num == 0 && codes[i].LoadsConstant(1600))
                {
                    codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetRegularRewardItemMinValue);
                    ++num;
                }
                else if (num == 1 && codes[i].LoadsConstant(5000))
                {
                    codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetRegularRewardItemMaxValue);
                    break;
                }
            }
            return codes.AsEnumerable();
        }

        [HarmonyPrefix]
        [HarmonyPatch("CachePossibleEliteRewardItems")]
        public static bool CachePossibleEliteRewardItemsPrefix(FightTournamentGame __instance) //Bool prefixes compete with each other and skip others, as well as original, if return false
        {
            if (!Settings.Instance!.EnableTournamentPrizeScaling)
            {
                return true;
            }

            if (Clan.PlayerClan.Tier < 2)
            {
                return true;
            }

            if (FieldAccessHelper.FTGPossibleEliteRewardItemObjectsCacheByRef(__instance) == null)
            {
                FieldAccessHelper.FTGPossibleEliteRewardItemObjectsCacheByRef(__instance) = [];
            }
            List<ItemObject> itemObjectList = [];

            var townCulture = __instance.Town.Culture;
            int culturalPrizesSelectedIndex = Settings.Instance!.CultureRestrictedTournamentPrizes.SelectedIndex;
            CultureObject? requiredCulture = culturalPrizesSelectedIndex >= 1 ? townCulture : null;
            List<ItemObject>? itemObjectCandidates = default;

            //Pick standard items with cultural restrictions
            if (requiredCulture != null)
            {
                //Get top 10 most valued standard items by type with town culture
                itemObjectCandidates =
                    Items.All
                    .Where(itemObject => IsSuitablePrize(itemObject, requiredCulture)).ToList()
                    .GroupBy(itemObject => (itemObject.Type))
                    .Select(accessGroup => (GroupKey: accessGroup.Key, TopTenItems: accessGroup.OrderByDescending(subg => subg.Value).Take(10)))
                    .SelectMany(x => x.TopTenItems.Select(y => (x.GroupKey, Item: y)))
                    .Select(x => x.Item).ToList();
            }

            //Another try without cultural restrictions
            if (itemObjectCandidates is null || itemObjectCandidates.Count <= 0)
            {
                //Get top 5 most valued standard items by type and culture
                itemObjectCandidates =
                    Items.All
                    .Where(itemObject => IsSuitablePrize(itemObject, null)).ToList()
                    .GroupBy(itemObject => (itemObject.Type, itemObject.Culture))
                    .Select(accessGroup => (GroupKey: accessGroup.Key, TopFiveItems: accessGroup.OrderByDescending(subg => subg.Value).Take(5)))
                    .SelectMany(x => x.TopFiveItems.Select(y => (x.GroupKey, Item: y)))
                    .Select(x => x.Item).ToList();
            }

            //Add standard items
            if (itemObjectCandidates != null)
            {
                itemObjectList.AddRange(itemObjectCandidates);
            }

            //Add unique weapons and armors
            requiredCulture = culturalPrizesSelectedIndex >= 2 ? townCulture : null;

            var uniqueWeapons = Items.All.Where(itemObject => IsUniqueWeapon(itemObject, requiredCulture)).ToList();
            itemObjectList.AddRange(uniqueWeapons);

            var uniqueArmors = Items.All.Where(itemObject => IsUniqueArmor(itemObject, requiredCulture)).ToList();
            itemObjectList.AddRange(uniqueArmors);

            //Add unique mounts
            requiredCulture = culturalPrizesSelectedIndex == 3 ? townCulture : null;

            var uniqueMounts = Items.All.Where(itemObject => IsUniqueMount(itemObject, requiredCulture)).ToList();
            itemObjectList.AddRange(uniqueMounts);

            //Save list in TW class
            if (FieldAccessHelper.FTGPossibleEliteRewardItemObjectsCacheByRef(__instance).IsEmpty())
            {
                FieldAccessHelper.FTGPossibleEliteRewardItemObjectsCacheByRef(__instance).AddRange(itemObjectList);
            }
            FieldAccessHelper.FTGPossibleEliteRewardItemObjectsCacheByRef(__instance).Sort((x, y) => x.Value.CompareTo(y.Value));
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("CachePossibleEliteRewardItems")]
        public static void CachePossibleEliteRewardItemsPostfix(FightTournamentGame __instance)
        {
            if (Settings.Instance!.EnableTournamentPrizeScaling || Settings.Instance!.CultureRestrictedTournamentPrizes.SelectedIndex <= 1 || __instance.Town.Culture is not CultureObject townCulture)
            {
                return;
            }

            var ignoreMounts = Settings.Instance!.CultureRestrictedTournamentPrizes.SelectedIndex == 2;
            var list = FieldAccessHelper.FTGPossibleEliteRewardItemObjectsCacheByRef(__instance);
            FilterByCulture(list, townCulture, ignoreMounts);
        }

        [HarmonyPostfix]
        [HarmonyPatch("CachePossibleBannerItems")]
        public static void CachePossibleBannerItemsPostfix(FightTournamentGame __instance, bool isElite)
        {
            if (isElite)
            {
                //We have to fix Native bug with banner sorting regardless of settings
                var list = FieldAccessHelper.FTGPossibleEliteRewardItemObjectsCacheByRef(__instance);
                if (Settings.Instance!.CultureRestrictedTournamentPrizes.SelectedIndex >= 1 && __instance.Town.Culture is CultureObject townCulture)
                {
                    FilterByCulture(list, townCulture, ignoreEverythingButBanners: true);
                }
                else
                {
                    list.Sort((x, y) => x.Value.CompareTo(y.Value));
                }
            }
            else
            {
                if (Settings.Instance!.CultureRestrictedTournamentPrizes.SelectedIndex < 1 || __instance.Town.Culture is not CultureObject townCulture)
                {
                    return;
                }
                var list = FieldAccessHelper.FTGPossibleBannerRewardItemObjectsCacheByRef(__instance);
                FilterByCulture(list, townCulture);
            }
        }

        /* service methods */
        private static void FilterByCulture(List<ItemObject> list, CultureObject townCulture, bool ignoreMounts = false, bool ignoreEverythingButBanners = false)
        {
            if (list.Count > 0)
            {
                var filteredList = list.Where(x => x.Culture is null || !x.Culture.CanHaveSettlement || x.Culture == townCulture || (ignoreMounts && x.IsMountable) || (ignoreEverythingButBanners && !x.HasBannerComponent)).ToList();
                filteredList.Sort((x, y) => x.Value.CompareTo(y.Value));
                list.Clear();
                list.AddRange(filteredList);
            }
        }

        private static bool IsSuitablePrize(ItemObject itemObject, CultureObject? requiredCulture) =>
            (itemObject.Tier is ItemObject.ItemTiers.Tier5 or ItemObject.ItemTiers.Tier6)
            && itemObject.Value <= GetMaxItemValueForElitePrize()
            && (requiredCulture is null || itemObject.Culture is null || itemObject.Culture == requiredCulture || !itemObject.Culture.CanHaveSettlement)
            && !itemObject.NotMerchandise
            && (itemObject.IsCraftedWeapon || itemObject.IsMountable || itemObject.ArmorComponent != null)
            && !itemObject.IsCraftedByPlayer;

        private static bool IsUniqueItemObject(ItemObject itemObject, CultureObject? requiredCulture) =>
            itemObject.NotMerchandise && (int) itemObject.Tier >= 2 && !itemObject.IsCraftedByPlayer
            && (requiredCulture is null || itemObject.Culture is null || itemObject.Culture == requiredCulture || !itemObject.Culture.CanHaveSettlement);

        private static bool IsUniqueWeapon(ItemObject itemObject, CultureObject? requiredCulture) =>
            IsUniqueItemObject(itemObject, requiredCulture)
            && itemObject.IsCraftedWeapon
            //Remove practice weapons
            && !itemObject.StringId.EndsWith("_blunt")
            && !itemObject.StringId.StartsWith("practice_")
            //Remove cheap weapons
            && !itemObject.StringId.StartsWith("peasant_");

        private static bool IsUniqueArmor(ItemObject itemObject, CultureObject? requiredCulture) =>
            IsUniqueItemObject(itemObject, requiredCulture)
            && itemObject.ArmorComponent != null
            //Remove practice armors
            && !itemObject.StringId.StartsWith("dummy_")
            //Remove magic armors
            && itemObject.StringId != "celtic_frost" && itemObject.StringId != "saddle_of_aeneas" && itemObject.StringId != "fortunas_choice";

        private static bool IsUniqueMount(ItemObject itemObject, CultureObject? requiredCulture) => IsUniqueItemObject(itemObject, requiredCulture) && itemObject.IsMountable;

        internal static bool ShouldPrizeBeRerolled(int lastRecordedNobleCountForTournamentPrize, int participantingNoblesCount) =>
            Settings.Instance!.TournamentPrizeRerollCondition.SelectedIndex switch
            {
                0 => false, //Never
                1 => lastRecordedNobleCountForTournamentPrize < 4 && participantingNoblesCount >= 4, //When prize tier can be improved
                2 => lastRecordedNobleCountForTournamentPrize < participantingNoblesCount, //When chances for better prize are improved
                3 => lastRecordedNobleCountForTournamentPrize != participantingNoblesCount, //When situation changed
                _ => true,
            };

        internal static int GetRegularRewardItemMinValue()
        {
            return Settings.Instance!.EnableTournamentPrizeScaling ? 1600 + GetRenownRegularRewardItemValueIncrease(true) : 1600;
        }

        internal static int GetRegularRewardItemMaxValue()
        {
            return Settings.Instance!.EnableTournamentPrizeScaling ? 5000 + GetRenownRegularRewardItemValueIncrease() : 5000;
        }

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
    }
}