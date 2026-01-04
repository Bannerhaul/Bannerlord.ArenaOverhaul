using ArenaOverhaul.Helpers;
using ArenaOverhaul.ModSettings;
using ArenaOverhaul.Tournament;

using HarmonyLib;
using HarmonyLib.BUTR.Extensions;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using TaleWorlds.CampaignSystem;
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

        /* service methods */
        internal static bool ShouldPrizeBeRerolled(int lastRecordedNobleCountForTournamentPrize, int participantingNoblesCount) =>
            Settings.Instance!.TournamentPrizeRerollCondition.SelectedIndex switch
            {
                0 => false, //Never
                1 => lastRecordedNobleCountForTournamentPrize < 4 && participantingNoblesCount >= 4, //When prize tier can be improved
                2 => lastRecordedNobleCountForTournamentPrize < participantingNoblesCount, //When chances for better prize are improved
                3 => lastRecordedNobleCountForTournamentPrize != participantingNoblesCount, //When situation changed
                _ => true,
            };
    }
}