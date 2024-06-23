using ArenaOverhaul.CampaignBehaviors.BehaviorManagers;
using ArenaOverhaul.Helpers;
using ArenaOverhaul.ModSettings;

using HarmonyLib;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using TaleWorlds.CampaignSystem.GameComponents;

namespace ArenaOverhaul.Patches
{
    [HarmonyPatch(typeof(DefaultCombatXpModel))]
    public static class DefaultCombatXpModelPatch
    {
        private static readonly MethodInfo? miGetTournamentXPRate = AccessTools.Method(typeof(DefaultCombatXpModelPatch), "GetTournamentXPRate");
        private static readonly MethodInfo? miGetPracticeFightXPRate = AccessTools.Method(typeof(DefaultCombatXpModelPatch), "GetPracticeFightXPRate");

        [HarmonyTranspiler]
        [HarmonyPatch("GetXpFromHit")]
        public static IEnumerable<CodeInstruction> GetXpFromHitTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = new(instructions);
            int numberOfEdits = 0;
            for (int i = 0; i < codes.Count; ++i)
            {
                if (numberOfEdits == 0 && codes[i].LoadsConstant(0.33f))
                {
                    codes[i].opcode = OpCodes.Call;
                    codes[i].operand = miGetTournamentXPRate;
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 1 && codes[i].LoadsConstant(1.0 / 16.0))
                {
                    codes[i].opcode = OpCodes.Call;
                    codes[i].operand = miGetPracticeFightXPRate;
                    ++numberOfEdits;
                    break;
                }
            }

            //Logging
            const int RequiredNumberOfEdits = 2;
            if (numberOfEdits < RequiredNumberOfEdits)
            {
                LoggingHelper.LogNoHooksIssue(codes, numberOfEdits, RequiredNumberOfEdits, __originalMethod, [], []);
            }
            return codes.AsEnumerable();
        }

        internal static double GetTournamentXPRate()
        {
            return Settings.Instance!.TournamentExperienceRate;
        }

        internal static double GetPracticeFightXPRate()
        {
            return AOArenaBehaviorManager.Instance!.GetPracticeExperienceRate();
        }
    }
}