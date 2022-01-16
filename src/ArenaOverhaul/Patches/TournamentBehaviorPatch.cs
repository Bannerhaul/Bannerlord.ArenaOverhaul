using ArenaOverhaul.Helpers;

using HarmonyLib;

using SandBox.TournamentMissions.Missions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace ArenaOverhaul.Patches
{
    [HarmonyPatch(typeof(TournamentBehavior))]
    public static class TournamentBehaviorPatch
    {
        private static readonly MethodInfo miGetMaximumBet = AccessTools.Method(typeof(TournamentBehaviorPatch), "GetMaximumBet");
        private static readonly MethodInfo miGetBettingOddRandomFactor = AccessTools.Method(typeof(TournamentBehaviorPatch), "GetBettingOddRandomFactor");
        private static readonly MethodInfo miGetNextRound = AccessTools.PropertyGetter(typeof(TournamentBehavior), "NextRound");
        private static readonly MethodInfo miUpdateRoundWinnings = AccessTools.Method(typeof(TournamentRewardManager), "UpdateRoundWinnings", new Type[] { typeof(TournamentBehavior) });
        private static readonly MethodInfo miGetMainHero = AccessTools.PropertyGetter(typeof(Hero), "MainHero");
        private static readonly MethodInfo miGetOverallExpectedDenars = AccessTools.PropertyGetter(typeof(TournamentBehavior), "OverallExpectedDenars");
        private static readonly MethodInfo miMathFPow = AccessTools.Method(typeof(MathF), "Pow", new Type[] { typeof(float), typeof(float) });

        [HarmonyTranspiler]
        [HarmonyPatch("GetMaximumBet")]
        public static IEnumerable<CodeInstruction> GetMaximumBetTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new(instructions);
            for (int i = 0; i < codes.Count; ++i)
            {
                if (codes[i].LoadsConstant(150))
                {
                    codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetMaximumBet);
                    break;
                }
            }
            return codes.AsEnumerable();
        }

        [HarmonyTranspiler]
        [HarmonyPatch("CalculateBet")]
        public static IEnumerable<CodeInstruction> CalculateBetTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new(instructions);
            for (int i = 0; i < codes.Count; ++i)
            {
                if (codes[i].LoadsConstant(1.1f) && codes[i - 1].Calls(miMathFPow))
                {
                    codes.InsertRange(i, new CodeInstruction[] { new CodeInstruction(opcode: OpCodes.Call, operand: miGetBettingOddRandomFactor), new CodeInstruction(OpCodes.Mul) });
                    break;
                }
            }
            return codes.AsEnumerable();
        }

        [HarmonyPostfix]
        [HarmonyPatch("AfterStart")]
        public static void AfterStartPostfix(TournamentBehavior __instance)
        {
            TournamentRewardManager.InitiateTournament(__instance.TournamentGame.Town);
        }

        [HarmonyTranspiler]
        [HarmonyPatch("EndCurrentMatch")]
        public static IEnumerable<CodeInstruction> EndCurrentMatchTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new(instructions);
            int updateRoundWinningsIndex = 0;
            for (int i = 0; i < codes.Count; ++i)
            {
                if (codes[i].opcode == OpCodes.Ldarg_0 && codes[i + 1].Calls(miGetNextRound))
                {
                    updateRoundWinningsIndex = i;
                    break;
                }
            }
            //Logging
            if (updateRoundWinningsIndex == 0)
            {
                LogNoHooksIssue(updateRoundWinningsIndex, codes);
                MessageHelper.ErrorMessage("Harmony transpiler for TournamentBehavior. EndCurrentMatch could not find code hooks adding round winnings!");
            }
            else
            {
                codes[updateRoundWinningsIndex].opcode = OpCodes.Nop;
                codes.InsertRange(updateRoundWinningsIndex + 1, new CodeInstruction[] { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(opcode: OpCodes.Call, operand: miUpdateRoundWinnings), new CodeInstruction(OpCodes.Ldarg_0) });
            }

            return codes.AsEnumerable();

            //local methods
            static void LogNoHooksIssue(int updateRoundWinningsIndex, List<CodeInstruction> codes)
            {
                LoggingHelper.Log("Indexes:", "Transpiler for TournamentBehavior.EndCurrentMatch");
                StringBuilder issueInfo = new("");
                issueInfo.Append($"\tupdateRoundWinningsIndex={updateRoundWinningsIndex}.");
                issueInfo.Append($"\nMethodInfos:");
                issueInfo.Append($"\n\tmiGetNextRound={(miGetNextRound != null ? miGetNextRound.ToString() : "not found")}");
                issueInfo.Append($"\n\tmiUpdateRoundWinnings={(miUpdateRoundWinnings != null ? miUpdateRoundWinnings.ToString() : "not found")}");
                LoggingHelper.LogILAndPatches(codes, issueInfo, MethodBase.GetCurrentMethod());
                LoggingHelper.Log(issueInfo.ToString());
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch("OnPlayerWinTournament")]
        public static IEnumerable<CodeInstruction> OnPlayerWinTournamentTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new(instructions);
            int numberOfEdits = 0;
            int renownAndInfluenceStartIndex = 0;
            int renownAndInfluenceEndIndex = 0;
            for (int i = 0; i < codes.Count; ++i)
            {
                if (numberOfEdits == 0 && codes[i].Calls(miGetMainHero) && codes[i - 1].opcode == OpCodes.Ret)
                {
                    codes[i].operand = null;
                    codes[i].opcode = OpCodes.Nop;
                    renownAndInfluenceStartIndex = i + 1;
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 1 && codes[i].opcode == OpCodes.Ldarg_0 && codes[i + 1].Calls(miGetOverallExpectedDenars))
                {
                    renownAndInfluenceEndIndex = i;
                    break;
                }
            }
            //Logging
            if (renownAndInfluenceStartIndex == 0 || renownAndInfluenceEndIndex == 0)
            {
                LogNoHooksIssue(renownAndInfluenceStartIndex, renownAndInfluenceEndIndex, codes);
                MessageHelper.ErrorMessage("Harmony transpiler for TournamentBehavior. OnPlayerWinTournament could not find code hooks for removing surplus rewards!");
            }
            else
            {
                codes.RemoveRange(renownAndInfluenceStartIndex, renownAndInfluenceEndIndex - renownAndInfluenceStartIndex);
            }

            return codes.AsEnumerable();

            //local methods
            static void LogNoHooksIssue(int renownAndInfluenceStartIndex, int renownAndInfluenceEndIndex, List<CodeInstruction> codes)
            {
                LoggingHelper.Log("Indexes:", "Transpiler for TournamentBehavior.OnPlayerWinTournament");
                StringBuilder issueInfo = new("");
                issueInfo.Append($"\trenownAndInfluenceStartIndex={renownAndInfluenceStartIndex}.");
                issueInfo.Append($"\trenownAndInfluenceEndIndex={renownAndInfluenceEndIndex}.");
                issueInfo.Append($"\nMethodInfos:");
                issueInfo.Append($"\n\tmiGetMainHero={(miGetMainHero != null ? miGetMainHero.ToString() : "not found")}");
                issueInfo.Append($"\n\tmiGetOverallExpectedDenars={(miGetOverallExpectedDenars != null ? miGetOverallExpectedDenars.ToString() : "not found")}");
                LoggingHelper.LogILAndPatches(codes, issueInfo, MethodBase.GetCurrentMethod());
                LoggingHelper.Log(issueInfo.ToString());
            }
        }

        /* service methods */
        internal static int GetMaximumBet()
        {
            return Settings.Instance!.TournamentMaximumBet;
        }

        internal static float GetBettingOddRandomFactor()
        {
            return Settings.Instance!.EnableRandomizedBettingOdds ? MBRandom.RandomFloatRanged(0.75f, 1.25f) : 1f;
        }
    }
}