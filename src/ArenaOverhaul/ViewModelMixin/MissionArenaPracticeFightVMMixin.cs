using ArenaOverhaul.ArenaPractice;
using ArenaOverhaul.CampaignBehaviors.BehaviorManagers;
using ArenaOverhaul.Extensions;
using ArenaOverhaul.Helpers;

using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;

using SandBox.Missions.MissionLogics.Arena;
using SandBox.ViewModelCollection.Missions;

using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace ArenaOverhaul.ViewModelMixin
{
    [ViewModelMixin(nameof(MissionArenaPracticeFightVM.UpdatePrizeText))]
    internal sealed class MissionArenaPracticeFightVMMixin : BaseViewModelMixin<MissionArenaPracticeFightVM>
    {
        private bool _isStandardPanelVisible;

        private bool _isParryPanelVisible;
        private bool _isSpecialPanelVisible;


        private string _successfulBlocksText = "";
        private string _perfectBlocksText = "";
        private string _chamberBlocksText = "";
        private string _hitsTakenText = "";

        private readonly MissionArenaPracticeFightVM baseVM;
        private readonly ArenaPracticeFightMissionController? _practiceMissionController;

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

        public MissionArenaPracticeFightVMMixin(MissionArenaPracticeFightVM vm) : base(vm)
        {
            baseVM = vm;
            _practiceMissionController = FieldAccessHelper.MAPFVMPracticeMissionControllerByRef(vm);
        }

        public override void OnRefresh()
        {
            UpdatePanelsVisibility();

            if (IsParryPanelVisible)
            {
                UpdateParryPanelStats();
            }

            if (!IsParryPanelVisible)
            {
                UpdatePrizeText();
            }
        }

        private void UpdatePanelsVisibility()
        {
            IsStandardPanelVisible = baseVM.IsPlayerPracticing && !AOArenaBehaviorManager.Instance!.PracticeMode.Contains(ArenaPracticeMode.Parry);
            IsParryPanelVisible = baseVM.IsPlayerPracticing && AOArenaBehaviorManager.Instance!.PracticeMode.Contains(ArenaPracticeMode.Parry);

            IsSpecialPanelVisible = IsParryPanelVisible;
        }

        private void UpdateParryPanelStats()
        {
            var successfulBlocks = new TextObject("{=}Prepared blocks: {PREPARED_BLOCKS}", new() { ["PREPARED_BLOCKS"] = ParryStatsManager.PreparedBlocks });
            var perfectBlocks = new TextObject("{=}Perfect blocks: {PERFECT_BLOCKS}", new() { ["PERFECT_BLOCKS"] = ParryStatsManager.PerfectBlocks });
            var chamberBlocks = new TextObject("{=}Chamber blocks: {CHAMBER_BLOCKS}", new() { ["CHAMBER_BLOCKS"] = ParryStatsManager.ChamberBlocks });
            var hitsTaken = new TextObject("{=}Hits taken: {HITS_TAKEN}", new() { ["HITS_TAKEN"] = ParryStatsManager.HitsTaken });

            SuccessfulBlocksText = successfulBlocks.ToString();
            PerfectBlocksText = perfectBlocks.ToString();
            ChamberBlocksText = chamberBlocks.ToString();
            HitsTakenText = hitsTaken.ToString();
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