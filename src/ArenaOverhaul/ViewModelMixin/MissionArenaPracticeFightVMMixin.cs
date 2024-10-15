using ArenaOverhaul.ArenaPractice;
using ArenaOverhaul.CampaignBehaviors.BehaviorManagers;
using ArenaOverhaul.Extensions;
using ArenaOverhaul.Helpers;

using Bannerlord.ButterLib.Common.Helpers;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;

using SandBox.Missions.MissionLogics.Arena;
using SandBox.ViewModelCollection.Missions;

using System;

using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace ArenaOverhaul.ViewModelMixin
{
    [ViewModelMixin(nameof(MissionArenaPracticeFightVM.UpdatePrizeText))]
    internal sealed class MissionArenaPracticeFightVMMixin(MissionArenaPracticeFightVM vm) : BaseViewModelMixin<MissionArenaPracticeFightVM>(vm)
    {
        private bool _isStandardPanelVisible;
        private bool _isParryPanelVisible;
        private bool _isTeamPanelVisible;
        private bool _isTeamSpawnPanelVisible;

        private bool _isSpecialPanelVisible;

        private string _successfulBlocksText = "";
        private string _perfectBlocksText = "";
        private string _chamberBlocksText = "";
        private string _hitsTakenText = "";
        private string _alliesRemainingText = "";
        private string _awaitingText;

        private readonly MissionArenaPracticeFightVM baseVM = vm;
        private readonly ArenaPracticeFightMissionController? _practiceMissionController = FieldAccessHelper.MAPFVMPracticeMissionControllerByRef(vm);

        [DataSourceProperty]
        public bool IsStandardPanelVisible
        {
            get => _isStandardPanelVisible;
            set
            {
                if (value != _isStandardPanelVisible)
                {
                    _isStandardPanelVisible = value;
                    OnPropertyChangedWithValue(value, nameof(IsStandardPanelVisible));
                }
            }
        }

        [DataSourceProperty]
        public bool IsSpecialPanelVisible
        {
            get => _isSpecialPanelVisible;
            set
            {
                if (value != _isSpecialPanelVisible)
                {
                    _isSpecialPanelVisible = value;
                    OnPropertyChangedWithValue(value, nameof(IsSpecialPanelVisible));
                }
            }
        }

        [DataSourceProperty]
        public bool IsParryPanelVisible
        {
            get => _isParryPanelVisible;
            set
            {
                if (value != _isParryPanelVisible)
                {
                    _isParryPanelVisible = value;
                    OnPropertyChangedWithValue(value, nameof(IsParryPanelVisible));
                }
            }
        }

        [DataSourceProperty]
        public bool IsTeamPanelVisible
        {
            get => _isTeamPanelVisible;
            set
            {
                if (value != _isTeamPanelVisible)
                {
                    _isTeamPanelVisible = value;
                    OnPropertyChangedWithValue(value, nameof(IsTeamPanelVisible));
                }
            }
        }

        [DataSourceProperty]
        public bool IsTeamSpawnPanelVisible
        {
            get => _isTeamSpawnPanelVisible;
            set
            {
                if (value != _isTeamSpawnPanelVisible)
                {
                    _isTeamSpawnPanelVisible = value;
                    OnPropertyChangedWithValue(value, nameof(IsTeamSpawnPanelVisible));
                }
            }
        }

        [DataSourceProperty]
        public string SuccessfulBlocksText
        {
            get => _successfulBlocksText;
            set
            {
                if (value != _successfulBlocksText)
                {
                    _successfulBlocksText = value;
                    OnPropertyChangedWithValue(value, nameof(SuccessfulBlocksText));
                }
            }
        }

        [DataSourceProperty]
        public string PerfectBlocksText
        {
            get => _perfectBlocksText;
            set
            {
                if (value != _perfectBlocksText)
                {
                    _perfectBlocksText = value;
                    OnPropertyChangedWithValue(value, nameof(PerfectBlocksText));
                }
            }
        }

        [DataSourceProperty]
        public string ChamberBlocksText
        {
            get => _chamberBlocksText;
            set
            {
                if (value != _chamberBlocksText)
                {
                    _chamberBlocksText = value;
                    OnPropertyChangedWithValue(value, nameof(ChamberBlocksText));
                }
            }
        }

        [DataSourceProperty]
        public string HitsTakenText
        {
            get => _hitsTakenText;
            set
            {
                if (value != _hitsTakenText)
                {
                    _hitsTakenText = value;
                    OnPropertyChangedWithValue(value, nameof(HitsTakenText));
                }
            }
        }

        [DataSourceProperty]
        public string AlliesRemainingText
        {
            get => _alliesRemainingText;
            set
            {
                if (value != _alliesRemainingText)
                {
                    _alliesRemainingText = value;
                    OnPropertyChangedWithValue(value, nameof(AlliesRemainingText));
                }
            }
        }

        [DataSourceProperty]
        public string AwaitingText
        {
            get => _awaitingText;
            set
            {
                if (value != _awaitingText)
                {
                    _awaitingText = value;
                    OnPropertyChangedWithValue(value, nameof(AwaitingText));
                }
            }
        }

        public override void OnRefresh()
        {
            UpdatePanelsVisibility();

            if (IsParryPanelVisible)
            {
                UpdateParryPanelStats();
            }

            if (IsTeamPanelVisible)
            {
                UpdateTeamPanelStats();
            }

            if (!IsParryPanelVisible)
            {
                UpdatePrizeText();
            }
        }

        private void UpdatePanelsVisibility()
        {
            bool isParryPractice = AOArenaBehaviorManager.Instance!.PracticeMode.Contains(ArenaPracticeMode.Parry);
            bool isTeamPractice = AOArenaBehaviorManager.Instance!.PracticeMode.Contains(ArenaPracticeMode.Team);

            IsStandardPanelVisible = baseVM.IsPlayerPracticing && !isParryPractice && !isTeamPractice;
            IsParryPanelVisible = baseVM.IsPlayerPracticing && isParryPractice;
            IsTeamPanelVisible = baseVM.IsPlayerPracticing && isTeamPractice;
            IsTeamSpawnPanelVisible = IsTeamPanelVisible && TeamPracticeController.CharacterObjectToSwitchTo != null;

            IsSpecialPanelVisible = IsParryPanelVisible || IsTeamPanelVisible;
        }

        private void UpdateParryPanelStats()
        {
            var successfulBlocks = new TextObject("{=PumaUhrt2}Prepared blocks: {PREPARED_BLOCKS}", new() { ["PREPARED_BLOCKS"] = ParryPracticeStatsManager.PreparedBlocks });
            var perfectBlocks = new TextObject("{=XiR0Srf50}Perfect blocks: {PERFECT_BLOCKS}", new() { ["PERFECT_BLOCKS"] = ParryPracticeStatsManager.PerfectBlocks });
            var chamberBlocks = new TextObject("{=R0UCmq6Jn}Chamber blocks: {CHAMBER_BLOCKS}", new() { ["CHAMBER_BLOCKS"] = ParryPracticeStatsManager.ChamberBlocks });
            var hitsTaken = new TextObject("{=CSGg2vSS3}Hits taken: {HITS_TAKEN}", new() { ["HITS_TAKEN"] = ParryPracticeStatsManager.HitsTaken });

            SuccessfulBlocksText = successfulBlocks.ToString();
            PerfectBlocksText = perfectBlocks.ToString();
            ChamberBlocksText = chamberBlocks.ToString();
            HitsTakenText = hitsTaken.ToString();
        }

        private void UpdateTeamPanelStats()
        {
            var alliesRemaining = new TextObject("{=l1z9vuw8B}Allies remaining: {ALLIES_REMAINING}", new() { ["ALLIES_REMAINING"] = TeamPracticeStatsManager.RemainingAlliesCount });
            AlliesRemainingText = alliesRemaining.ToString();

            GameTexts.SetVariable("BEATEN_OPPONENT_COUNT", _practiceMissionController!.OpponentCountBeatenByPlayer);
            baseVM.OpponentsBeatenText = GameTexts.FindText("str_beaten_opponent").ToString();

            if (IsTeamSpawnPanelVisible)
            {
                var characterToSwitchTo = TeamPracticeController.CharacterObjectToSwitchTo;
                var positionInLine = AOArenaBehaviorManager._lastPlayerRelatedCharacterList!.IndexOf(characterToSwitchTo!) - TeamPracticeStatsManager.SpawnedAliedAgentCount + 1;
                var awaitingTextObject = new TextObject("{=}Awaiting for {HERO.NAME} to enter arena. {?HERO.GENDER}She{?}He{\\?} is {POSITION_IN_LINE} in the line.", new() { ["POSITION_IN_LINE"] = positionInLine });
                LocalizationHelper.SetEntityProperties(awaitingTextObject, "HERO", characterToSwitchTo!.HeroObject);
                AwaitingText = awaitingTextObject.ToString();
            }
        }

        private void UpdatePrizeText()
        {
            int remainingOpponentCount = _practiceMissionController!.RemainingOpponentCount;
            int countBeatenByPlayer = _practiceMissionController!.OpponentCountBeatenByPlayer;

            int prizeAmount = PracticePrizeManager.GetPrizeAmount(remainingOpponentCount, countBeatenByPlayer);
            GameTexts.SetVariable("DENAR_AMOUNT", prizeAmount);
            GameTexts.SetVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
            baseVM.PrizeText = GameTexts.FindText("str_earned_denar", null).ToString();
        }
    }
}