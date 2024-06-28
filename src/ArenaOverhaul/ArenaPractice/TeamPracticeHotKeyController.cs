using ArenaOverhaul.CampaignBehaviors.BehaviorManagers;
using ArenaOverhaul.Extensions;
using ArenaOverhaul.Helpers;

using Bannerlord.ButterLib.HotKeys;

using SandBox.Missions.MissionLogics.Arena;

using System.Collections.Generic;
using System.Linq;

using TaleWorlds.Core;
using TaleWorlds.InputSystem;
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
            DisplayName = "{=}Switch to other hero";
            Description = "{=}Switches to another active hero of your choice in a Team Practice match in the arena.";
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
                if (mission.PlayerTeam.ActiveAgents.Any(IsFittingToSwitchTo))
                {
                    _isInquiryActive = true;

                    var inquiryElementList = new List<InquiryElement>();
                    mission.PlayerTeam.ActiveAgents
                        .Where(IsFittingToSwitchTo).ToList()
                        .ForEach(x => inquiryElementList.Add(new InquiryElement(x, x.Name.ToString(), new ImageIdentifier(CharacterCode.CreateFrom(x.Character)))));

                    MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData("{=}Available characters".ToLocalizedString(),
                        "{=}Select a teammate to switch to".ToLocalizedString(), inquiryElementList, true, 1, 1, GameTexts.FindText("str_ok").ToString(), GameTexts.FindText("str_cancel").ToString(), inquiryElements =>
                        {
                            if (inquiryElements is null || inquiryElements.Count <= 0 || inquiryElements.First().Identifier is not Agent agent)
                            {
                                return;
                            }
                            if (mainAgent != null)
                            {
                                mainAgent.Controller = Agent.ControllerType.AI;
                                mainAgent.SetWatchState(Agent.WatchState.Alarmed);
                            }
                            agent.Controller = Agent.ControllerType.Player;
                            _isInquiryActive = false;
                        }, inquiryElements => _isInquiryActive = false, ""), true);
                }
                else
                {
                    MessageHelper.TechnicalMessage("{=}There are no available characters on your team to switch to!".ToLocalizedString());
                }
            }
            else
            {
                MessageHelper.TechnicalMessage("{=}Currently you cannot switch to another character!".ToLocalizedString());
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
