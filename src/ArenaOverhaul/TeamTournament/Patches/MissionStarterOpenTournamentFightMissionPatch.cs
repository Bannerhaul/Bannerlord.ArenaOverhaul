using HarmonyLib;

using SandBox;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Arena;
using SandBox.Tournaments;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace ArenaOverhaul.TeamTournament.Patches
{
    [HarmonyPatch(typeof(TournamentMissionStarter), "OpenTournamentFightMission")]
    public static class MissionStarterOpenTournamentFightMissionPatch
    {
        public static bool Prefix(ref Mission __result, string scene, TournamentGame tournamentGame, Settlement settlement, CultureObject culture, bool isPlayerParticipating)
        {
            if (TeamTournamentInfo.Current != null && TeamTournamentInfo.Current.IsStarted)
            {
                __result = MissionState.OpenNew("TournamentFight", SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", false), delegate (Mission missionController)
                {
                    var tournamentMissionController = new TeamTournamentMissionController();
                    return new MissionBehavior[]
                    {
                        new CampaignMissionComponent(),
                        new EquipmentControllerLeaveLogic(),
                        tournamentMissionController, // this is patched!
                        new TeamTournamentBehavior(tournamentGame, settlement, tournamentMissionController, isPlayerParticipating), // this is patched!
                        new AgentVictoryLogic(),
                        new MissionAgentPanicHandler(),
                        new AgentHumanAILogic(),
                        new ArenaAgentStateDeciderLogic(),
                        new MissionHardBorderPlacer(),
                        new MissionBoundaryPlacer(),
                        new MissionOptionsComponent(),
                        new HighlightsController(),
                        new SandboxHighlightsController()
                    };
                }, true, true);
                return false;
            }
            return true;
        }
    }
}