using ArenaOverhaul.CampaignBehaviors;

using HarmonyLib;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Map;

namespace ArenaOverhaul.Patches
{
    [HarmonyPatch(typeof(DefaultCombatXpModel))]
    public static class DefaultCombatXpModelPatch
    {
        private static readonly MethodInfo miGetTournamentXPRate = AccessTools.Method(typeof(DefaultCombatXpModelPatch), "GetTournamentXPRate");
        private static readonly MethodInfo miGetPracticeFightXPRate = AccessTools.Method(typeof(DefaultCombatXpModelPatch), "GetPracticeFightXPRate");

        [HarmonyTranspiler]
        [HarmonyPatch("GetXpFromHit")]
        public static IEnumerable<CodeInstruction> GetXpFromHitTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            int num = 0;
            for (int i = 0; i < codes.Count; ++i)
            {
                if (num == 0 && codes[i].LoadsConstant(0.33f))
                {
                    codes[i].opcode = OpCodes.Call;
                    codes[i].operand = miGetTournamentXPRate;
                    ++num;
                }
                else if (num == 1 && codes[i].LoadsConstant(1.0 / 16.0))
                {
                    codes[i].opcode = OpCodes.Call;
                    codes[i].operand = miGetPracticeFightXPRate;
                    break;
                }
            }
            return codes.AsEnumerable();
        }

        internal static double GetTournamentXPRate()
        {
            return Settings.Instance!.TournamentExperienceRate;
            
        }

        internal static double GetPracticeFightXPRate()
        {
            return IsExpansivePractice() ? Settings.Instance!.ExpansivePracticeExperienceRate : Settings.Instance!.PracticeExperienceRate;
        }

        private static bool IsExpansivePractice() => Campaign.Current.CampaignBehaviorManager.GetBehavior<AOArenaBehavior>()!.InExpansivePractice;
    }
}
