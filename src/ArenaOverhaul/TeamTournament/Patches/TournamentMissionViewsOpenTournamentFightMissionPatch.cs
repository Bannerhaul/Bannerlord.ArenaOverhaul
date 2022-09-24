using HarmonyLib;

#if e172
using SandBox.View;
#endif
using SandBox.View.Missions;
using SandBox.View.Missions.Tournaments;
#if !e172
using SandBox.View.Missions.Sound.Components;
#endif

using System;
using System.Collections.Generic;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
#if e172
using TaleWorlds.MountAndBlade.LegacyGUI.Missions;
using TaleWorlds.MountAndBlade.View.Missions;
#else
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Sound;
using TaleWorlds.MountAndBlade.View.MissionViews.Sound.Components;
#endif

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
#if e172
                    new CampaignMissionView(),
                    new ConversationCameraView(),
                    ViewCreator.CreateMissionSingleplayerEscapeMenu(CampaignOptions.IsIronmanMode),
                    ViewCreator.CreateOptionsUIHandler(),
                    ViewCreator.CreateMissionMainAgentEquipDropView(mission),
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
                    new CampaignBattleSpectatorView(),
                    ViewCreator.CreatePhotoModeView()
#else
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
                    ViewCreator.CreatePhotoModeView()/*,
                    new ArenaPreloadView()*/
#endif
                }.ToArray();
                return false;
            }
            return true;
        }
    }
}