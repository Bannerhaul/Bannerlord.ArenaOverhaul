using ArenaOverhaul.Helpers;

using HarmonyLib;
using HarmonyLib.BUTR.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents;
using TaleWorlds.CampaignSystem.SandBox.Source.TournamentGames;
using TaleWorlds.Core;

namespace ArenaOverhaul.Patches
{
    [HarmonyPatch(typeof(FightTournamentGame))]
    public static class FightTournamentGamePatch
    {
        private static readonly MethodInfo miGetRegularRewardItemMinValue = AccessTools.Method(typeof(FightTournamentGamePatch), "GetRegularRewardItemMinValue");
        private static readonly MethodInfo miGetRegularRewardItemMaxValue = AccessTools.Method(typeof(FightTournamentGamePatch), "GetRegularRewardItemMaxValue");
        private static readonly MethodInfo miAddPlayerParyToParticipantCharacters = AccessTools.Method(typeof(FightTournamentGamePatch), "AddPlayerParyToParticipantCharacters");

        private static readonly MethodInfo miListAdd = AccessTools.Method(typeof(List<CharacterObject>), "Add");

        private static readonly MethodInfo miCanNpcJoinTournament = AccessTools.Method(typeof(FightTournamentGame), "CanNpcJoinTournament");
        private delegate bool CanNpcJoinTournamentDelegate(FightTournamentGame instance, Hero hero, List<CharacterObject> participantCharacters, bool considerSkills);
        private static readonly CanNpcJoinTournamentDelegate? deCanNpcJoinTournament = AccessTools2.GetDelegate<CanNpcJoinTournamentDelegate>(miCanNpcJoinTournament);

        [HarmonyTranspiler]
        [HarmonyPatch("GetParticipantCharacters")]
        public static IEnumerable<CodeInstruction> GetParticipantCharactersTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new(instructions);
            int numberOfEdits = 0;
            int includePlayerStartIndex = 0;
            int includePlayerEndIndex = 0;
            for (int i = 2; i < codes.Count; ++i)
            {
                if (numberOfEdits == 0 && codes[i].opcode == OpCodes.Ldloc_0 && codes[i - 1].opcode == OpCodes.Brfalse_S && codes[i - 2].opcode == OpCodes.Ldarg_2)
                {
                    includePlayerStartIndex = i;
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 1 && codes[i].Calls(miListAdd))
                {
                    includePlayerEndIndex = i;
                    ++numberOfEdits;
                    break;

                }
            }

            //Logging
            if (includePlayerStartIndex == 0 || includePlayerEndIndex == 0 || numberOfEdits < 2)
            {
                LogNoHooksIssue(includePlayerStartIndex, includePlayerEndIndex, numberOfEdits, codes);
                if (numberOfEdits < 2)
                {
                    MessageHelper.ErrorMessage("Harmony transpiler for FightTournamentGame. GetParticipantCharacters was not able to make all required changes!");
                }
            }
            if (includePlayerStartIndex > 0 && includePlayerEndIndex > 0)
            {
                codes.RemoveRange(includePlayerStartIndex, includePlayerEndIndex - includePlayerStartIndex + 1);                
                codes.InsertRange(includePlayerStartIndex, new CodeInstruction[] { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldloc_0), new CodeInstruction(opcode: OpCodes.Call, operand: miAddPlayerParyToParticipantCharacters) });
            }
            else
            {
                MessageHelper.ErrorMessage("Harmony transpiler for FightTournamentGame. GetParticipantCharacters could not find code hooks for player injection!");
            }

            return codes.AsEnumerable();

            //local methods
            static void LogNoHooksIssue(int includePlayerStartIndex, int includePlayerEndIndex, int numberOfEdits, List<CodeInstruction> codes)
            {
                LoggingHelper.Log("Indexes:", "Transpiler for FightTournamentGame.GetParticipantCharacters");
                StringBuilder issueInfo = new("");
                issueInfo.Append($"\tincludePlayerStartIndex={includePlayerStartIndex}.\n\tincludePlayerEndIndex={includePlayerEndIndex}.");
                issueInfo.Append($"\nNumberOfEdits: {numberOfEdits}");
                issueInfo.Append($"\nMethodInfos:");
                issueInfo.Append($"\n\tmiListAdd={(miListAdd != null ? miListAdd.ToString() : "not found")}");
                issueInfo.Append($"\n\tmiAddPlayerParyToParticipantCharacters={(miAddPlayerParyToParticipantCharacters != null ? miAddPlayerParyToParticipantCharacters.ToString() : "not found")}");
                LoggingHelper.LogILAndPatches(codes, issueInfo, MethodBase.GetCurrentMethod());
                LoggingHelper.Log(issueInfo.ToString());
            }
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

            if (Clan.PlayerClan.Tier <= 2)
            {
                return true;
            }

            if (FieldAccessHelper.FTGPossibleEliteRewardItemObjectsCacheByRef(__instance) == null)
            {
                FieldAccessHelper.FTGPossibleEliteRewardItemObjectsCacheByRef(__instance) = new List<ItemObject>();
            }
            List<ItemObject> itemObjectList = new();
            if (Clan.PlayerClan.Tier <= 5)
            {
                if (Clan.PlayerClan.Tier == 5)
                {
                    var itemObjectCandidates = Items.All.Where(itemObject => itemObject.Tier == ItemObject.ItemTiers.Tier6 && itemObject.Value <= 75000 && !itemObject.NotMerchandise && (itemObject.IsCraftedWeapon || itemObject.IsMountable || itemObject.ArmorComponent != null) && !itemObject.IsCraftedByPlayer).ToList();
                    itemObjectList.AddRange(itemObjectCandidates);
                    if (Settings.Instance!.EnableUnobtainablePrizes)
                    {
                        var uniqueitemObjects = Items.All.Where(itemObject => itemObject.Tier == ItemObject.ItemTiers.Tier6 && itemObject.NotMerchandise && itemObject.Value <= 75000 && (itemObject.IsCraftedWeapon || itemObject.IsMountable || itemObject.ArmorComponent != null) && !itemObject.IsCraftedByPlayer).ToList();
                        itemObjectList.AddRange(uniqueitemObjects);
                    }
                }
                else
                {
                    var itemObjectCandidates = Items.All.Where(itemObject => (int)itemObject.Tier == Clan.PlayerClan.Tier && !itemObject.NotMerchandise && (itemObject.IsCraftedWeapon || itemObject.IsMountable || itemObject.ArmorComponent != null) && !itemObject.IsCraftedByPlayer).ToList();
                    itemObjectList.AddRange(itemObjectCandidates);
                }
            }
            else
            {
                //Get top 3 most valued Tier6 items by type and culture
                var itemObjectCandidates =
                    Items.All.Where(itemObject => itemObject.Tier == ItemObject.ItemTiers.Tier6 && !itemObject.NotMerchandise && (itemObject.IsCraftedWeapon || itemObject.IsMountable || itemObject.ArmorComponent != null) && !itemObject.IsCraftedByPlayer).ToList()
                    .GroupBy(itemObject => (itemObject.Type, itemObject.Culture))
                    .Select(accessGroup => (GroupKey: accessGroup.Key, TopThreeItems: accessGroup.OrderByDescending(subg => subg.Value).Take(3)))
                    .SelectMany(x => x.TopThreeItems.Select(y => (x.GroupKey, Item: y)))
                    .Select(x => x.Item).ToList();
                itemObjectList.AddRange(itemObjectCandidates);

                if (Settings.Instance!.EnableUnobtainablePrizes)
                {
                    var uniqueitemObjects = Items.All.Where(itemObject => itemObject.Tier == ItemObject.ItemTiers.Tier6 && itemObject.NotMerchandise && (itemObject.IsCraftedWeapon || itemObject.IsMountable || itemObject.ArmorComponent != null) && !itemObject.IsCraftedByPlayer).ToList();
                    itemObjectList.AddRange(uniqueitemObjects);
                }
            }

            if (FieldAccessHelper.FTGPossibleEliteRewardItemObjectsCacheByRef(__instance).IsEmpty())
            {
                FieldAccessHelper.FTGPossibleEliteRewardItemObjectsCacheByRef(__instance).AddRange(itemObjectList);
            }
            FieldAccessHelper.FTGPossibleEliteRewardItemObjectsCacheByRef(__instance).Sort((x, y) => x.Value.CompareTo(y.Value));
            return false;
        }

        /* service methods */
        internal static void AddPlayerParyToParticipantCharacters(FightTournamentGame instance, List<CharacterObject> participantCharacters)
        {
            if (Hero.MainHero.PartyBelongedTo?.Party?.MemberRoster is null)
            {
                return;
            }

            foreach (TroopRosterElement troopRosterElement in Hero.MainHero.PartyBelongedTo.Party.MemberRoster.GetTroopRoster())
            {
                if (troopRosterElement.Character.IsHero)
                {
                    if (troopRosterElement.Character.IsPlayerCharacter || deCanNpcJoinTournament!(instance, troopRosterElement.Character.HeroObject, participantCharacters, true))
                    {
                        participantCharacters.Add(troopRosterElement.Character);
                    }
                }
            }
        }

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
            int playerRenown = (int)Clan.PlayerClan.Renown;
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
    }
}
