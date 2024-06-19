using ArenaOverhaul.ArenaPractice;
using ArenaOverhaul.CampaignBehaviors.BehaviorManagers;
using ArenaOverhaul.Helpers;
using ArenaOverhaul.ModSettings;
using ArenaOverhaul.Tournament;

using HarmonyLib;
using HarmonyLib.BUTR.Extensions;

using SandBox.Missions.MissionLogics.Arena;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace ArenaOverhaul.Patches
{
    //At this point of patch intensity, it might seem like it would be easier to just write a new variant of ArenaPracticeFightMissionController, but I've tried and turns out it would take a lot of effort to make the game work with it.
    //If some fellow modder is reading this and knows how to do it, I would appretiate the contribution!
    [HarmonyPatch(typeof(ArenaPracticeFightMissionController))]
    public static class ArenaPracticeFightMissionControllerPatch
    {
        private static readonly MethodInfo? miGetInitialParticipantsCount = AccessTools.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetInitialParticipantsCount");
        private static readonly MethodInfo? miGetTotalParticipantsCount = AccessTools.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetTotalParticipantsCount");
        private static readonly MethodInfo? miGetActiveOpponentCount = AccessTools.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetActiveOpponentCount");
        private static readonly MethodInfo? miGetMinimumActiveOpponentCount = AccessTools.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetMinimumActiveOpponentCount");
        private static readonly MethodInfo? miSpawnArenaAgents = AccessTools.Method(typeof(ArenaPracticeFightMissionControllerPatch), "SpawnArenaAgents");
        private static readonly MethodInfo? miGetParticipantCharactersLocal = AccessTools.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetPlayerRelatedParticipantCharacters");
        private static readonly MethodInfo? miFilterAvailableWeapons = AccessTools.Method(typeof(ArenaPracticeFightMissionControllerPatch), "FilterAvailableWeapons");
        private static readonly MethodInfo? miGetChosenEquipmentStage = AccessTools.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetChosenEquipmentStage");
        private static readonly MethodInfo? miGetChosenEquipment = AccessTools.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetChosenEquipment");
        private static readonly MethodInfo? miInitializeParticipantCharacters = AccessTools.Method(typeof(ArenaPracticeFightMissionControllerPatch), "InitializeParticipantCharacters");
        private static readonly MethodInfo? miGetAITeamsCount = AccessTools.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetAITeamsCount");
        private static readonly MethodInfo? miGetParticipantTeam = AccessTools.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetParticipantTeam");

        private static readonly MethodInfo? miSelectRandomAiTeam = AccessTools.Method(typeof(ArenaPracticeFightMissionController), "SelectRandomAiTeam");
        private static readonly MethodInfo? miGetParticipantCharacters = AccessTools.Method(typeof(ArenaPracticeFightMissionController), "GetParticipantCharacters");

        private delegate Agent SpawnArenaAgentDelegate(ArenaPracticeFightMissionController instance, Team team, MatrixFrame frame);
        private delegate Team SelectRandomAiTeamDelegate(ArenaPracticeFightMissionController instance);
        private delegate MatrixFrame GetSpawnFrameDelegate(ArenaPracticeFightMissionController instance, bool considerPlayerDistance, bool isInitialSpawn);

        private static readonly SpawnArenaAgentDelegate? deSpawnArenaAgent = AccessTools2.GetDelegate<SpawnArenaAgentDelegate>(typeof(ArenaPracticeFightMissionController), "SpawnArenaAgent");
        private static readonly SelectRandomAiTeamDelegate? deSelectRandomAiTeam = AccessTools2.GetDelegate<SelectRandomAiTeamDelegate>(typeof(ArenaPracticeFightMissionController), "SelectRandomAiTeam");
        private static readonly GetSpawnFrameDelegate? deGetSpawnFrame = AccessTools2.GetDelegate<GetSpawnFrameDelegate>(typeof(ArenaPracticeFightMissionController), "GetSpawnFrame");

        private static bool wasInitialSpawnLastTime = false;
        private static int spawnCounter = 0;

        [HarmonyPostfix]
        [HarmonyPatch("RemainingOpponentCount", MethodType.Getter)]
        public static void RemainingOpponentCountPosfix(ref int __result, ArenaPracticeFightMissionController __instance)
        {
            __result = GetTotalParticipantsCount() - FieldAccessHelper.APFMCSpawnedOpponentAgentCountByRef(__instance) + FieldAccessHelper.APFMCAliveOpponentCountByRef(__instance);
        }

        [HarmonyTranspiler]
        [HarmonyPatch("OnMissionTick")]
        public static IEnumerable<CodeInstruction> OnMissionTickTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = new(instructions);
            int numberOfEdits = 0;
            int spawnStartIndex = 0, spawnEndIndex = 0;
            for (int i = 0; i < codes.Count; ++i)
            {
                if (numberOfEdits == 0 && codes[i].opcode == OpCodes.Ldc_I4_6 && miGetActiveOpponentCount != null)
                {
                    codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetActiveOpponentCount);
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 1 && codes[i].opcode == OpCodes.Ldc_I4_S && (sbyte) codes[i].operand == 30 && miGetTotalParticipantsCount != null)
                {
                    codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetTotalParticipantsCount);
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 2 && codes[i].opcode == OpCodes.Ldc_I4_2 && codes[i + 1].opcode == OpCodes.Beq_S && miGetMinimumActiveOpponentCount != null)
                {
                    codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetMinimumActiveOpponentCount);
                    codes[i + 1].opcode = OpCodes.Ble_S;
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 3 && spawnStartIndex == 0 && codes[i - 1].opcode == OpCodes.Ldarg_0 && miSelectRandomAiTeam != null && codes[i].Calls(miSelectRandomAiTeam))
                {
                    spawnStartIndex = i;
                }
                else if (numberOfEdits == 3 && spawnStartIndex > 0 && spawnEndIndex == 0 && codes[i].opcode == OpCodes.Stfld && codes[i].operand.ToString()!.Contains("_nextSpawnTime"))
                {
                    spawnEndIndex = i;
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 4 && codes[i].opcode == OpCodes.Ldc_I4_S && (sbyte) codes[i].operand == 30 && miGetTotalParticipantsCount != null)
                {
                    codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetTotalParticipantsCount);
                    ++numberOfEdits;
                    break;
                }
            }
            //Logging
            const int RequiredNumberOfEdits = 5;
            if (spawnStartIndex == 0 || spawnEndIndex == 0 || numberOfEdits < RequiredNumberOfEdits || miSpawnArenaAgents is null)
            {
                LoggingHelper.LogNoHooksIssue(
                    codes, numberOfEdits, RequiredNumberOfEdits, __originalMethod,
                    [
                        (nameof(spawnStartIndex), spawnStartIndex),
                        (nameof(spawnEndIndex), spawnEndIndex),
                    ],
                    [
                        (nameof(miGetActiveOpponentCount), miGetActiveOpponentCount),
                        (nameof(miGetTotalParticipantsCount), miGetTotalParticipantsCount),
                        (nameof(miGetMinimumActiveOpponentCount), miGetMinimumActiveOpponentCount),
                        (nameof(miSelectRandomAiTeam), miSelectRandomAiTeam),
                        (nameof(miSpawnArenaAgents), miSpawnArenaAgents)
                    ]);
            }
            if (spawnStartIndex > 0 && spawnEndIndex > 0)
            {
                codes.RemoveRange(spawnStartIndex, spawnEndIndex - spawnStartIndex + 1);
                codes.InsertRange(spawnStartIndex, [new CodeInstruction(opcode: OpCodes.Call, operand: miSpawnArenaAgents)]);
            }
            else
            {
                MessageHelper.ErrorMessage("Harmony transpiler for ArenaPracticeFightMissionController. OnMissionTick could not find code hooks for SpawnArenaAgents action!");
            }

            return codes.AsEnumerable();
        }

        [HarmonyTranspiler]
        [HarmonyPatch("AddRandomWeapons")]
        public static IEnumerable<CodeInstruction> AddRandomWeaponsTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
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
            const int RequiredNumberOfEdits = 4;
            bool indexesNotFound = stageDefinitionStartIndex < 0 || stageDefinitionEndIndex < 0 || equipmentDefinitionStartIndex < 0 || equipmentDefinitionEndIndex < 0;
            if (indexesNotFound || numberOfEdits < RequiredNumberOfEdits || miGetChosenEquipment is null || miGetChosenEquipmentStage is null)
            {
                LoggingHelper.LogNoHooksIssue(
                    codes, numberOfEdits, RequiredNumberOfEdits, __originalMethod,
                    [
                        (nameof(stageDefinitionStartIndex), stageDefinitionStartIndex),
                        (nameof(stageDefinitionEndIndex), stageDefinitionEndIndex),
                        (nameof(equipmentDefinitionStartIndex), equipmentDefinitionStartIndex),
                        (nameof(equipmentDefinitionEndIndex), equipmentDefinitionEndIndex),
                    ],
                    [
                        (nameof(miGetChosenEquipmentStage), miGetChosenEquipmentStage),
                        (nameof(miFilterAvailableWeapons), miFilterAvailableWeapons),
                        (nameof(miGetChosenEquipment), miGetChosenEquipment),
                    ]);
            }
            if (stageDefinitionStartIndex >= 0 && stageDefinitionEndIndex > 0 && equipmentDefinitionStartIndex > 0 && equipmentDefinitionEndIndex > 0)
            {
                codes.RemoveRange(equipmentDefinitionStartIndex, equipmentDefinitionEndIndex - equipmentDefinitionStartIndex + 1);
                codes.InsertRange(equipmentDefinitionStartIndex,
                    [
                        new CodeInstruction(OpCodes.Ldloc_1), new CodeInstruction(opcode: OpCodes.Call, operand: miFilterAvailableWeapons), new CodeInstruction(OpCodes.Stloc_1),
                        new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldarg_2), new CodeInstruction(OpCodes.Ldloc_1), new CodeInstruction(opcode: OpCodes.Call, operand: miGetChosenEquipment)
                    ]);
                codes.RemoveRange(stageDefinitionStartIndex, stageDefinitionEndIndex - stageDefinitionStartIndex + 1);
                codes.InsertRange(stageDefinitionStartIndex, [new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldarg_2), new CodeInstruction(opcode: OpCodes.Call, operand: miGetChosenEquipmentStage)]);
            }
            else
            {
                MessageHelper.ErrorMessage("Harmony transpiler for ArenaPracticeFightMissionController. AddRandomWeapons could not find code hooks for loadout switching!");
            }

            return codes.AsEnumerable();
        }

        [HarmonyTranspiler]
        [HarmonyPatch("InitializeTeams")]
        public static IEnumerable<CodeInstruction> InitializeTeamsTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = new(instructions);
            int numberOfEdits = 0;
            if (miGetAITeamsCount != null)
            {
                for (int i = 0; i < codes.Count; ++i)
                {
                    if (codes[i].opcode == OpCodes.Ldc_I4_6)
                    {
                        codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetAITeamsCount);
                        ++numberOfEdits;
                        break;
                    }
                }
            }

            //Logging
            const int RequiredNumberOfEdits = 1;
            if (numberOfEdits < RequiredNumberOfEdits)
            {
                LoggingHelper.LogNoHooksIssue(
                    codes, numberOfEdits, RequiredNumberOfEdits, __originalMethod, [],
                    [
                        (nameof(miGetAITeamsCount), miGetAITeamsCount),
                    ]);
            }

            return codes.AsEnumerable();
        }

        [HarmonyTranspiler]
        [HarmonyPatch("StartPractice")]
        public static IEnumerable<CodeInstruction> StartPracticeTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = new(instructions);
            int numberOfEdits = 0;
            if (miGetInitialParticipantsCount != null)
            {
                for (int i = 0; i < codes.Count; ++i)
                {
                    if (codes[i].opcode == OpCodes.Ldc_I4_6)
                    {
                        codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetInitialParticipantsCount);
                        ++numberOfEdits;
                        break;
                    }
                }
            }

            //Logging
            const int RequiredNumberOfEdits = 1;
            if (numberOfEdits < RequiredNumberOfEdits)
            {
                LoggingHelper.LogNoHooksIssue(
                    codes, numberOfEdits, RequiredNumberOfEdits, __originalMethod, [],
                    [
                        (nameof(miGetInitialParticipantsCount), miGetInitialParticipantsCount),
                    ]);
            }

            return codes.AsEnumerable();
        }

        [HarmonyPostfix]
        [HarmonyPatch("StartPractice")]
        public static void StartPracticePostfix() => ParryStatsManager.Reset();

        [HarmonyTranspiler]
        [HarmonyPatch("InitializeParticipantCharacters")]
        public static IEnumerable<CodeInstruction> InitializeParticipantCharactersTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = new(instructions);
            int numberOfEdits = 0;
            int initializationStartIndex = -1, initializationEndIndex = -1;
            if (miGetParticipantCharacters != null)
            {
                for (int i = 0; i < codes.Count; ++i)
                {
                    if (numberOfEdits == 0 && codes[i].opcode == OpCodes.Ldloc_0)
                    {
                        initializationStartIndex = i + 1;
                        ++numberOfEdits;
                    }
                    else if (numberOfEdits == 1 && codes[i].opcode == OpCodes.Stfld && codes[i + 1].opcode == OpCodes.Ret)
                    {
                        initializationEndIndex = i - 1;
                        ++numberOfEdits;
                        break;
                    }
                }
            }

            //Logging
            const int RequiredNumberOfEdits = 2;
            if (initializationStartIndex < 0 || initializationEndIndex < 0 || numberOfEdits < RequiredNumberOfEdits || miInitializeParticipantCharacters is null)
            {
                LoggingHelper.LogNoHooksIssue(
                    codes, numberOfEdits, RequiredNumberOfEdits, __originalMethod,
                    [
                        (nameof(initializationStartIndex), initializationStartIndex),
                        (nameof(initializationEndIndex), initializationEndIndex),
                    ],
                    [
                        (nameof(miGetParticipantCharacters), miGetParticipantCharacters),
                        (nameof(miInitializeParticipantCharacters), miInitializeParticipantCharacters)
                    ]);
            }
            if (initializationStartIndex >= 0 && initializationEndIndex > 0)
            {
                codes.RemoveRange(initializationStartIndex, initializationEndIndex - initializationStartIndex + 1);
                codes.InsertRange(initializationStartIndex, [new CodeInstruction(opcode: OpCodes.Call, operand: miInitializeParticipantCharacters)]);
            }
            else
            {
                MessageHelper.ErrorMessage("Harmony transpiler for ArenaPracticeFightMissionController. InitializeParticipantCharacters could not find code hooks for initializing participants!");
            }

            return codes.AsEnumerable();
        }

        [HarmonyTranspiler]
        [HarmonyPatch("GetParticipantCharacters")]
        public static IEnumerable<CodeInstruction> GetParticipantCharactersTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = new(instructions);
            int numberOfEdits = 0;
            if (miGetTotalParticipantsCount != null && miGetParticipantCharactersLocal != null)
            {
                for (int i = 0; i < codes.Count; ++i)
                {
                    if (codes[i].opcode == OpCodes.Ldc_I4_S && (sbyte) codes[i].operand == 30)
                    {
                        codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetTotalParticipantsCount);
                        ++numberOfEdits;
                    }
                    else if (codes[i + 1].opcode == OpCodes.Stloc_1 && codes[i].opcode == OpCodes.Newobj)
                    {
                        codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetParticipantCharactersLocal);
                        ++numberOfEdits;
                        break;
                    }
                }
            }

            //Logging
            const int RequiredNumberOfEdits = 2;
            if (numberOfEdits < RequiredNumberOfEdits)
            {
                LoggingHelper.LogNoHooksIssue(
                    codes, numberOfEdits, RequiredNumberOfEdits, __originalMethod, [],
                    [
                        (nameof(miGetTotalParticipantsCount), miGetTotalParticipantsCount),
                        (nameof(miGetParticipantCharactersLocal), miGetParticipantCharactersLocal),
                    ]);
            }

            return codes.AsEnumerable();
        }

        /*
        [HarmonyTranspiler]
        [HarmonyPatch("SpawnArenaAgent")]
        public static IEnumerable<CodeInstruction> SpawnArenaAgentTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new(instructions);
            int insertIndex = 0;
            for (int i = 0; i < codes.Count; ++i)
            {
                if (codes[i].opcode == OpCodes.Stloc_2)
                {
                    insertIndex = i + 1;
                    break;
                }
            }
            codes.InsertRange(insertIndex, [new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldarg_1), new CodeInstruction(opcode: OpCodes.Call, operand: miGetParticipantTeam), new CodeInstruction(OpCodes.Starg, 1)]);
            return codes.AsEnumerable();
        }
        */

        [HarmonyPostfix]
        [HarmonyPatch("OnScoreHit")]
        public static void OnScoreHitPostfix(Agent affectedAgent, Agent affectorAgent, bool isBlocked, AttackCollisionData collisionData, ArenaPracticeFightMissionController __instance)
        {
            if (affectorAgent != null && affectedAgent != null && affectedAgent == Agent.Main && affectorAgent.IsEnemyOf(affectedAgent) && GetPracticeMode() == ArenaPracticeMode.Parry)
            {
                switch (collisionData.CollisionResult)
                {
                    case CombatCollisionResult.StrikeAgent:
                        ParryStatsManager.HitsTaken++;
                        break;
                    case CombatCollisionResult.Blocked:
                        ParryStatsManager.PreparedBlocks++;
                        break;
                    case CombatCollisionResult.Parried:
                        ParryStatsManager.PerfectBlocks++;
                        break;
                    case CombatCollisionResult.ChamberBlocked:
                        ParryStatsManager.ChamberBlocks++;
                        break;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("EnemyHitReward")]
        public static void EnemyHitRewardPostfix(Agent affectedAgent, Agent affectorAgent, float lastSpeedBonus, float lastShotDifficulty, WeaponComponentData? attackerWeapon, float hitpointRatio, float damageAmount, ArenaPracticeFightMissionController __instance)
        {
            if (affectedAgent.Origin == null || affectorAgent == null || affectorAgent.Origin == null || !IsExpansivePractice(__instance) || !Settings.Instance!.EnableViewerExperienceGain)
            {
                return;
            }

            PartyBase party = Hero.MainHero.PartyBelongedTo.Party;
            CharacterObject affectorCharacter = (CharacterObject) affectorAgent.Character;

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
                    party.MemberRoster.AddXpToTroop((int) xpAmount, troopRosterElement.Character);
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

        [HarmonyPrefix]
        [HarmonyPatch("SelectRandomAiTeam")]
        public static bool SelectRandomAiTeamPrefix(ArenaPracticeFightMissionController __instance, ref Team? __result) //Bool prefixes compete with each other and skip others, as well as original, if return false
        {
            var teams = FieldAccessHelper.APFMCAIParticipantTeamsByRef(__instance);
            //There is an issue with MBRandom.RandomInt(1) that it can return 1;
            if (teams.Count <= 1)
            {
                __result = teams.FirstOrDefault();
                return false;
            }
            return true;
        }

        /* service methods */

        internal static Team GetParticipantTeam(ArenaPracticeFightMissionController instance, Team team)
        {
            var character = FieldAccessHelper.APFMCParticipantCharactersByRef(instance)[FieldAccessHelper.APFMCSpawnedOpponentAgentCountByRef(instance)];
            if (character.IsHero && character.HeroObject.PartyBelongedTo == MobileParty.MainParty && instance.Mission.PlayerTeam.ActiveAgents.Count <= 4)
            {
                return instance.Mission.PlayerTeam;
            }
            return team;
        }

        internal static int GetInitialParticipantsCount()
        {
            return AOArenaBehaviorManager.GetInitialParticipantsCount(GetPracticeMode());
        }

        internal static int GetTotalParticipantsCount()
        {
            return AOArenaBehaviorManager.GetTotalParticipantsCount(GetPracticeMode());
        }

        internal static int GetActiveOpponentCount()
        {
            return AOArenaBehaviorManager.GetActiveOpponentCount(GetPracticeMode(false));
        }

        internal static int GetMinimumActiveOpponentCount()
        {
            return AOArenaBehaviorManager.GetMinimumActiveOpponentCount(GetPracticeMode());
        }

        internal static int GetAITeamsCount()
        {
            return AOArenaBehaviorManager.GetAITeamsCount(GetPracticeMode(false));
        }

        internal static List<Equipment> FilterAvailableWeapons(List<Equipment> loadoutList)
        {
            return AOArenaBehaviorManager.Instance?.FilterAvailableWeapons(loadoutList) ?? loadoutList;
        }

        internal static int GetChosenEquipmentStage(ArenaPracticeFightMissionController instance, int spawnIndex)
        {
            int chosenStage = 1 + spawnIndex * 3 / GetTotalParticipantsCount();
            return IsPlayerSpawn(instance, spawnIndex) ? (AOArenaBehaviorManager.Instance?.ChosenLoadoutStage ?? chosenStage) : chosenStage;
        }

        internal static int GetChosenEquipment(ArenaPracticeFightMissionController instance, int spawnIndex, List<Equipment> equipmentList)
        {
            int chosenLoadout = AOArenaBehaviorManager.Instance?.ChosenLoadout ?? -1;
            return IsPlayerSpawn(instance, spawnIndex) && chosenLoadout >= 0 ? chosenLoadout : MBRandom.RandomInt(equipmentList.Count);
        }

        internal static void SpawnArenaAgents(ArenaPracticeFightMissionController instance)
        {
            int countToAddAtOnce = Math.Max(FieldAccessHelper.APFMCSpawnFramesByRef(instance).Count - 1, 1);
            if (IsExpansivePractice(instance) && IsUndercrowded() && GetTotalParticipantsCount() >= FieldAccessHelper.APFMCSpawnedOpponentAgentCountByRef(instance) + countToAddAtOnce)
            {                
                int num = 0;
                while (num < countToAddAtOnce && FieldAccessHelper.APFMCSpawnedOpponentAgentCountByRef(instance) <= GetTotalParticipantsCount())
                {
                    FieldAccessHelper.APFMCParticipantAgentsByRef(instance).Add(deSpawnArenaAgent!(instance, deSelectRandomAiTeam!(instance), deGetSpawnFrame!(instance, true, UseInitialSpawnForDiversity(instance))));
                    ++num;
                }
            }
            else
            {
                FieldAccessHelper.APFMCParticipantAgentsByRef(instance).Add(deSpawnArenaAgent!(instance, deSelectRandomAiTeam!(instance), GetSpawnFrameInternal(instance, true, UseInitialSpawnForDiversity(instance))));
            }
            FieldAccessHelper.APFMCNextSpawnTimeByRef(instance) = (float) (instance.Mission.CurrentTime + 12.0 - Math.Min(FieldAccessHelper.APFMCSpawnedOpponentAgentCountByRef(instance) / (GetTotalParticipantsCount() / 10.0), 11.0));

            bool IsUndercrowded() => FieldAccessHelper.APFMCNextSpawnTimeByRef(instance) > instance.Mission.CurrentTime;
        }

        private static bool UseInitialSpawnForDiversity(ArenaPracticeFightMissionController instance)
        {
            var spawnFramesCount = FieldAccessHelper.APFMCSpawnFramesByRef(instance).Count;
            if (GetMinimumActiveOpponentCount() <= spawnFramesCount * 2 || FieldAccessHelper.APFMCAliveOpponentCountByRef(instance) <= spawnFramesCount * 3)
            {
                return false;
            }

            ++spawnCounter;
            if (spawnCounter > spawnFramesCount)
            {
                spawnCounter = 0;
                wasInitialSpawnLastTime = !wasInitialSpawnLastTime;
            }
            return wasInitialSpawnLastTime;
        }

        internal static List<CharacterObject> InitializeParticipantCharacters(List<CharacterObject> participantCharacters)
        {
            var heroes = participantCharacters.Where(x => x.IsHero).ToList();
            participantCharacters = participantCharacters.Where(x => !x.IsHero).OrderBy(x => AbstractTournamentApplicantManager.GetRandomizedImportance(x.Level)).ToList();
            foreach (var hero in heroes)
            {
                participantCharacters.Insert(MBRandom.RandomInt(0, 2 * participantCharacters.Count / 3), hero);
            }
            return participantCharacters;
        }

        internal static List<CharacterObject> GetPlayerRelatedParticipantCharacters()
        {
            return AOArenaBehaviorManager.GetPlayerRelatedParticipantCharacters(GetPracticeMode(), GetTotalParticipantsCount());
        }

        private static MatrixFrame GetSpawnFrameInternal(ArenaPracticeFightMissionController instance, bool considerPlayerDistance, bool isInitialSpawn)
        {
            if (GetPracticeMode() == ArenaPracticeMode.Parry && Agent.Main is Agent mainAgent && mainAgent.IsActive())
            {
                var matrixFrameList = isInitialSpawn ? FieldAccessHelper.APFMCInitialSpawnFramesByRef(instance) : FieldAccessHelper.APFMCSpawnFramesByRef(instance);
                return GetClosestSpawnFrame(matrixFrameList, mainAgent);
            }
            return deGetSpawnFrame!(instance, true, UseInitialSpawnForDiversity(instance));
        }

        private static MatrixFrame GetClosestSpawnFrame(List<MatrixFrame> matrixFrameList, Agent agent)
        {
            return matrixFrameList.OrderBy(x => x.origin.DistanceSquared(agent.Position)).First();
        }

        private static bool IsPlayerSpawn(ArenaPracticeFightMissionController instance, int spawnIndex)
        {
            return instance.IsPlayerPracticing && spawnIndex == 0 && FieldAccessHelper.APFMCParticipantAgentsByRef(instance).IsEmpty();
        }

        private static bool IsExpansivePractice(ArenaPracticeFightMissionController instance) => (instance?.IsPlayerPracticing ?? false) && ((AOArenaBehaviorManager.Instance?.PracticeMode ?? ArenaPracticeMode.Standard) == ArenaPracticeMode.Expansive);

        private static ArenaPracticeMode GetPracticeMode(bool checkPlayerIsPracticing = true) 
        {
            if (checkPlayerIsPracticing && !(Mission.Current?.GetMissionBehavior<ArenaPracticeFightMissionController>()?.IsPlayerPracticing ?? false))
            {
                return ArenaPracticeMode.Standard;
            }
            return AOArenaBehaviorManager.Instance?.PracticeMode ?? ArenaPracticeMode.Standard;
        }
    }
}