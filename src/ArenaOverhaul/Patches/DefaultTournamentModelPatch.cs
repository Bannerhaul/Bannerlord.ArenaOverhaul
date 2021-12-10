using HarmonyLib;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using TaleWorlds.CampaignSystem.SandBox.GameComponents;

namespace ArenaOverhaul.Patches
{
    [HarmonyPatch(typeof(DefaultTournamentModel))]
    public static class DefaultTournamentModelPatch
    {
        private static readonly MethodInfo miGetRenownReward = AccessTools.Method(typeof(DefaultTournamentModelPatch), "GetRenownReward");

        [HarmonyTranspiler]
        [HarmonyPatch("GetRenownReward")]
        public static IEnumerable<CodeInstruction> GetRenownRewardTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new(instructions);
            for (int i = 0; i < codes.Count; ++i)
            {
                if (codes[i].LoadsConstant(3f) && codes[i + 1].opcode == OpCodes.Stloc_0)
                {
                    codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetRenownReward);
                    break;
                }
            }
            return codes.AsEnumerable();
        }

        /* service methods */
        internal static float GetRenownReward()
        {
            return Settings.Instance!.TournamentBaseRenownReward;
        }
    }
}
