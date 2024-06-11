using SandBox.CampaignBehaviors;
using SandBox.Missions.MissionLogics.Arena;
using SandBox.ViewModelCollection.Missions;

using System.Collections.Generic;
using System.Reflection;

using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

using static HarmonyLib.AccessTools;

namespace ArenaOverhaul.Helpers
{
    public static class FieldAccessHelper
    {
        private static readonly FieldInfo fiTierLowerRenownLimits = Field(typeof(DefaultClanTierModel), "TierLowerRenownLimits");

        public static readonly FieldRef<ArenaMasterCampaignBehavior, bool> ArenaMasterKnowTournamentsByRef = FieldRefAccess<ArenaMasterCampaignBehavior, bool>("_knowTournaments");
        public static readonly FieldRef<ArenaMasterCampaignBehavior, List<Settlement>> ArenaMasterHasMetInSettlementsByRef = FieldRefAccess<ArenaMasterCampaignBehavior, List<Settlement>>("_arenaMasterHasMetInSettlements");

        public static readonly FieldRef<ArenaPracticeFightMissionController, int> APFMCSpawnedOpponentAgentCountByRef = FieldRefAccess<ArenaPracticeFightMissionController, int>("_spawnedOpponentAgentCount");
        public static readonly FieldRef<ArenaPracticeFightMissionController, int> APFMCAliveOpponentCountByRef = FieldRefAccess<ArenaPracticeFightMissionController, int>("_aliveOpponentCount");
        public static readonly FieldRef<ArenaPracticeFightMissionController, float> APFMCNextSpawnTimeByRef = FieldRefAccess<ArenaPracticeFightMissionController, float>("_nextSpawnTime");
        public static readonly FieldRef<ArenaPracticeFightMissionController, List<Agent>> APFMCParticipantAgentsByRef = FieldRefAccess<ArenaPracticeFightMissionController, List<Agent>>("_participantAgents");
        public static readonly FieldRef<ArenaPracticeFightMissionController, List<Team>> APFMCAIParticipantTeamsByRef = FieldRefAccess<ArenaPracticeFightMissionController, List<Team>>("_AIParticipantTeams");

        public static readonly FieldRef<MissionArenaPracticeFightVM, ArenaPracticeFightMissionController> MAPFVMPracticeMissionControllerByRef = FieldRefAccess<MissionArenaPracticeFightVM, ArenaPracticeFightMissionController>("_practiceMissionController");

        public static readonly FieldRef<FightTournamentGame, List<ItemObject>> FTGPossibleRegularRewardItemObjectsCacheByRef = FieldRefAccess<FightTournamentGame, List<ItemObject>>("_possibleRegularRewardItemObjectsCache");
        public static readonly FieldRef<FightTournamentGame, List<ItemObject>> FTGPossibleEliteRewardItemObjectsCacheByRef = FieldRefAccess<FightTournamentGame, List<ItemObject>>("_possibleEliteRewardItemObjectsCache");
        public static readonly FieldRef<FightTournamentGame, List<ItemObject>> FTGPossibleBannerRewardItemObjectsCacheByRef = FieldRefAccess<FightTournamentGame, List<ItemObject>>("_possibleBannerRewardItemObjectsCache");

        public static readonly FieldRef<int[]> DCTMTierLowerRenownLimitsByRef = StaticFieldRefAccess<int[]>(fiTierLowerRenownLimits);
    }
}