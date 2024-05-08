using ArenaOverhaul.Helpers;
using ArenaOverhaul.Tournament;

using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;

namespace ArenaOverhaul.Patches
{
    [HarmonyPatch(typeof(TournamentGame))]
    public static class TournamentGamePatch
    {
        private static readonly MethodInfo miTownSetter = AccessTools.PropertySetter(typeof(TournamentGame), "Town");
        private static readonly MethodInfo miCreationTimeSetter = AccessTools.PropertySetter(typeof(TournamentGame), "CreationTime");
        private static readonly FieldInfo fiLastRecordedNobleCountForTournamentPrize = AccessTools.Field(typeof(TournamentGame), "_lastRecordedLordCountForTournamentPrize");

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(TournamentGame), MethodType.Constructor, [typeof(Town), typeof(ItemObject)])]
        public static IEnumerable<CodeInstruction> TournamentGameTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new(instructions);
            int numberOfEdits = 0;
            int initializeNobleCountIndexNew = 0;
            int initializeNobleCountIndexOldStart = 0;
            int initializeNobleCountIndexOldEnd = 0;
            for (int i = 0; i < codes.Count; ++i)
            {
                if (numberOfEdits == 0 && codes[i].Calls(miTownSetter))
                {
                    initializeNobleCountIndexNew = i;
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 1 && codes[i].opcode == OpCodes.Ldc_I4_0 && codes[i + 1].opcode == OpCodes.Ldarg_0 && codes[i + 2].opcode == OpCodes.Ldfld && (FieldInfo) codes[i + 2].operand == fiLastRecordedNobleCountForTournamentPrize)
                {
                    codes[i].opcode = OpCodes.Ldc_I4_1;
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 2 && codes[i].Calls(miCreationTimeSetter))
                {
                    initializeNobleCountIndexOldStart = i + 1;
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 3 && codes[i].opcode == OpCodes.Ret)
                {
                    initializeNobleCountIndexOldEnd = i;
                    ++numberOfEdits;
                    break;
                }
            }
            //Logging
            if (initializeNobleCountIndexNew == 0 || initializeNobleCountIndexOldStart == 0 || initializeNobleCountIndexOldEnd == 0 || numberOfEdits < 4)
            {
                LogNoHooksIssue(initializeNobleCountIndexNew, initializeNobleCountIndexOldStart, initializeNobleCountIndexOldEnd, numberOfEdits, codes);
                if (numberOfEdits < 4)
                {
                    MessageHelper.ErrorMessage("Harmony transpiler for TournamentGame. Constructor was not able to make all required changes!");
                }
            }
            if (initializeNobleCountIndexNew > 0 && initializeNobleCountIndexOldStart > 0 && initializeNobleCountIndexOldEnd > 0)
            {
                codes.RemoveRange(initializeNobleCountIndexOldStart, initializeNobleCountIndexOldEnd - initializeNobleCountIndexOldStart);
                codes.InsertRange(initializeNobleCountIndexNew + 1, [new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldc_I4_0), new CodeInstruction(opcode: OpCodes.Stfld, operand: fiLastRecordedNobleCountForTournamentPrize)]);
            }
            else
            {
                MessageHelper.ErrorMessage("Harmony transpiler for TournamentGame. Constructor could not find code hooks for counting nobles!");
            }

            return codes.AsEnumerable();

            //local methods
            static void LogNoHooksIssue(int initializeNobleCountIndexNew, int initializeNobleCountIndexOldStart, int initializeNobleCountIndexOldEnd, int numberOfEdits, List<CodeInstruction> codes)
            {
                LoggingHelper.Log("Indexes:", "Transpiler for TournamentGame.Constructor");
                StringBuilder issueInfo = new("");
                issueInfo.Append($"\tinitializeNobleCountIndexNew={initializeNobleCountIndexNew}.\n\tinitializeNobleCountIndexOldStart={initializeNobleCountIndexOldStart}.\n\tinitializeNobleCountIndexOldEnd={initializeNobleCountIndexOldEnd}");
                issueInfo.Append($"\nNumberOfEdits: {numberOfEdits}");
                issueInfo.Append($"\nMethodInfos:");
                issueInfo.Append($"\n\tmiTownSetter={(miTownSetter != null ? miTownSetter.ToString() : "not found")}");
                issueInfo.Append($"\n\tmiCreationTimeSetter={(miCreationTimeSetter != null ? miCreationTimeSetter.ToString() : "not found")}");
                issueInfo.Append($"\n\tfiLastRecordedNobleCountForTournamentPrize={(fiLastRecordedNobleCountForTournamentPrize != null ? fiLastRecordedNobleCountForTournamentPrize.ToString() : "not found")}");
                LoggingHelper.LogILAndPatches(codes, issueInfo, MethodBase.GetCurrentMethod()!);
                LoggingHelper.Log(issueInfo.ToString());
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("TournamentWinRenown", MethodType.Getter)]
        public static void OnTournamentEndPostfix(TournamentGame __instance, ref float __result)
        {
            Town tournamentTown = __instance.Town;
            __result += TournamentRewardManager.GetTakedownRenownReward(Hero.MainHero, tournamentTown);
        }
    }
}