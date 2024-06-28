using ArenaOverhaul.CampaignBehaviors.BehaviorManagers;
using ArenaOverhaul.Helpers;
using ArenaOverhaul.ModSettings;

using HarmonyLib.BUTR.Extensions;

using SandBox.Missions.MissionLogics.Arena;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

using static ArenaOverhaul.Patches.ArenaPracticeFightMissionControllerPatch;

namespace ArenaOverhaul.ArenaPractice
{
    internal static class TeamPracticeController
    {
        private static readonly PropertyInfo? piOpponentCountBeatenByPlayer = AccessTools2.DeclaredProperty(typeof(ArenaPracticeFightMissionController), "OpponentCountBeatenByPlayer");

        private delegate void AddRandomWeaponsDelegate(ArenaPracticeFightMissionController instance, Equipment equipment, int spawnIndex);
        private delegate void AddRandomClothesDelegate(ArenaPracticeFightMissionController instance, CharacterObject troop, Equipment equipment);

        private static readonly AddRandomWeaponsDelegate? deAddRandomWeapons = AccessTools2.GetDelegate<AddRandomWeaponsDelegate>(typeof(ArenaPracticeFightMissionController), "AddRandomWeapons");
        private static readonly AddRandomClothesDelegate? deAddRandomClothes = AccessTools2.GetDelegate<AddRandomClothesDelegate>(typeof(ArenaPracticeFightMissionController), "AddRandomClothes");

        internal static CharacterObject? SpawningCharacterObject { get; private set; } = null;

        public static void SpawnArenaAgentsForTeamPractice(ArenaPracticeFightMissionController instance, int countToAddAtOnce)
        {
            var aiTeamCount = FieldAccessHelper.APFMCAIParticipantTeamsByRef(instance).Count;
            if (GetTotalParticipantsCount() >= FieldAccessHelper.APFMCSpawnedOpponentAgentCountByRef(instance) + countToAddAtOnce * GetAITeamsCount())
            {
                for (int teamIndex = 0; teamIndex < aiTeamCount; ++teamIndex)
                {
                    var team = FieldAccessHelper.APFMCAIParticipantTeamsByRef(instance)[teamIndex];
                    var frame = GetBestFrameForTeam(instance, team);
                    //var frame = deGetSpawnFrame!(instance, true, UseInitialSpawnForDiversity(instance));
                    for (int i = 0; i < countToAddAtOnce; ++i)
                    {
                        FieldAccessHelper.APFMCParticipantAgentsByRef(instance).Add(deSpawnArenaAgent!(instance, team, frame));
                    }
                }
            }
            else
            {
                int localSpwanIndex = 0;
                int teamCount = FieldAccessHelper.APFMCAIParticipantTeamsByRef(instance).Count;
                while (FieldAccessHelper.APFMCSpawnedOpponentAgentCountByRef(instance) < GetTotalParticipantsCount())
                {
                    var team = FieldAccessHelper.APFMCAIParticipantTeamsByRef(instance)[localSpwanIndex % teamCount];
                    var frame = GetBestFrameForTeam(instance, team);
                    //var frame = deGetSpawnFrame!(instance, true, UseInitialSpawnForDiversity(instance));
                    FieldAccessHelper.APFMCParticipantAgentsByRef(instance).Add(deSpawnArenaAgent!(instance, team, frame));
                    ++localSpwanIndex;
                }
            }

            if (AOArenaBehaviorManager._lastPlayerRelatedCharacterList != null && AOArenaBehaviorManager._lastPlayerRelatedCharacterList.Count > 0)
            {
                var spawnedAllies = 0;
                var mission = instance.Mission;
                var spawnIndex = FieldAccessHelper.APFMCSpawnedOpponentAgentCountByRef(instance);
                var team = mission.PlayerTeam;
                var frame = GetBestFrameForTeam(instance, team);
                //var frame = deGetSpawnFrame!(instance, true, UseInitialSpawnForDiversity(instance));
                while (spawnedAllies < countToAddAtOnce && TeamPracticeStatsManager.SpawnedAliedAgentCount < AOArenaBehaviorManager._lastPlayerRelatedCharacterList.Count)
                {
                    SpwanAllyAgent(instance, mission, spawnIndex, team, frame);
                    ++spawnedAllies;
                }
            }
        }

        public static void SpawnInitialPlayerTeamAgents(ArenaPracticeFightMissionController instance, Mission mission, int spawnIndex, Team team, MatrixFrame frame)
        {
            if (AOArenaBehaviorManager._lastPlayerRelatedCharacterList is null || AOArenaBehaviorManager._lastPlayerRelatedCharacterList.Count <= 0)
            {
                return;
            }

            var agentCountToSpawn = Math.Min(GetInitialParticipantsCount() / GetAITeamsCount(), AOArenaBehaviorManager._lastPlayerRelatedCharacterList.Count / 2);
            while (TeamPracticeStatsManager.SpawnedAliedAgentCount < agentCountToSpawn)
            {
                SpwanAllyAgent(instance, mission, spawnIndex, team, frame);
            }
        }

        public static void SpwanAllyAgent(ArenaPracticeFightMissionController instance, Mission mission, int spawnIndex, Team team, MatrixFrame frame)
        {
            var characterObject = AOArenaBehaviorManager._lastPlayerRelatedCharacterList![TeamPracticeStatsManager.SpawnedAliedAgentCount];
            var agent = SpawnArenaAgentInternal(instance, mission, characterObject, spawnIndex, team, frame);
            if (agent.IsAIControlled)
            {
                agent.SetWatchState(Agent.WatchState.Alarmed);
            }
            ++TeamPracticeStatsManager.SpawnedAliedAgentCount;
            ++TeamPracticeStatsManager.AliveAlliesCount;

            FieldAccessHelper.APFMCParticipantAgentsByRef(instance).Add(agent);
        }

        public static bool SpawnArenaAgent(ArenaPracticeFightMissionController instance, Team team, MatrixFrame frame, out Agent agent)
        {
            Mission mission = instance.Mission;
            CharacterObject characterObject;
            int spawnIndex;
            if (team == mission.PlayerTeam && mission.MainAgent is null)
            {
                characterObject = GetPlayerCharacter();
                spawnIndex = 0;
                SpawnInitialPlayerTeamAgents(instance, mission, spawnIndex, team, frame);
            }
            else
            {
                spawnIndex = FieldAccessHelper.APFMCSpawnedOpponentAgentCountByRef(instance);
                characterObject = FieldAccessHelper.APFMCParticipantCharactersByRef(instance)[spawnIndex];
            }

            agent = SpawnArenaAgentInternal(instance, mission, characterObject, spawnIndex, team, frame);
            if (team != mission.PlayerTeam)
            {
                ++FieldAccessHelper.APFMCAliveOpponentCountByRef(instance);
                ++FieldAccessHelper.APFMCSpawnedOpponentAgentCountByRef(instance);
            }
            if (agent.IsAIControlled)
            {
                agent.SetWatchState(Agent.WatchState.Alarmed);
            }

            return false;
        }

        public static Agent SpawnArenaAgentInternal(ArenaPracticeFightMissionController instance, Mission mission, CharacterObject characterObject, int spawnIndex, Team team, MatrixFrame frame)
        {
            Equipment equipment = new();

            SpawningCharacterObject = characterObject;
            deAddRandomWeapons!(instance, equipment, spawnIndex);
            deAddRandomClothes!(instance, characterObject, equipment);
            AgentBuildData agentBuildData1 = new AgentBuildData(characterObject).Team(team).InitialPosition(in frame.origin);
            Vec2 vec2 = frame.rotation.f.AsVec2;
            vec2 = vec2.Normalized();
            ref Vec2 local = ref vec2;
            AgentBuildData agentBuildData2 = agentBuildData1
                .InitialDirection(in local).NoHorses(true)
                .TroopOrigin(new SimpleAgentOrigin(characterObject))
                .Equipment(equipment)
                .ClothingColor1(team.Color)
                .ClothingColor2(team.Color)
                .Banner(team.Banner)
                .Controller(characterObject == GetPlayerCharacter() ? Agent.ControllerType.Player : Agent.ControllerType.AI);
            Agent agent = mission.SpawnAgent(agentBuildData2);
            agent.FadeIn();
            return agent;
        }

        public static MatrixFrame GetBestFrameForTeam(ArenaPracticeFightMissionController instance, Team team)
        {
            var teamsDict = GetClosestSpawnFramesForTeams(instance);
            var framesDict = GetClosestTeamsForSpawnFrames(teamsDict);

            var agents = team.ActiveAgents;
            if (agents is null || agents.Count <= 0)
            {
                var bestFramesForWipedOutTeam = framesDict.Where(kvp => kvp.Value.FirstOrDefault() is { } teamDistance && teamDistance.Team != team).Select(kvp => (Frame: kvp.Key, kvp.Value.First().Distance)).OrderByDescending(x => x.Distance).ToList();
                return bestFramesForWipedOutTeam.Count >= 1 ? bestFramesForWipedOutTeam.First().Frame : deGetSpawnFrame!(instance, true, UseInitialSpawnForDiversity(instance));
            }

            var bestFrames = framesDict.Where(kvp => kvp.Value.FirstOrDefault() is { } teamDistance && teamDistance.Team == team).Select(kvp => (Frame: kvp.Key, kvp.Value.First().Distance)).OrderBy(x => x.Distance).ToList();
            if (bestFrames.Count >= 1)
            {
                return bestFrames.First().Frame;
            }

            var optimalFrames = framesDict.Where(kvp => kvp.Value.FirstOrDefault(x => x.Team == team) is { } teamDistance && teamDistance.Team == team).Select(kvp =>
            {
                var teamDistance = kvp.Value.First(x => x.Team == team).Distance;
                return (Frame: kvp.Key, BetterTeamsCount: kvp.Value.Count(x => x.Team != team && x.Distance < teamDistance), Distance: teamDistance);
            }).OrderBy(x => x.BetterTeamsCount).ThenBy(x => x.Distance).ToList();


            return (optimalFrames.Count >= 1)
                ? optimalFrames.First().Frame
                : teamsDict.TryGetValue(team, out var teamDistances) && teamDistances.Count > 0 ? teamDistances.First().Frame : deGetSpawnFrame!(instance, true, UseInitialSpawnForDiversity(instance));
        }


        public static Dictionary<MatrixFrame, List<(Team Team, float Distance)>> GetClosestTeamsForSpawnFrames(Dictionary<Team, List<(MatrixFrame Frame, float Distance)>> teamsDict)
        {
            return teamsDict
                .Select(kvp => (Team: kvp.Key, FrameDistances: kvp.Value))
                .SelectMany(x => x.FrameDistances, (item, frameDistance) => (frameDistance.Frame, item.Team, frameDistance.Distance))
                .GroupBy(x => x.Frame).Select(group => (Frame: group.Key, TeamDistances: group.Select(x => (x.Team, x.Distance)).OrderBy(x => x.Distance).ToList()))
                .ToDictionary(key => key.Frame, value => value.TeamDistances);
        }

        public static Dictionary<Team, List<(MatrixFrame Frame, float Distance)>> GetClosestSpawnFramesForTeams(ArenaPracticeFightMissionController instance)
        {
            var teams = instance.Mission.Teams.Where(x => x.Side != BattleSideEnum.None);
            return teams.ToDictionary(key => key, value => GetClosestSpawnFrames(instance, value));
        }

        public static List<(MatrixFrame Frame, float Distance)> GetClosestSpawnFrames(ArenaPracticeFightMissionController instance, Team team)
        {
            var matrixFrameList = FieldAccessHelper.APFMCInitialSpawnFramesByRef(instance).Concat(FieldAccessHelper.APFMCSpawnFramesByRef(instance)).ToList();

            var agents = team.ActiveAgents;
            if (agents is null || agents.Count <= 0)
            {
                return matrixFrameList.Select(x => (Frame: x, Distance: float.MaxValue)).ToList();
            }

            var matrixFrameListWithDistances = new List<(MatrixFrame Frame, float Distance)>();
            foreach (var agent in agents)
            {
                matrixFrameListWithDistances.AddRange(matrixFrameList.Select(x => (Frame: x, Distance: x.origin.DistanceSquared(agent.Position))));
            }

            return matrixFrameListWithDistances.GroupBy(x => x.Frame).Select(group => (Frame: group.Key, Distance: group.Average(item => item.Distance))).OrderBy(x => x.Distance).ToList();
        }

        public static bool OnAgentRemovedInternal(ArenaPracticeFightMissionController instance, Agent? affectedAgent, Agent? affectorAgent)
        {
            var playerTeam = instance.Mission.PlayerTeam;
            if (affectedAgent != null && affectedAgent.IsHuman)
            {
                if (affectedAgent.Team != playerTeam)
                {
                    --FieldAccessHelper.APFMCAliveOpponentCountByRef(instance);
                }
                else if (affectedAgent.Character != GetPlayerCharacter())
                {
                    --TeamPracticeStatsManager.AliveAlliesCount;
                }
                if (affectedAgent.IsMainAgent && Settings.Instance!.EnableTeamPracticeAgentSwitching && TeamPracticeHotKeyController.HasTargetsToSwitchTo())
                {
                    MBInformationManager.AddQuickInformation(new("{=}You are defeated, but your team is still fighting and you can switch to another teammate if you like."));
                }
                if (affectorAgent != null && affectorAgent.IsHuman && affectorAgent.Team == playerTeam && affectedAgent.IsEnemyOf(affectorAgent))
                {
                    int beatenOpponents = instance.OpponentCountBeatenByPlayer + 1;
                    piOpponentCountBeatenByPlayer!.SetValue(instance, beatenOpponents);
                }
            }
            if (affectedAgent is null || !FieldAccessHelper.APFMCParticipantAgentsByRef(instance).Contains(affectedAgent))
            {
                return false;
            }
            FieldAccessHelper.APFMCParticipantAgentsByRef(instance).Remove(affectedAgent!);
            return false;
        }
    }
}
