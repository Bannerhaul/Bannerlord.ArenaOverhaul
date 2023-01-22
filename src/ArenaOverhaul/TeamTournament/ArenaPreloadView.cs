using System.Collections.Generic;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade;

namespace ArenaOverhaul.TeamTournament
{
    internal class ArenaPreloadView : MissionView
    {
        private readonly PreloadHelper _helperInstance = new PreloadHelper();
        private bool _preloadDone;

        public override void OnPreMissionTick(float dt)
        {
            if (_preloadDone)
                return;
            List<BasicCharacterObject> characters = new();
            TeamTournamentBehavior missionBehavior = Mission.Current.GetMissionBehavior<TeamTournamentBehavior>();
            if (missionBehavior != null)
            {
                foreach (CharacterObject possibleParticipant in missionBehavior.GetAllPossibleParticipants())
                    characters.Add(possibleParticipant);
            }
            _helperInstance.PreloadCharacters(characters);
            _preloadDone = true;
        }

        public override void OnSceneRenderingStarted() => _helperInstance.WaitForMeshesToBeLoaded();

#if v100 || v101 || v102 || v103
        public override void OnMissionDeactivate()
        {
            base.OnMissionDeactivate();
            _helperInstance.Clear();
        }
#else
        public override void OnMissionStateDeactivated()
        {
            base.OnMissionStateDeactivated();
            _helperInstance.Clear();
        }
#endif
    }
}