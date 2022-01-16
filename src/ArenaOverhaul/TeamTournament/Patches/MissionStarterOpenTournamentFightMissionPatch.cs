using HarmonyLib;

using SandBox;
using SandBox.Source.Missions;

using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace ArenaOverhaul.TeamTournament.Patches
{
    [HarmonyPatch(typeof(MissionStarter), "OpenTournamentFightMission")]
    class MissionStarterOpenTournamentFightMissionPatch
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
#if e165
                        new AgentBattleAILogic(),
#else
                        new AgentHumanAILogic(),
#endif
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