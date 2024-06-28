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
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace ArenaOverhaul.Patches
{
    //At this point of patch intensity, it might seem like it would be easier to just write a new variant of ArenaPracticeFightMissionController, but I've tried and turns out it would take a lot of effort to make the game work with it.
    //If some fellow modder is reading this and knows how to do it, I would appretiate the contribution!
    [HarmonyPatch(typeof(ArenaPracticeFightMissionController))]
    public static class ArenaPracticeFightMissionControllerPatch
    {
        private static readonly MethodInfo? miGetInitialParticipantsCount = AccessTools2.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetInitialParticipantsCount");
        private static readonly MethodInfo? miGetTotalParticipantsCount = AccessTools2.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetTotalParticipantsCount");
        private static readonly MethodInfo? miGetActiveOpponentCount = AccessTools2.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetActiveOpponentCount");
        private static readonly MethodInfo? miGetMinimumActiveOpponentCount = AccessTools2.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetMinimumActiveOpponentCount");
        private static readonly MethodInfo? miSpawnArenaAgents = AccessTools2.Method(typeof(ArenaPracticeFightMissionControllerPatch), "SpawnArenaAgents");
        private static readonly MethodInfo? miGetParticipantCharactersLocal = AccessTools2.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetPlayerRelatedParticipantCharacters");
        private static readonly MethodInfo? miFilterAvailableWeapons = AccessTools2.Method(typeof(ArenaPracticeFightMissionControllerPatch), "FilterAvailableWeapons");
        private static readonly MethodInfo? miGetChosenEquipmentStage = AccessTools2.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetChosenEquipmentStage");
        private static readonly MethodInfo? miGetChosenEquipment = AccessTools2.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetChosenEquipment");
        private static readonly MethodInfo? miInitializeParticipantCharacters = AccessTools2.Method(typeof(ArenaPracticeFightMissionControllerPatch), "InitializeParticipantCharacters");
        private static readonly MethodInfo? miGetAITeamsCount = AccessTools2.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetAITeamsCount");
        private static readonly MethodInfo? miGetAITeamColor = AccessTools2.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetAITeamColor");
        private static readonly MethodInfo? miGetAITeamBanner = AccessTools2.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetAITeamBanner");
        private static readonly MethodInfo? miGetPlayerTeamBanner = AccessTools2.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetPlayerTeamBanner");
        private static readonly MethodInfo? miSpawnInitialAITeamAgents = AccessTools2.Method(typeof(ArenaPracticeFightMissionControllerPatch), "SpawnInitialAITeamAgents");
        private static readonly MethodInfo? miGetPlayerCharacter = AccessTools2.Method(typeof(ArenaPracticeFightMissionControllerPatch), "GetPlayerCharacter");

        private static readonly FieldInfo? fiParticipantAgents = AccessTools2.Field(typeof(ArenaPracticeFightMissionController), "_participantAgents");

        private static readonly MethodInfo? miGetParticipantCharacters = AccessTools2.Method(typeof(ArenaPracticeFightMissionController), "GetParticipantCharacters");
        private static readonly MethodInfo? miSelectRandomAiTeam = AccessTools2.Method(typeof(ArenaPracticeFightMissionController), "SelectRandomAiTeam");
        private static readonly MethodInfo? miSpawnArenaAgent = AccessTools2.Method(typeof(ArenaPracticeFightMissionController), "SpawnArenaAgent");

        private static readonly MethodInfo? miPlayerCharacterGetter = AccessTools2.PropertyGetter(typeof(CharacterObject), "PlayerCharacter");
        private static readonly MethodInfo? miClothingColor1 = AccessTools2.Method(typeof(AgentBuildData), "ClothingColor1");
        private static readonly MethodInfo? miTeamColorGetter = AccessTools2.PropertyGetter(typeof(Team), "Color");
        private static readonly MethodInfo? miSpawnAgent = AccessTools2.Method(typeof(Mission), "SpawnAgent");

        internal delegate Agent SpawnArenaAgentDelegate(ArenaPracticeFightMissionController instance, Team team, MatrixFrame frame);
        private delegate Team SelectRandomAiTeamDelegate(ArenaPracticeFightMissionController instance);
        internal delegate MatrixFrame GetSpawnFrameDelegate(ArenaPracticeFightMissionController instance, bool considerPlayerDistance, bool isInitialSpawn);
        private delegate void InitializeTeamsDelegate(ArenaPracticeFightMissionController instance);

        internal static readonly SpawnArenaAgentDelegate? deSpawnArenaAgent = AccessTools2.GetDelegate<SpawnArenaAgentDelegate>(typeof(ArenaPracticeFightMissionController), "SpawnArenaAgent");
        private static readonly SelectRandomAiTeamDelegate? deSelectRandomAiTeam = AccessTools2.GetDelegate<SelectRandomAiTeamDelegate>(typeof(ArenaPracticeFightMissionController), "SelectRandomAiTeam");
        internal static readonly GetSpawnFrameDelegate? deGetSpawnFrame = AccessTools2.GetDelegate<GetSpawnFrameDelegate>(typeof(ArenaPracticeFightMissionController), "GetSpawnFrame");
        private static readonly InitializeTeamsDelegate? deInitializeTeams = AccessTools2.GetDelegate<InitializeTeamsDelegate>(typeof(ArenaPracticeFightMissionController), "InitializeTeams");

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
            if (miGetAITeamsCount != null && miGetAITeamColor != null && miGetAITeamBanner != null && miGetPlayerTeamBanner != null)
            {
                for (int i = 0; i < codes.Count; ++i)
                {
                    if (numberOfEdits == 0 && codes[i].opcode == OpCodes.Ldnull && codes[i + 1].opcode == OpCodes.Ldc_I4_1 && codes[i + 2].opcode == OpCodes.Ldc_I4_0 && codes[i + 3].opcode == OpCodes.Ldc_I4_1)
                    {
                        codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetPlayerTeamBanner);
                        ++numberOfEdits;
                    }
                    if (numberOfEdits == 1 && codes[i - 1].opcode == OpCodes.Ldc_I4_1 && codes[i].opcode == OpCodes.Ldc_I4_M1 && codes[i + 1].opcode == OpCodes.Ldc_I4_M1)
                    {
                        codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetAITeamColor);
                        codes.InsertRange(i, [new CodeInstruction(OpCodes.Ldarg_0)]);
                        ++numberOfEdits;
                    }
                    else if (numberOfEdits == 2 && codes[i].opcode == OpCodes.Ldc_I4_M1)
                    {
                        codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetAITeamColor);
                        codes.InsertRange(i, [new CodeInstruction(OpCodes.Ldarg_0)]);
                        ++numberOfEdits;
                    }
                    else if (numberOfEdits == 3 && codes[i].opcode == OpCodes.Ldnull)
                    {
                        codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetAITeamBanner);
                        codes.InsertRange(i, [new CodeInstruction(OpCodes.Ldarg_0)]);
                        ++numberOfEdits;
                    }
                    else if (numberOfEdits == 4 && codes[i].opcode == OpCodes.Ldc_I4_6)
                    {
                        codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetAITeamsCount);
                        ++numberOfEdits;
                        break;
                    }
                }
            }

            //Logging
            const int RequiredNumberOfEdits = 5;
            if (numberOfEdits < RequiredNumberOfEdits)
            {
                LoggingHelper.LogNoHooksIssue(
                    codes, numberOfEdits, RequiredNumberOfEdits, __originalMethod, [],
                    [
                        (nameof(miGetAITeamsCount), miGetAITeamsCount),
                        (nameof(miGetAITeamColor), miGetAITeamColor),
                        (nameof(miGetAITeamBanner), miGetAITeamBanner),
                        (nameof(miGetPlayerTeamBanner), miGetPlayerTeamBanner),
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
            int spawnAIAgentsStartIndex = -1, spawnAIAgentsEndIndex = -1;
            if (fiParticipantAgents != null && miSpawnArenaAgent != null && miGetInitialParticipantsCount != null)
            {
                for (int i = 2; i < codes.Count; ++i)
                {
                    if (numberOfEdits == 0 && codes[i].opcode == OpCodes.Ldarg_0 && codes[i + 1].opcode == OpCodes.Ldfld && (FieldInfo) codes[i + 1].operand == fiParticipantAgents && codes[i + 2].opcode == OpCodes.Ldarg_0 && codes[i + 3].opcode == OpCodes.Ldarg_0)
                    {
                        spawnAIAgentsStartIndex = i + 1;
                        ++numberOfEdits;
                    }
                    else if (numberOfEdits == 1 && codes[i].Calls(miSpawnArenaAgent) && codes[i + 1].opcode == OpCodes.Callvirt && codes[i + 1].operand.ToString()!.Contains("Add"))
                    {
                        spawnAIAgentsEndIndex = i + 1;
                        ++numberOfEdits;
                    }
                    else if (numberOfEdits == 2 && codes[i].opcode == OpCodes.Ldc_I4_6)
                    {
                        codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetInitialParticipantsCount);
                        ++numberOfEdits;
                        break;
                    }
                }
            }

            //Logging
            const int RequiredNumberOfEdits = 3;
            if (spawnAIAgentsStartIndex < 0 || spawnAIAgentsEndIndex < 0 || numberOfEdits < RequiredNumberOfEdits || miSpawnInitialAITeamAgents is null)
            {
                LoggingHelper.LogNoHooksIssue(
                    codes, numberOfEdits, RequiredNumberOfEdits, __originalMethod,
                    [
                        (nameof(spawnAIAgentsStartIndex), spawnAIAgentsStartIndex),
                        (nameof(spawnAIAgentsEndIndex), spawnAIAgentsEndIndex),
                    ],
                    [
                        (nameof(fiParticipantAgents), fiParticipantAgents),
                        (nameof(miSpawnArenaAgent), miSpawnArenaAgent),
                        (nameof(miGetInitialParticipantsCount), miGetInitialParticipantsCount),
                        (nameof(miSpawnInitialAITeamAgents), miSpawnInitialAITeamAgents),
                    ]);
            }

            if (spawnAIAgentsStartIndex >= 0 && spawnAIAgentsEndIndex > 0)
            {
                codes.RemoveRange(spawnAIAgentsStartIndex, spawnAIAgentsEndIndex - spawnAIAgentsStartIndex + 1);
                codes.InsertRange(spawnAIAgentsStartIndex, [new CodeInstruction(opcode: OpCodes.Ldloc_2), new CodeInstruction(opcode: OpCodes.Ldloc_0), new CodeInstruction(opcode: OpCodes.Call, operand: miSpawnInitialAITeamAgents)]);
            }
            else
            {
                MessageHelper.ErrorMessage("Harmony transpiler for ArenaPracticeFightMissionController. StartPractice could not find code hooks for spwaning initial participants!");
            }

            return codes.AsEnumerable();
        }

        [HarmonyPrefix]
        [HarmonyPatch("StartPractice")]
        public static void StartPracticePrefix(ArenaPracticeFightMissionController __instance)
        {
            if (!AOArenaBehaviorManager.Instance?.IsPlayerPrePractice ?? false)
            {
                ReInitializeTeams(__instance);
            }
            if (AOArenaBehaviorManager.Instance != null)
            {
                AOArenaBehaviorManager.Instance.IsPlayerPrePractice = false;
            }
            ParryPracticeStatsManager.Reset();
            TeamPracticeStatsManager.Reset();
        }

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

        [HarmonyTranspiler]
        [HarmonyPatch("SpawnArenaAgent")]
        public static IEnumerable<CodeInstruction> SpawnArenaAgentTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = new(instructions);
            int numberOfEdits = 0;
            if (miPlayerCharacterGetter != null && miGetPlayerCharacter != null)
            {
                for (int i = 0; i < codes.Count; ++i)
                {
                    if (numberOfEdits == 0 && codes[i].Calls(miPlayerCharacterGetter))
                    {
                        codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetPlayerCharacter);
                        ++numberOfEdits;
                    }
                    else if (numberOfEdits == 1 && codes[i].Calls(miPlayerCharacterGetter))
                    {
                        codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetPlayerCharacter);
                        ++numberOfEdits;
                    }
                    else if (numberOfEdits == 2 && codes[i].Calls(miPlayerCharacterGetter))
                    {
                        codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetPlayerCharacter);
                        ++numberOfEdits;
                        break;
                    }
                }
            }

            //Logging
            const int RequiredNumberOfEdits = 3;
            if (numberOfEdits < RequiredNumberOfEdits)
            {
                LoggingHelper.LogNoHooksIssue(
                    codes, numberOfEdits, RequiredNumberOfEdits, __originalMethod, [],
                    [
                        (nameof(miPlayerCharacterGetter), miPlayerCharacterGetter),
                        (nameof(miGetPlayerCharacter), miGetPlayerCharacter),
                    ]);
            }

            return codes.AsEnumerable();
        }

        [HarmonyPrefix]
        [HarmonyPatch("SpawnArenaAgent")]
        public static bool SpawnArenaAgentPrefix(Team team, MatrixFrame frame, ref Agent __result, ArenaPracticeFightMissionController __instance) //Bool prefixes compete with each other and skip others, as well as original, if return false
        {
            if (GetPracticeMode() != ArenaPracticeMode.Team)
            {
                return true;
            }
            return TeamPracticeController.SpawnArenaAgent(__instance, team, frame, out __result);
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnAgentRemoved")]
        public static bool OnAgentRemovedPrefix(ArenaPracticeFightMissionController __instance, Agent? affectedAgent, Agent? affectorAgent, AgentState agentState, KillingBlow killingBlow) //Bool prefixes compete with each other and skip others, as well as original, if return false
        {
            if (!IsTeamPractice(__instance))
            {
                return true;
            }
            return TeamPracticeController.OnAgentRemovedInternal(__instance, affectedAgent, affectorAgent);
        }

        [HarmonyPrefix]
        [HarmonyPatch("CheckPracticeEndedForPlayer")]
        public static bool CheckPracticeEndedForPlayerPrefix(ArenaPracticeFightMissionController __instance, ref bool __result) //Bool prefixes compete with each other and skip others, as well as original, if return false
        {
            if (!IsTeamPractice(__instance))
            {
                return true;
            }
            __result = ((__instance.Mission.MainAgent == null || !__instance.Mission.MainAgent.IsActive()) && TeamPracticeStatsManager.RemainingAlliesCount == 0) || __instance.RemainingOpponentCount == 0;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnEndMissionRequest")]
        public static bool OnEndMissionRequestPrefix(ArenaPracticeFightMissionController __instance, ref InquiryData? __result, out bool canPlayerLeave) //Bool prefixes compete with each other and skip others, as well as original, if return false
        {
            canPlayerLeave = true;
            if (!IsTeamPractice(__instance) || TeamPracticeStatsManager.RemainingAlliesCount <= 0 || (__instance.Mission.MainAgent?.IsActive() ?? false))
            {
                return true;
            }

            __result = !__instance.IsPlayerPracticing 
                ? null
                : new InquiryData(new TextObject("{=zv49qE35}Practice Fight").ToString(), new TextObject("{=}Your teammates are still fighting. Do you want to leave the arena?").ToString(), true, true, GameTexts.FindText("str_ok").ToString(), GameTexts.FindText("str_cancel").ToString(), new Action(__instance.Mission.OnEndMissionResult), null);
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnScoreHit")]
        public static void OnScoreHitPostfix(Agent affectedAgent, Agent affectorAgent, bool isBlocked, AttackCollisionData collisionData, ArenaPracticeFightMissionController __instance)
        {
            if (affectorAgent != null && affectedAgent != null && affectorAgent.IsEnemyOf(affectedAgent) && GetPracticeMode() == ArenaPracticeMode.Parry)
            {
                if (affectedAgent == Agent.Main)
                {
                    switch (collisionData.CollisionResult)
                    {
                        case CombatCollisionResult.StrikeAgent:
                            ParryPracticeStatsManager.HitsTaken++;
                            break;
                        case CombatCollisionResult.Blocked:
                            ParryPracticeStatsManager.PreparedBlocks++;
                            break;
                        case CombatCollisionResult.Parried:
                            ParryPracticeStatsManager.PerfectBlocks++;
                            break;
                        case CombatCollisionResult.ChamberBlocked:
                            ParryPracticeStatsManager.ChamberBlocks++;
                            break;
                    }
                }
                else if (affectorAgent == Agent.Main && collisionData.CollisionResult == CombatCollisionResult.StrikeAgent)
                {
                    ParryPracticeStatsManager.HitsMade++;
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("EnemyHitReward")]
        public static void EnemyHitRewardPostfix(Agent affectedAgent, Agent affectorAgent, float lastSpeedBonus, float lastShotDifficulty, WeaponComponentData? attackerWeapon, float hitpointRatio, float damageAmount, ArenaPracticeFightMissionController __instance)
        {
            bool expansivePracticeExpGain = IsExpansivePractice(__instance) && Settings.Instance!.EnableViewerExperienceGain;
            bool teamPracticeExpGain = (IsTeamPractice(__instance) && Settings.Instance!.TeamEnableViewerExperienceGain);
            if (affectedAgent?.Origin == null || affectorAgent == null || affectorAgent.Origin == null || !affectedAgent.IsEnemyOf(affectorAgent) || !expansivePracticeExpGain || !teamPracticeExpGain)
            {
                return;
            }

            PartyBase party = Hero.MainHero.PartyBelongedTo.Party;
            CharacterObject affectorCharacter = (CharacterObject) affectorAgent.Character;

            bool affectorIsAliedHero = (affectorCharacter == GetPlayerCharacter()) || (affectorCharacter.IsHero && party.MemberRoster.Contains(affectorCharacter));
            if (!affectorIsAliedHero)
            {
                return;
            }

            float xpAmount = affectedAgent.Health < 1.0 ? 10f : 2f;
            SkillObject? relevantSkill = attackerWeapon?.RelevantSkill;
            foreach (TroopRosterElement troopRosterElement in party.MemberRoster.GetTroopRoster())
            {
                if (!troopRosterElement.Character.IsHero && troopRosterElement.Character != GetPlayerCharacter())
                {
                    party.MemberRoster.AddXpToTroop((int) xpAmount, troopRosterElement.Character);
                }
                else if (relevantSkill is not null && troopRosterElement.Character.IsHero && troopRosterElement.Character != GetPlayerCharacter() && !FieldAccessHelper.APFMCParticipantAgentsByRef(__instance).Select(a => a.Character).Contains(troopRosterElement.Character))
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
            return AOArenaBehaviorManager.GetActiveOpponentCount(GetPracticeMode());
        }

        internal static int GetMinimumActiveOpponentCount()
        {
            return AOArenaBehaviorManager.GetMinimumActiveOpponentCount(GetPracticeMode());
        }

        internal static int GetAITeamsCount()
        {
            return AOArenaBehaviorManager.GetAITeamsCount(GetPracticeMode());
        }

        internal static uint GetAITeamColor(ArenaPracticeFightMissionController instance)
        {
            return AOArenaBehaviorManager.GetAITeamColor(GetPracticeMode(), FieldAccessHelper.APFMCAIParticipantTeamsByRef(instance).Count);
        }

        internal static Banner GetAITeamBanner(ArenaPracticeFightMissionController instance)
        {
            return Banner.CreateOneColoredEmptyBanner(BannerManager.GetColorId(GetAITeamColor(instance)));
        }

        internal static Banner GetPlayerTeamBanner()
        {
            return Hero.MainHero.ClanBanner;
        }

        internal static List<Equipment> FilterAvailableWeapons(List<Equipment> loadoutList)
        {
            return AOArenaBehaviorManager.Instance?.FilterAvailableWeapons(loadoutList) ?? loadoutList;
        }

        private static int GetCurrentStage(int spawnIndex)
        {
            return 1 + spawnIndex * 3 / GetTotalParticipantsCount();
        }

        internal static int GetChosenEquipmentStage(ArenaPracticeFightMissionController instance, int spawnIndex)
        {
            int currentStage = GetCurrentStage(spawnIndex);
            return ShouldUseCustomLoadout(instance, spawnIndex, out var loadout) ? loadout.ChosenLoadoutStage : currentStage;
        }

        internal static int GetChosenEquipment(ArenaPracticeFightMissionController instance, int spawnIndex, List<Equipment> equipmentList)
        {
            return ShouldUseCustomLoadout(instance, spawnIndex, out var loadout) ? loadout.ChosenLoadout : MBRandom.RandomInt(equipmentList.Count);
        }

        internal static void SpawnArenaAgents(ArenaPracticeFightMissionController instance)
        {
            int countToAddAtOnce = IsExpansivePractice(instance)
                ? Math.Max(FieldAccessHelper.APFMCSpawnFramesByRef(instance).Count - 1, 1)
                : MBRandom.RandomInt(2, Math.Max(3, GetActiveOpponentCount() / GetAITeamsCount() + 1));

            if (IsExpansivePractice(instance) && IsUndercrowded() && GetTotalParticipantsCount() >= FieldAccessHelper.APFMCSpawnedOpponentAgentCountByRef(instance) + countToAddAtOnce)
            {
                int num = 0;
                while (num < countToAddAtOnce && FieldAccessHelper.APFMCSpawnedOpponentAgentCountByRef(instance) <= GetTotalParticipantsCount())
                {
                    FieldAccessHelper.APFMCParticipantAgentsByRef(instance).Add(deSpawnArenaAgent!(instance, deSelectRandomAiTeam!(instance), deGetSpawnFrame!(instance, true, UseInitialSpawnForDiversity(instance))));
                    ++num;
                }
            }
            else if (IsTeamPractice(instance))
            {
                TeamPracticeController.SpawnArenaAgentsForTeamPractice(instance, countToAddAtOnce);
            }
            else
            {
                FieldAccessHelper.APFMCParticipantAgentsByRef(instance).Add(deSpawnArenaAgent!(instance, deSelectRandomAiTeam!(instance), GetSpawnFrameInternal(instance, true, UseInitialSpawnForDiversity(instance))));
            }
            FieldAccessHelper.APFMCNextSpawnTimeByRef(instance) = (float) (instance.Mission.CurrentTime + 12.0 - Math.Min(FieldAccessHelper.APFMCSpawnedOpponentAgentCountByRef(instance) / (GetTotalParticipantsCount() / 10.0), 11.0));

            bool IsUndercrowded() => FieldAccessHelper.APFMCNextSpawnTimeByRef(instance) > instance.Mission.CurrentTime;
        }

        internal static bool UseInitialSpawnForDiversity(ArenaPracticeFightMissionController instance)
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

        internal static void ReInitializeTeams(ArenaPracticeFightMissionController instance)
        {
            instance.Mission.Teams.Clear();
            deInitializeTeams!(instance);
        }

        internal static List<CharacterObject> GetPlayerRelatedParticipantCharacters()
        {
            return AOArenaBehaviorManager.GetPlayerRelatedParticipantCharacters(GetPracticeMode(), GetTotalParticipantsCount());
        }

        internal static void SpawnInitialAITeamAgents(ArenaPracticeFightMissionController instance, int spawnIndex, int teamCount)
        {
            if (GetPracticeMode() == ArenaPracticeMode.Team)
            {
                var frame = deGetSpawnFrame!(instance, true, true);
                var numberToSpawn = Math.DivRem(GetInitialParticipantsCount(), teamCount, out var reminder) + (reminder > spawnIndex ? 1 : 0);
                var team = FieldAccessHelper.APFMCAIParticipantTeamsByRef(instance)[spawnIndex];
                for (int i = 0; i < numberToSpawn; ++i)
                {
                    FieldAccessHelper.APFMCParticipantAgentsByRef(instance).Add(deSpawnArenaAgent!(instance, team, frame));
                }
                return;
            }
            FieldAccessHelper.APFMCParticipantAgentsByRef(instance).Add(deSpawnArenaAgent!(instance, FieldAccessHelper.APFMCAIParticipantTeamsByRef(instance)[spawnIndex % teamCount], deGetSpawnFrame!(instance, false, true)));
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

        private static bool ShouldUseCustomLoadout(ArenaPracticeFightMissionController instance, int spawnIndex, out (int ChosenLoadoutStage, int ChosenLoadout) loadoutToUse)
        {
            loadoutToUse = (0, -1);
            if (instance.IsPlayerPracticing)
            {
                if (GetPracticeMode() == ArenaPracticeMode.Team && TeamPracticeController.SpawningCharacterObject != null)
                {
                    if (TeamPracticeController.SpawningCharacterObject == GetPlayerCharacter())
                    {
                        return GetPlayerLoadout(spawnIndex, out loadoutToUse);
                    }
                    return GetCompanionLoadout(TeamPracticeController.SpawningCharacterObject, ref loadoutToUse);
                }

                //Player
                if (spawnIndex == 0 && FieldAccessHelper.APFMCParticipantAgentsByRef(instance).IsEmpty())
                {
                    return GetPlayerLoadout(spawnIndex, out loadoutToUse);
                }

                //Companions
                var character = FieldAccessHelper.APFMCParticipantCharactersByRef(instance)[spawnIndex];
                return GetCompanionLoadout(character, ref loadoutToUse);
            }

            return false;

            //Local methods
            static bool GetPlayerLoadout(int spawnIndex, out (int ChosenLoadoutStage, int ChosenLoadout) loadoutToUse)
            {
                int chosenLoadoutStage = AOArenaBehaviorManager.Instance?.ChosenLoadoutStage ?? GetCurrentStage(spawnIndex);
                int chosenLoadout = AOArenaBehaviorManager.Instance?.ChosenLoadout ?? -1;

                loadoutToUse = (chosenLoadoutStage, chosenLoadout);
                return chosenLoadoutStage > 0 && chosenLoadout >= 0;
            }

            static bool GetCompanionLoadout(CharacterObject character, ref (int ChosenLoadoutStage, int ChosenLoadout) loadoutToUse)
            {
                if (character.IsHero && character.HeroObject.Clan == Clan.PlayerClan)
                {
                    return (AOArenaBehaviorManager.Instance?.CompanionLoadouts.TryGetValue(character.HeroObject, out loadoutToUse) ?? false) && loadoutToUse.ChosenLoadoutStage > 0 && loadoutToUse.ChosenLoadout >= 0;
                }
                return false;
            }
        }

        internal static CharacterObject GetPlayerCharacter() =>
            !(Mission.Current?.GetMissionBehavior<ArenaPracticeFightMissionController>()?.IsPlayerPracticing ?? false)
                ? CharacterObject.PlayerCharacter
                : AOArenaBehaviorManager.Instance?.ChosenCharacter ?? CharacterObject.PlayerCharacter;

        private static bool IsExpansivePractice(ArenaPracticeFightMissionController instance) => (instance?.IsPlayerPracticing ?? false) && ((AOArenaBehaviorManager.Instance?.PracticeMode ?? ArenaPracticeMode.Standard) == ArenaPracticeMode.Expansive);
        private static bool IsTeamPractice(ArenaPracticeFightMissionController instance) => (instance?.IsPlayerPracticing ?? false) && ((AOArenaBehaviorManager.Instance?.PracticeMode ?? ArenaPracticeMode.Standard) == ArenaPracticeMode.Team);

        internal static ArenaPracticeMode GetPracticeMode() 
        {
            if (!(AOArenaBehaviorManager.Instance?.IsPlayerPrePractice ?? false) && !(Mission.Current?.GetMissionBehavior<ArenaPracticeFightMissionController>()?.IsPlayerPracticing ?? false))
            {
                return ArenaPracticeMode.Standard;
            }
            return AOArenaBehaviorManager.Instance?.PracticeMode ?? ArenaPracticeMode.Standard;
        }
    }
}