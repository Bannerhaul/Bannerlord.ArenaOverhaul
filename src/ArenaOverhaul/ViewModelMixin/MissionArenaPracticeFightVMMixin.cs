using ArenaOverhaul.ArenaPractice;
using ArenaOverhaul.CampaignBehaviors.BehaviorManagers;
using ArenaOverhaul.Extensions;
using ArenaOverhaul.Helpers;

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

        private bool _isSpecialPanelVisible;


        private string _successfulBlocksText = "";
        private string _perfectBlocksText = "";
        private string _chamberBlocksText = "";
        private string _hitsTakenText = "";
        private string _alliesRemainingText = "";

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

            IsSpecialPanelVisible = IsParryPanelVisible || IsTeamPanelVisible;
        }

        private void UpdateParryPanelStats()
        {
            var successfulBlocks = new TextObject("{=}Prepared blocks: {PREPARED_BLOCKS}", new() { ["PREPARED_BLOCKS"] = ParryPracticeStatsManager.PreparedBlocks });
            var perfectBlocks = new TextObject("{=}Perfect blocks: {PERFECT_BLOCKS}", new() { ["PERFECT_BLOCKS"] = ParryPracticeStatsManager.PerfectBlocks });
            var chamberBlocks = new TextObject("{=}Chamber blocks: {CHAMBER_BLOCKS}", new() { ["CHAMBER_BLOCKS"] = ParryPracticeStatsManager.ChamberBlocks });
            var hitsTaken = new TextObject("{=}Hits taken: {HITS_TAKEN}", new() { ["HITS_TAKEN"] = ParryPracticeStatsManager.HitsTaken });

            SuccessfulBlocksText = successfulBlocks.ToString();
            PerfectBlocksText = perfectBlocks.ToString();
            ChamberBlocksText = chamberBlocks.ToString();
            HitsTakenText = hitsTaken.ToString();
        }

        private void UpdateTeamPanelStats()
        {
            var alliesRemaining = new TextObject("{=}Allies remaining: {ALLIES_REMAINING}", new() { ["ALLIES_REMAINING"] = TeamPracticeStatsManager.RemainingAlliesCount });
            AlliesRemainingText = alliesRemaining.ToString();

            GameTexts.SetVariable("BEATEN_OPPONENT_COUNT", _practiceMissionController!.OpponentCountBeatenByPlayer);
            baseVM.OpponentsBeatenText = GameTexts.FindText("str_beaten_opponent").ToString();
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