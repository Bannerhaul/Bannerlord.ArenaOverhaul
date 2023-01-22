using HarmonyLib;

using SandBox.View;
using SandBox.View.Missions;
using SandBox.View.Missions.Sound.Components;
using SandBox.View.Missions.Tournaments;

using System;
using System.Collections.Generic;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Sound;
using TaleWorlds.MountAndBlade.View.MissionViews.Sound.Components;

namespace ArenaOverhaul.TeamTournament.Patches
{
    [HarmonyPatch(typeof(TournamentMissionViews), "OpenTournamentFightMission")]
    public static class TournamentMissionViewsOpenTournamentFightMissionPatch
    {
        public static bool Prefix(ref MissionView[] __result, Mission mission)
        {
            if (TeamTournamentInfo.Current != null && TeamTournamentInfo.Current.IsStarted)
            {
                __result = new List<MissionView>
                {
                    new MissionCampaignView(),
                    new MissionConversationCameraView(),
                    ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
                    ViewCreator.CreateOptionsUIHandler(),
                    ViewCreator.CreateMissionMainAgentEquipDropView(mission),
                    ViewCreatorManager.CreateMissionView<MissionGauntletTeamTournamentView>(false, null, Array.Empty<object>()), // this is patched!
                    new MissionAudienceHandler(0.4f + (MBRandom.RandomFloat * 0.6f)),
                    ViewCreator.CreateMissionAgentStatusUIHandler(mission),
                    ViewCreator.CreateMissionMainAgentEquipmentController(mission),
                    ViewCreator.CreateMissionMainAgentCheerBarkControllerView(mission),
                    ViewCreator.CreateMissionAgentLockVisualizerView(mission),
                    ViewCreator.CreateMissionSpectatorControlView(mission),
                    new MusicTournamentMissionView(),
                    new MissionSingleplayerViewHandler(),
                    ViewCreator.CreateSingleplayerMissionKillNotificationUIHandler(),
                    new MusicMissionView(new MusicBaseComponent[1] { new MusicMissionTournamentComponent() }),
                    ViewCreator.CreateMissionAgentLabelUIHandler(mission),
                    new MissionItemContourControllerView(),
                    new MissionCampaignBattleSpectatorView(),
                    ViewCreator.CreatePhotoModeView(),
                    new ArenaPreloadView(),
                }.ToArray();
                return false;
            }
            return true;
        }
    }
}