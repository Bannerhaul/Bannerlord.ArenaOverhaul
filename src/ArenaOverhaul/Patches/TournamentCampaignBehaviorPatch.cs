using ArenaOverhaul.Helpers;

using HarmonyLib;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.Source.TournamentGames;

namespace ArenaOverhaul.Patches
{
#if !e165
    [HarmonyPatch(typeof(TournamentCampaignBehavior))]
    public static class TournamentCampaignBehaviorPatch
    {
        private static readonly MethodInfo miAddRenown = AccessTools.Method(typeof(Clan), "AddRenown");

        [HarmonyTranspiler]
        [HarmonyPatch("OnTournamentFinished")]
        public static IEnumerable<CodeInstruction> EndCurrentMatchTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new(instructions);
            int renownAwardStartIndex = 0;
            int renownAwardEndIndex = 0;
            for (int i = 1; i < codes.Count; ++i)
            {
                if (renownAwardStartIndex == 0 && codes[i].opcode == OpCodes.Ldarg_1 && codes[i - 1].opcode == OpCodes.Brfalse_S && codes[i + 3].opcode != OpCodes.Brfalse_S)
                {
                    renownAwardStartIndex = i;
                }
                else if (renownAwardStartIndex > 0 && renownAwardEndIndex == 0 && codes[i].Calls(miAddRenown))
                {
                    renownAwardEndIndex = i;
                    break;
                }
            }
            //Logging
            if (renownAwardStartIndex == 0 || renownAwardEndIndex == 0)
            {
                LogNoHooksIssue(renownAwardStartIndex, renownAwardEndIndex, codes);
                MessageHelper.ErrorMessage("Harmony transpiler for TournamentCampaignBehavior. OnTournamentFinished could not find code hooks for removing surplus renown reward!");
            }
            else
            {
                codes.RemoveRange(renownAwardStartIndex, renownAwardEndIndex - renownAwardStartIndex + 1);
            }

            return codes.AsEnumerable();

            //local methods
            static void LogNoHooksIssue(int renownAwardStartIndex, int renownAwardEndIndex, List<CodeInstruction> codes)
            {
                LoggingHelper.Log("Indexes:", "Transpiler for TournamentCampaignBehavior.OnTournamentFinished");
                StringBuilder issueInfo = new("");
                issueInfo.Append($"\trenownAwardStartIndex={renownAwardStartIndex}.\n\trenownAwardEndIndex={renownAwardEndIndex}.");
                issueInfo.Append($"\nMethodInfos:");
                issueInfo.Append($"\n\tmiAddRenown={(miAddRenown != null ? miAddRenown.ToString() : "not found")}");
                LoggingHelper.LogILAndPatches(codes, issueInfo, MethodBase.GetCurrentMethod());
                LoggingHelper.Log(issueInfo.ToString());
            }
        }
    }
#endif
}