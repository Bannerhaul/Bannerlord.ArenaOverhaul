using HarmonyLib;

using SandBox.View;
using SandBox.View.Missions;
using SandBox.View.Missions.Tournaments;

using System;
using System.Collections.Generic;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.LegacyGUI.Missions;
using TaleWorlds.MountAndBlade.View.Missions;

namespace ArenaOverhaul.TeamTournament.Patches
{
    [HarmonyPatch(typeof(TournamentMissionViews), "OpenTournamentFightMission")]
    class TournamentMissionViewsOpenTournamentFightMissionPatch
    {
        static bool Prefix(ref MissionView[] __result, Mission mission)
        {
            if (TeamTournamentInfo.Current != null && TeamTournamentInfo.Current.IsStarted)
            {
                __result = new List<MissionView>
                {
                    new CampaignMissionView(),
                    new ConversationCameraView(),
                    ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
                    ViewCreator.CreateOptionsUIHandler(),
                    ViewCreator.CreateMissionMainAgentGamepadEquipDropView(mission),
                    ViewCreatorManager.CreateMissionView<MissionGauntletTeamTournamentView>(false, null, Array.Empty<object>()), // this is patched!
                    new MissionAudienceHandler(0.4f + (MBRandom.RandomFloat * 0.6f)),
                    ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                    ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                    ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
                    ViewCreator.CreateMissionAgentLockVisualizerView(mission),
                    new MusicTournamentMissionView(),
                    new MissionSingleplayerUIHandler(),
                    ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
                    new MusicMissionView(new MusicBaseComponent[] { new MusicMissionTournamentComponent() }),
                    ViewCreator.CreateMissionAgentLabelUIHandler(mission),
                    new MissionItemContourControllerView(),
                    ViewCreator.CreatePhotoModeView()
                }.ToArray();
                return false;
            }
            return true;
        }
    }
}