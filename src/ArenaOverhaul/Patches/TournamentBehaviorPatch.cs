using ArenaOverhaul.Helpers;
using ArenaOverhaul.ModSettings;
using ArenaOverhaul.Tournament;

using HarmonyLib;

using SandBox.Tournaments.MissionLogics;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

using MathF = TaleWorlds.Library.MathF;

namespace ArenaOverhaul.Patches
{
    [HarmonyPatch(typeof(TournamentBehavior))]
    public static class TournamentBehaviorPatch
    {
        private static readonly MethodInfo? miGetMaximumBet = AccessTools.Method(typeof(TournamentBehaviorPatch), "GetMaximumBet");
        private static readonly MethodInfo? miGetBettingOddRandomFactor = AccessTools.Method(typeof(TournamentBehaviorPatch), "GetBettingOddRandomFactor");
        private static readonly MethodInfo? miGetNextRound = AccessTools.PropertyGetter(typeof(TournamentBehavior), "NextRound");
        private static readonly MethodInfo? miUpdateRoundWinnings = AccessTools.Method(typeof(TournamentRewardManager), "UpdateRoundWinnings", [typeof(TournamentBehavior)]);
        private static readonly MethodInfo? miGetMainHero = AccessTools.PropertyGetter(typeof(Hero), "MainHero");
        private static readonly MethodInfo? miGetOverallExpectedDenars = AccessTools.PropertyGetter(typeof(TournamentBehavior), "OverallExpectedDenars");
        private static readonly MethodInfo? miMathFPow = AccessTools.Method(typeof(MathF), "Pow", [typeof(float), typeof(float)]);

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
                    codes.InsertRange(i, [new CodeInstruction(opcode: OpCodes.Call, operand: miGetBettingOddRandomFactor), new CodeInstruction(OpCodes.Mul)]);
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
        public static IEnumerable<CodeInstruction> EndCurrentMatchTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = new(instructions);
            int updateRoundWinningsIndex = 0;
            if (miGetNextRound != null)
            {
                for (int i = 0; i < codes.Count; ++i)
                {
                    if (codes[i].opcode == OpCodes.Ldarg_0 && codes[i + 1].Calls(miGetNextRound))
                    {
                        updateRoundWinningsIndex = i;
                        break;
                    }
                }
            }
            //Logging
            if (updateRoundWinningsIndex == 0 || miUpdateRoundWinnings is null)
            {
                LoggingHelper.LogNoHooksIssue(
                    codes, 1, 1, __originalMethod,
                    [
                        (nameof(updateRoundWinningsIndex), updateRoundWinningsIndex),
                    ],
                    [
                        (nameof(miGetNextRound), miGetNextRound),
                        (nameof(miUpdateRoundWinnings), miUpdateRoundWinnings),
                    ]);
                MessageHelper.ErrorMessage("Harmony transpiler for TournamentBehavior. EndCurrentMatch could not find code hooks adding round winnings!");
            }
            else
            {
                codes[updateRoundWinningsIndex].opcode = OpCodes.Nop;
                codes.InsertRange(updateRoundWinningsIndex + 1, [new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(opcode: OpCodes.Call, operand: miUpdateRoundWinnings), new CodeInstruction(OpCodes.Ldarg_0)]);
            }

            return codes.AsEnumerable();
        }

        [HarmonyTranspiler]
        [HarmonyPatch("OnPlayerWinTournament")]
        public static IEnumerable<CodeInstruction> OnPlayerWinTournamentTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = new(instructions);
            int numberOfEdits = 0;
            int renownAndInfluenceStartIndex = 0;
            int renownAndInfluenceEndIndex = 0;
            if (miGetMainHero != null && miGetOverallExpectedDenars != null)
            {
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
                        ++numberOfEdits;
                        break;
                    }
                }
            }

            //Logging
            const int RequiredNumberOfEdits = 2;
            if (renownAndInfluenceStartIndex == 0 || renownAndInfluenceEndIndex == 0 || numberOfEdits < RequiredNumberOfEdits)
            {
                LoggingHelper.LogNoHooksIssue(
                    codes, numberOfEdits, RequiredNumberOfEdits, __originalMethod,
                    [
                        (nameof(renownAndInfluenceStartIndex), renownAndInfluenceStartIndex),
                        (nameof(renownAndInfluenceEndIndex), renownAndInfluenceEndIndex),
                    ],
                    [
                        (nameof(miGetMainHero), miGetMainHero),
                        (nameof(miGetOverallExpectedDenars), miGetOverallExpectedDenars)
                    ]);
                MessageHelper.ErrorMessage("Harmony transpiler for TournamentBehavior. OnPlayerWinTournament could not find code hooks for removing surplus rewards!");
            }
            else
            {
                codes.RemoveRange(renownAndInfluenceStartIndex, renownAndInfluenceEndIndex - renownAndInfluenceStartIndex);
            }

            return codes.AsEnumerable();
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