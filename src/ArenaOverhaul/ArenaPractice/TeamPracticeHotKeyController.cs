using ArenaOverhaul.CampaignBehaviors.BehaviorManagers;
using ArenaOverhaul.Extensions;
using ArenaOverhaul.Helpers;

using Bannerlord.ButterLib.Common.Helpers;
using Bannerlord.ButterLib.HotKeys;

using SandBox.Missions.MissionLogics.Arena;

using System.Collections.Generic;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

using HotKeyManager = Bannerlord.ButterLib.HotKeys.HotKeyManager;

namespace ArenaOverhaul.ArenaPractice
{
    public class TeamPracticeHotKeyController : HotKeyBase
    {
        private bool _isInquiryActive = false;

        protected override string DisplayName { get; }
        protected override string Description { get; }
        protected override InputKey DefaultKey { get; }
        protected override string Category { get; }

        public TeamPracticeHotKeyController() : base(nameof(TeamPracticeHotKeyController))
        {
            DisplayName = "{=DXiVYMIUL}Switch to other hero";
            Description = "{=morP28atv}Switches to another active hero of your choice in a Team Practice match in the arena.";
            DefaultKey = InputKey.Slash;
            Category = HotKeyManager.Categories[HotKeyCategory.Action];

            Predicate = () => Mission.Current?.GetMissionBehavior<ArenaPracticeFightMissionController>()?.IsPlayerPracticing ?? false && (AOArenaBehaviorManager.Instance?.PracticeMode ?? ArenaPracticeMode.Standard) == ArenaPracticeMode.Team;
        }

        protected override void OnReleased()
        {
            if (_isInquiryActive)
            {
                return;
            }

            var mission = Mission.Current;
            var mainAgent = mission.MainAgent;
            if (GetSwitchingAvailability(mainAgent))
            {
                _isInquiryActive = true;

                var inquiryElementList = new List<InquiryElement>();
                mission.PlayerTeam.ActiveAgents
                    .Where(IsFittingToSwitchTo).ToList()
                    .ForEach(x => inquiryElementList.Add(new InquiryElement(x, x.Name.ToString(), new ImageIdentifier(CharacterCode.CreateFrom(x.Character)))));

                for (var i = TeamPracticeStatsManager.SpawnedAliedAgentCount; i < AOArenaBehaviorManager._lastPlayerRelatedCharacterList!.Count; i++)
                {
                    var characterObject = AOArenaBehaviorManager._lastPlayerRelatedCharacterList![i];
                    if (characterObject.IsHero)
                    {
                        TextObject hint = new("{=}This hero is not yet in the fight and is {QUEUE_NUMBER} in line to enter the arena.");
                        LocalizationHelper.SetNumericVariable(hint, "QUEUE_NUMBER", i - TeamPracticeStatsManager.SpawnedAliedAgentCount + 1);
                        inquiryElementList.Add(new InquiryElement(characterObject, characterObject.Name.ToString(), new ImageIdentifier(CharacterCode.CreateFrom(characterObject)), true, hint.ToString()));
                    }
                }

                if (inquiryElementList.Count > 0)
                {
                    MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData("{=xRdn4pEme}Available characters".ToLocalizedString(),
                        "{=jq2jXUcse}Select a teammate to switch to".ToLocalizedString(), inquiryElementList, true, 1, 1, GameTexts.FindText("str_ok").ToString(), GameTexts.FindText("str_cancel").ToString(), inquiryElements =>
                        {
                            if (inquiryElements is not null && inquiryElements.Count > 0)
                            {
                                switch (inquiryElements.First().Identifier)
                                {
                                    case CharacterObject character:
                                        TeamPracticeController.CharacterObjectToSwitchTo = character;
                                        break;
                                    case Agent agent:
                                        if (mainAgent != null)
                                        {
                                            mainAgent.Controller = Agent.ControllerType.AI;
                                            mainAgent.SetWatchState(Agent.WatchState.Alarmed);
                                        }
                                        agent.Controller = Agent.ControllerType.Player;
                                        break;
                                    default:
                                        break;
                                }
                            }
                            _isInquiryActive = false;
                        }, inquiryElements => _isInquiryActive = false, ""), true);
                }
                else
                {
                    _isInquiryActive = false;
                    MessageHelper.TechnicalMessage("{=XZcFEonSW}There are no available characters on your team to switch to!".ToLocalizedString());
                }
            }
            else
            {
                MessageHelper.TechnicalMessage("{=Iz2BWpesF}Currently you cannot switch to another character!".ToLocalizedString());
            }
        }

        private static bool GetSwitchingAvailability(Agent mainAgent)
        {
            return mainAgent is null || !mainAgent.IsActive();
        }

        private static bool IsFittingToSwitchTo(Agent agent)
        {
            return agent.IsHero && agent.IsAIControlled;
        }

        internal static bool HasTargetsToSwitchTo() => Mission.Current.PlayerTeam.ActiveAgents.Any(IsFittingToSwitchTo);
    }
}