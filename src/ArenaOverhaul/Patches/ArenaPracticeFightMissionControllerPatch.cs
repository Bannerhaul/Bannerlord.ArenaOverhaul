using ArenaOverhaul.CampaignBehaviors;
using ArenaOverhaul.Helpers;

using HarmonyLib;
using HarmonyLib.BUTR.Extensions;

using SandBox;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace ArenaOverhaul.Patches
{
    [HarmonyPatch(typeof(ArenaPracticeFightMissionController))]
    public static class ArenaPracticeFightMissionControllerPatch
    {
        private static readonly MethodInfo miGetInitialParticipantsCount = AccessTools.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetInitialParticipantsCount");
        private static readonly MethodInfo miGetTotalParticipantsCount = AccessTools.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetTotalParticipantsCount");
        private static readonly MethodInfo miGetActiveOpponentCount = AccessTools.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetActiveOpponentCount");
        private static readonly MethodInfo miGetMinimumActiveOpponentCount = AccessTools.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetMinimumActiveOpponentCount");
        private static readonly MethodInfo miSpawnArenaAgents = AccessTools.Method(typeof(ArenaPracticeFightMissionControllerPatch), "SpawnArenaAgents");
        private static readonly MethodInfo miGetParticipantCharacters = AccessTools.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetParticipantCharacters");
        private static readonly MethodInfo miGetChosenEquipmentStage = AccessTools.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetChosenEquipmentStage");
        private static readonly MethodInfo miGetChosenEquipment = AccessTools.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetChosenEquipment");

        private static readonly MethodInfo miSelectRandomAiTeam = AccessTools.Method(typeof(ArenaPracticeFightMissionController), "SelectRandomAiTeam");
        private static readonly MethodInfo miGetSpawnFrame = AccessTools.Method(typeof(ArenaPracticeFightMissionController), "GetSpawnFrame");

        private delegate Agent SpawnArenaAgentDelegate(ArenaPracticeFightMissionController instance, Team team, MatrixFrame frame);
        private delegate Team SelectRandomAiTeamDelegate(ArenaPracticeFightMissionController instance);
        private delegate MatrixFrame GetSpawnFrameDelegate(ArenaPracticeFightMissionController instance, bool considerPlayerDistance, bool isInitialSpawn);

        private static readonly SpawnArenaAgentDelegate? deSpawnArenaAgent = AccessTools2.GetDelegate<SpawnArenaAgentDelegate>(typeof(ArenaPracticeFightMissionController), "SpawnArenaAgent");
        private static readonly SelectRandomAiTeamDelegate? deSelectRandomAiTeam = AccessTools2.GetDelegate<SelectRandomAiTeamDelegate>(typeof(ArenaPracticeFightMissionController), "SelectRandomAiTeam");
        private static readonly GetSpawnFrameDelegate? deGetSpawnFrame = AccessTools2.GetDelegate<GetSpawnFrameDelegate>(typeof(ArenaPracticeFightMissionController), "GetSpawnFrame");


        [HarmonyPostfix]
        [HarmonyPatch("RemainingOpponentCount", MethodType.Getter)]
        public static void RemainingOpponentCountPosfix(ref int __result, ArenaPracticeFightMissionController __instance)
        {
            __result = GetTotalParticipantsCount() - FieldAccessHelper.APFMCSpawnedOpponentAgentCountByRef(__instance) + FieldAccessHelper.APFMCAliveOpponentCountByRef(__instance);
        }

        [HarmonyTranspiler]
        [HarmonyPatch("OnMissionTick")]
        public static IEnumerable<CodeInstruction> OnMissionTickTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new(instructions);
            int numberOfEdits = 0;
            int spawnStartIndex = 0, spawnEndIndex = 0;
            for (int i = 0; i < codes.Count; ++i)
            {
                if (numberOfEdits == 0 && codes[i].opcode == OpCodes.Ldc_I4_6)
                {
                    codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetActiveOpponentCount);
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 1 && codes[i].opcode == OpCodes.Ldc_I4_S && (sbyte)codes[i].operand == 30)
                {
                    codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetTotalParticipantsCount);
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 2 && codes[i].opcode == OpCodes.Ldc_I4_2)
                {
                    codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetMinimumActiveOpponentCount);
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 3 && spawnStartIndex == 0 && codes[i - 1].opcode == OpCodes.Ldarg_0 && codes[i].Calls(miSelectRandomAiTeam))
                {
                    spawnStartIndex = i;
                }
                else if (numberOfEdits == 3 && spawnStartIndex > 0 && spawnEndIndex == 0 && codes[i].opcode == OpCodes.Stfld && codes[i].operand.ToString().Contains("_nextSpawnTime"))
                {
                    spawnEndIndex = i;
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 4 && codes[i].opcode == OpCodes.Ldc_I4_S && (sbyte)codes[i].operand == 30)
                {
                    codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetTotalParticipantsCount);
                    ++numberOfEdits;
                    break;
                }
            }
            //Logging
            if (spawnStartIndex == 0 || spawnEndIndex == 0 || numberOfEdits < 5)
            {
                LogNoHooksIssue(spawnStartIndex, spawnEndIndex, numberOfEdits, codes);
                if (numberOfEdits < 5)
                {
                    MessageHelper.ErrorMessage("Harmony transpiler for ArenaPracticeFightMissionController. OnMissionTick was not able to make all required changes!");
                }
            }
            if (spawnStartIndex > 0 && spawnEndIndex > 0)
            {
                codes.RemoveRange(spawnStartIndex, spawnEndIndex - spawnStartIndex + 1);
                codes.InsertRange(spawnStartIndex, new CodeInstruction[] { new CodeInstruction(opcode: OpCodes.Call, operand: miSpawnArenaAgents) });
            }
            else
            {
                MessageHelper.ErrorMessage("Harmony transpiler for ArenaPracticeFightMissionController. OnMissionTick could not find code hooks for SpawnArenaAgents action!");
            }

            return codes.AsEnumerable();

            //local methods
            static void LogNoHooksIssue(int spawnStartIndex, int spawnEndIndex, int numberOfEdits, List<CodeInstruction> codes)
            {
                LoggingHelper.Log("Indexes:", "Transpiler for ArenaPracticeFightMissionController.OnMissionTick");
                StringBuilder issueInfo = new("");
                issueInfo.Append($"\tspawnStartIndex = {spawnStartIndex}.\n\tspawnEndIndex={spawnEndIndex}.");
                issueInfo.Append($"\nNumberOfEdits: {numberOfEdits}");
                issueInfo.Append($"\nMethodInfos:");
                issueInfo.Append($"\n\tmiGetTotalParticipantsCount={(miGetTotalParticipantsCount != null ? miGetTotalParticipantsCount.ToString() : "not found")}");
                issueInfo.Append($"\n\tmiGetActiveOpponentCount={(miGetActiveOpponentCount != null ? miGetActiveOpponentCount.ToString() : "not found")}");
                issueInfo.Append($"\n\tmiGetMinimumActiveOpponentCount={(miGetMinimumActiveOpponentCount != null ? miGetMinimumActiveOpponentCount.ToString() : "not found")}");
                issueInfo.Append($"\n\tmiSpawnArenaAgents={(miSpawnArenaAgents != null ? miSpawnArenaAgents.ToString() : "not found")}");
                issueInfo.Append($"\n\tmiSelectRandomAiTeam={(miSelectRandomAiTeam != null ? miSelectRandomAiTeam.ToString() : "not found")}");
                issueInfo.Append($"\n\tmiGetSpawnFrame={(miGetSpawnFrame != null ? miGetSpawnFrame.ToString() : "not found")}");
                LoggingHelper.LogILAndPatches(codes, issueInfo, MethodBase.GetCurrentMethod());
                LoggingHelper.Log(issueInfo.ToString());
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch("AddRandomWeapons")]
        public static IEnumerable<CodeInstruction> AddRandomWeaponsTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new(instructions);
            int numberOfEdits = 0;
            int stageDefinitionStartIndex = -1, stageDefinitionEndIndex = -1;
            int equipmentDefinitionStartIndex = -1, equipmentDefinitionEndIndex = -1;
            for (int i = 0; i < codes.Count; ++i)
            {
                if (numberOfEdits == 0 && codes[i].opcode == OpCodes.Ldc_I4_1 && codes[i + 1].opcode == OpCodes.Ldarg_2)
                {
                    stageDefinitionStartIndex = i;
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 1 && codes[i].opcode == OpCodes.Stloc_0)
                {
                    stageDefinitionEndIndex = i - 1;
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 2 && codes[i].opcode == OpCodes.Ldloc_1)
                {
                    equipmentDefinitionStartIndex = i;
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 3 && codes[i].opcode == OpCodes.Stloc_2)
                {
                    equipmentDefinitionEndIndex = i - 1;
                    ++numberOfEdits;
                    break;
                }
            }
            //Logging
            if (stageDefinitionStartIndex < 0 || stageDefinitionEndIndex < 0 || equipmentDefinitionStartIndex < 0 || equipmentDefinitionEndIndex < 0 || numberOfEdits < 4)
            {
                LogNoHooksIssue(stageDefinitionStartIndex, stageDefinitionEndIndex, equipmentDefinitionStartIndex, equipmentDefinitionEndIndex, numberOfEdits, codes);
                if (numberOfEdits < 4)
                {
                    MessageHelper.ErrorMessage("Harmony transpiler for ArenaPracticeFightMissionController. AddRandomWeapons was not able to make all required changes!");
                }
            }
            if (stageDefinitionStartIndex >= 0 && stageDefinitionEndIndex > 0 && equipmentDefinitionStartIndex > 0 && equipmentDefinitionEndIndex > 0)
            {
                codes.RemoveRange(equipmentDefinitionStartIndex, equipmentDefinitionEndIndex - equipmentDefinitionStartIndex + 1);
                codes.InsertRange(equipmentDefinitionStartIndex, new CodeInstruction[] { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldarg_2), new CodeInstruction(OpCodes.Ldloc_1), new CodeInstruction(opcode: OpCodes.Call, operand: miGetChosenEquipment) });

                codes.RemoveRange(stageDefinitionStartIndex, stageDefinitionEndIndex - stageDefinitionStartIndex + 1);
                codes.InsertRange(stageDefinitionStartIndex, new CodeInstruction[] { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldarg_2), new CodeInstruction(opcode: OpCodes.Call, operand: miGetChosenEquipmentStage) });
            }
            else
            {
                MessageHelper.ErrorMessage("Harmony transpiler for ArenaPracticeFightMissionController. AddRandomWeapons could not find code hooks for loadout switching!");
            }

            return codes.AsEnumerable();

            //local methods
            static void LogNoHooksIssue(int stageDefinitionStartIndex, int stageDefinitionEndIndex, int equipmentDefinitionStartIndex, int equipmentDefinitionEndIndex, int numberOfEdits, List<CodeInstruction> codes)
            {
                LoggingHelper.Log("Indexes:", "Transpiler for ArenaPracticeFightMissionController.OnMissionTick");
                StringBuilder issueInfo = new("");
                issueInfo.Append($"\tstageDefinitionStartIndex={stageDefinitionStartIndex}.\n\tstageDefinitionEndIndex={stageDefinitionEndIndex}.");
                issueInfo.Append($"\n\tequipmentDefinitionStartIndex={equipmentDefinitionStartIndex}.\n\tequipmentDefinitionEndIndex={equipmentDefinitionEndIndex}.");
                issueInfo.Append($"\nNumberOfEdits: {numberOfEdits}");
                issueInfo.Append($"\nMethodInfos:");
                issueInfo.Append($"\n\tmiGetChosenEquipmentStage={(miGetChosenEquipmentStage != null ? miGetChosenEquipmentStage.ToString() : "not found")}");
                issueInfo.Append($"\n\tmiGetChosenEquipment={(miGetChosenEquipment != null ? miGetChosenEquipment.ToString() : "not found")}");
                LoggingHelper.LogILAndPatches(codes, issueInfo, MethodBase.GetCurrentMethod());
                LoggingHelper.Log(issueInfo.ToString());
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch("InitializeParticipantCharacters")]
        public static IEnumerable<CodeInstruction> InitializeParticipantCharactersTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return TotalParticipantsCountTranspiler(instructions, 1);
        }

        [HarmonyTranspiler]
        [HarmonyPatch("StartPractice")]
        public static IEnumerable<CodeInstruction> StartPracticeTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new(instructions);
            for (int i = 0; i < codes.Count; ++i)
            {
                if (codes[i].opcode == OpCodes.Ldc_I4_6)
                {
                    codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetInitialParticipantsCount);
                    break;
                }
            }
            return codes.AsEnumerable();
        }

        [HarmonyTranspiler]
        [HarmonyPatch("GetParticipantCharacters")]
        public static IEnumerable<CodeInstruction> GetParticipantCharactersTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new(instructions);
            for (int i = 0; i < codes.Count; ++i)
            {
                if (codes[i + 1].opcode == OpCodes.Stloc_0 && codes[i].opcode == OpCodes.Newobj)
                {
                    codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetParticipantCharacters);
                    break;
                }
            }
            return codes.AsEnumerable();
        }

        [HarmonyPostfix]
        [HarmonyPatch("EnemyHitReward")]
        public static void EnemyHitRewardPostfix(Agent affectedAgent, Agent affectorAgent, float lastSpeedBonus, float lastShotDifficulty, WeaponComponentData? attackerWeapon, float hitpointRatio, float damageAmount, ArenaPracticeFightMissionController __instance)
        {
            if (affectedAgent.Origin == null || affectorAgent == null || affectorAgent.Origin == null || !IsExpansivePractice() || !Settings.Instance!.EnableViewerExperienceGain)
            {
                return;
            }

            PartyBase party = Hero.MainHero.PartyBelongedTo.Party;
            CharacterObject affectorCharacter = (CharacterObject)affectorAgent.Character;

            bool affectorIsAliedHero = affectorCharacter.IsPlayerCharacter || (affectorCharacter.IsHero && party.MemberRoster.Contains(affectorCharacter));
            if (!affectorIsAliedHero)
            {
                return;
            }

            float xpAmount = affectedAgent.Health < 1.0 ? 10f : 2f;
            SkillObject? relevantSkill = attackerWeapon?.RelevantSkill;
            foreach (TroopRosterElement troopRosterElement in party.MemberRoster.GetTroopRoster())
            {
                if (!troopRosterElement.Character.IsHero && !troopRosterElement.Character.IsPlayerCharacter)
                {
                    party.MemberRoster.AddXpToTroop((int)xpAmount, troopRosterElement.Character);
                }
                else if (relevantSkill is not null && troopRosterElement.Character.IsHero && !troopRosterElement.Character.IsPlayerCharacter && !FieldAccessHelper.APFMCParticipantAgentsByRef(__instance).Select(a => a.Character).Contains(troopRosterElement.Character))
                {
                    Hero heroObject = troopRosterElement.Character.HeroObject;
                    if (affectorCharacter.HeroObject.GetSkillValue(relevantSkill) > heroObject.GetSkillValue(relevantSkill))
                    {
                        heroObject.AddSkillXp(relevantSkill, xpAmount);
                    }
                }
            }
        }

        /* service methods */
        internal static int GetInitialParticipantsCount()
        {
            return IsExpansivePractice() ? Settings.Instance!.ExpansivePracticeInitialParticipants : Settings.Instance!.PracticeInitialParticipants;// ? 7 : 6;
        }

        internal static int GetTotalParticipantsCount()
        {
            return IsExpansivePractice() ? Settings.Instance!.ExpansivePracticeTotalParticipants : Settings.Instance!.PracticeTotalParticipants;// ? 90 : 30;
        }

        internal static int GetActiveOpponentCount()
        {
            return IsExpansivePractice() ? Settings.Instance!.ExpansivePracticeActiveParticipants : Settings.Instance!.PracticeActiveParticipants;// ? 12 : 6;
        }

        internal static int GetMinimumActiveOpponentCount()
        {
            return IsExpansivePractice() ? Settings.Instance!.ExpansivePracticeActiveParticipantsMinimum : Settings.Instance!.PracticeActiveParticipantsMinimum;// ? 4 : 2;
        }

        internal static int GetChosenEquipmentStage(ArenaPracticeFightMissionController instance, int spawnIndex)
        {
            return IsPlayerSpawn(instance, spawnIndex) ? Campaign.Current.CampaignBehaviorManager.GetBehavior<AOArenaBehavior>()!.ChosenLoadoutStage : (1 + spawnIndex * 3 / GetTotalParticipantsCount());
        }

        internal static int GetChosenEquipment(ArenaPracticeFightMissionController instance, int spawnIndex, List<Equipment> equipmentList)
        {
            int chosenLoadout = Campaign.Current.CampaignBehaviorManager.GetBehavior<AOArenaBehavior>()!.ChosenLoadout;
            return IsPlayerSpawn(instance, spawnIndex) && chosenLoadout >= 0 ? chosenLoadout : MBRandom.RandomInt(equipmentList.Count);
        }

        internal static void SpawnArenaAgents(ArenaPracticeFightMissionController instance)
        {
            if (Campaign.Current.CampaignBehaviorManager.GetBehavior<AOArenaBehavior>()!.InExpansivePractice && IsUndercrowded())
            {
                int count = FieldAccessHelper.APFMCAIParticipantTeamsByRef(instance).Count;
                int num = 0;
                while (num < 5 && GetTotalParticipantsCount() > FieldAccessHelper.APFMCSpawnedOpponentAgentCountByRef(instance) + FieldAccessHelper.APFMCAliveOpponentCountByRef(instance) + num)
                {
                    FieldAccessHelper.APFMCParticipantAgentsByRef(instance).Add(deSpawnArenaAgent!(instance, deSelectRandomAiTeam!(instance), deGetSpawnFrame!(instance, true, false)));
                    ++num;
                }
            }
            else
            {
                FieldAccessHelper.APFMCParticipantAgentsByRef(instance).Add(deSpawnArenaAgent!(instance, deSelectRandomAiTeam!(instance), deGetSpawnFrame!(instance, true, false)));
            }
            FieldAccessHelper.APFMCNextSpawnTimeByRef(instance) = (float)(instance.Mission.CurrentTime + 12.0 - Math.Min(FieldAccessHelper.APFMCSpawnedOpponentAgentCountByRef(instance) / (GetTotalParticipantsCount() / 10), 11.0));

            bool IsUndercrowded() => FieldAccessHelper.APFMCNextSpawnTimeByRef(instance) > instance.Mission.CurrentTime;
        }

        internal static List<CharacterObject> GetParticipantCharacters()
        {
            List<CharacterObject> characterObjectList = new();
            if (!IsExpansivePractice())
            {
                return characterObjectList;
            }

            int num1 = 0;
            int num2 = 0;
            int num3 = 0;
            int maxParticipantCount = GetTotalParticipantsCount() * 2 / 3;
            if (characterObjectList.Count < maxParticipantCount)
            {
                int num4 = maxParticipantCount - characterObjectList.Count;
                foreach (TroopRosterElement troopRosterElement in Hero.MainHero.PartyBelongedTo.Party.MemberRoster.GetTroopRoster())
                {
                    if (troopRosterElement.Character == Hero.MainHero.CharacterObject)
                    {
                        continue;
                    }
                    else if (!characterObjectList.Contains(troopRosterElement.Character) && troopRosterElement.Character.IsHero)
                    {
                        characterObjectList.Add(troopRosterElement.Character);
                    }
                    else if (!characterObjectList.Contains(troopRosterElement.Character) && troopRosterElement.Character.Tier == 3 && num4 * 0.400000005960464 > num1)
                    {
                        characterObjectList.Add(troopRosterElement.Character);
                        ++num1;
                    }
                    else if (!characterObjectList.Contains(troopRosterElement.Character) && troopRosterElement.Character.Tier == 4 && num4 * 0.400000005960464 > num2)
                    {
                        characterObjectList.Add(troopRosterElement.Character);
                        ++num2;
                    }
                    else if (!characterObjectList.Contains(troopRosterElement.Character) && troopRosterElement.Character.Tier == 5 && num4 * 0.200000002980232 > num3)
                    {
                        characterObjectList.Add(troopRosterElement.Character);
                        ++num3;
                    }
                    if (characterObjectList.Count >= maxParticipantCount)
                    {
                        break;
                    }
                }
            }
            return characterObjectList;
        }

        private static bool IsPlayerSpawn(ArenaPracticeFightMissionController instance, int spawnIndex)
        {
            return instance.IsPlayerPracticing && spawnIndex == 0 && FieldAccessHelper.APFMCParticipantAgentsByRef(instance).IsEmpty();
        }

        private static IEnumerable<CodeInstruction> TotalParticipantsCountTranspiler(IEnumerable<CodeInstruction> instructions, int numberOfOccureneces)
        {
            List<CodeInstruction> codes = new(instructions);
            int num = 0;
            for (int i = 0; i < codes.Count; ++i)
            {
                if (num < numberOfOccureneces - 1 && codes[i].opcode == OpCodes.Ldc_I4_S && (sbyte)codes[i].operand == 30)
                {
                    codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetTotalParticipantsCount);
                    ++num;
                }
                else if (num == numberOfOccureneces - 1 && codes[i].opcode == OpCodes.Ldc_I4_S && (sbyte)codes[i].operand == 30)
                {
                    codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetTotalParticipantsCount);
                    break;
                }
            }
            return codes.AsEnumerable();
        }

        private static bool IsExpansivePractice() => Campaign.Current.CampaignBehaviorManager.GetBehavior<AOArenaBehavior>()!.InExpansivePractice;
    }
}
