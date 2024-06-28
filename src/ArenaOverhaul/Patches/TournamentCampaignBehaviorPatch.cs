using ArenaOverhaul.Helpers;

using HarmonyLib;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.TournamentGames;

namespace ArenaOverhaul.Patches
{
    [HarmonyPatch(typeof(TournamentCampaignBehavior))]
    public static class TournamentCampaignBehaviorPatch
    {
        private static readonly MethodInfo? miAddRenown = AccessTools.Method(typeof(Clan), "AddRenown");

        [HarmonyTranspiler]
        [HarmonyPatch("OnTournamentFinished")]
        public static IEnumerable<CodeInstruction> EndCurrentMatchTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = new(instructions);
            int numberOfEdits = 0;
            int renownAwardStartIndex = 0;
            int renownAwardEndIndex = 0;
            if (miAddRenown != null)
            {
                for (int i = 1; i < codes.Count; ++i)
                {
                    if (numberOfEdits == 0 && codes[i].opcode == OpCodes.Ldarg_1 && codes[i - 1].opcode == OpCodes.Brfalse_S && codes[i + 3].opcode != OpCodes.Brfalse_S)
                    {
                        renownAwardStartIndex = i;
                        ++numberOfEdits;
                    }
                    else if (numberOfEdits == 1 && codes[i].Calls(miAddRenown))
                    {
                        renownAwardEndIndex = i;
                        ++numberOfEdits;
                        break;
                    }
                }
            }

            //Logging
            const int RequiredNumberOfEdits = 2;
            if (renownAwardStartIndex == 0 || renownAwardEndIndex == 0 || numberOfEdits < RequiredNumberOfEdits)
            {
                LoggingHelper.LogNoHooksIssue(
                    codes, numberOfEdits, RequiredNumberOfEdits, __originalMethod,
                    [
                        (nameof(renownAwardStartIndex), renownAwardStartIndex),
                        (nameof(renownAwardEndIndex), renownAwardEndIndex),
                    ],
                    [
                        (nameof(miAddRenown), miAddRenown),
                    ]);
                MessageHelper.ErrorMessage("Harmony transpiler for TournamentCampaignBehavior. OnTournamentFinished could not find code hooks for removing surplus renown reward!");
            }
            else
            {
                codes.RemoveRange(renownAwardStartIndex, renownAwardEndIndex - renownAwardStartIndex + 1);
            }

            return codes.AsEnumerable();
        }
    }
}