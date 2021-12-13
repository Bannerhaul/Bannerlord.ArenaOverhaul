using HarmonyLib;

using SandBox;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using TaleWorlds.CampaignSystem.SandBox.Source.TournamentGames;
using TaleWorlds.MountAndBlade;

namespace ArenaOverhaul.Patches
{
    [HarmonyPatch(typeof(TournamentFightMissionController))]
    public static class TournamentFightMissionControllerPatch
    {
        private static readonly MethodInfo miListRemove = AccessTools.Method(typeof(List<TournamentParticipant>), "Remove");
        private static readonly MethodInfo miTupleItem2Getter = AccessTools.PropertyGetter(typeof(Tuple<float, float>), "Item2");
        private static readonly MethodInfo miUpdateNoticableTakedowns = AccessTools.Method(typeof(TournamentRewardManager), "UpdateNoticableTakedowns", new Type[] { typeof(TournamentParticipant), typeof(TournamentParticipant) });

        [HarmonyPostfix]
        [HarmonyPatch("EnemyHitReward")]
        public static void EnemyHitRewardPostfix(Agent affectedAgent, Agent affectorAgent/*, float lastSpeedBonus, float lastShotDifficulty, WeaponComponentData? lastAttackerWeapon, float hitpointRatio, float damageAmount, TournamentFightMissionController __instance*/)
        {
            if (affectedAgent.Origin == null || affectorAgent == null || affectorAgent.Origin == null)
            {
                return;
            }
            TournamentRewardManager.UpdateNoticableTakedowns(affectorAgent, affectedAgent);
        }

        [HarmonyTranspiler]
        [HarmonyPatch("Simulate")]
        public static IEnumerable<CodeInstruction> SimulateTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new(instructions);
            int num = 0;
            int aliveParticipant1LoadIndex = 0;
            for (int i = 4; i < codes.Count; ++i)
            {
                if (num == 0 && codes[i].opcode == OpCodes.Ldloc_S && codes[i - 1].opcode == OpCodes.Ldloc_1 && codes[i - 2].Calls(miTupleItem2Getter))
                {
                    aliveParticipant1LoadIndex = i;
                    ++num;
                }
                else if (num == 1 && codes[i].opcode == OpCodes.Ldarg_0 && codes[i - 1].opcode == OpCodes.Pop && codes[i - 2].Calls(miListRemove) && codes[i - 3].opcode == OpCodes.Ldloc_S)
                {
                    codes.InsertRange(i, new CodeInstruction[] { new CodeInstruction(opcode: OpCodes.Ldloc_S, operand: codes[aliveParticipant1LoadIndex].operand),
                                                                 new CodeInstruction(opcode: OpCodes.Ldloc_S, operand: codes[i - 3].operand),
                                                                 new CodeInstruction(opcode: OpCodes.Call, operand: miUpdateNoticableTakedowns) });
                    break;
                }
            }
            return codes.AsEnumerable();
        }
    }
}