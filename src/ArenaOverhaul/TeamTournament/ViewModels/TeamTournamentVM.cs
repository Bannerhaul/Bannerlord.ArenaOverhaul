using SandBox.ViewModelCollection;

using System;
using System.Collections.Generic;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
#if e172
using TaleWorlds.Core.ViewModelCollection;
#else
using TaleWorlds.Core.ViewModelCollection.Information;
#endif
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace ArenaOverhaul.TeamTournament.ViewModels
{
    public class TeamTournamentVM : ViewModel
    {
        public Action DisableUI { get; }
        public TeamTournamentBehavior Tournament { get; }

        public TeamTournamentVM(Action disableUI, TeamTournamentBehavior tournamentBehavior)
        {
            DisableUI = disableUI;
            CurrentMatch = new TeamTournamentMatchVM();

            Round1 = new TeamTournamentRoundVM();
            Round2 = new TeamTournamentRoundVM();
            Round3 = new TeamTournamentRoundVM();
            Round4 = new TeamTournamentRoundVM();

            _rounds = new List<TeamTournamentRoundVM>
            {
                Round1,
                Round2,
                Round3,
                Round4
            };

            _tournamentWinner = new TeamTournamentMemberVM();
            Tournament = tournamentBehavior;
            WinnerIntro = GameTexts.FindText("str_tournament_winner_intro", null).ToString();
            BattleRewards = new MBBindingList<TournamentRewardVM>();

            for (int i = 0; i < Tournament.Rounds.Length; i++)
                _rounds[i].Initialize(Tournament.Rounds[i], GameTexts.FindText("str_tournament_round", i.ToString()));

            Refresh();

            Tournament.TournamentEnd += OnTournamentEnd;
            Tournament.MatchEnd += OnMatchEnd;

            PrizeVisual = (HasPrizeItem ? new ImageIdentifierVM(Tournament.TournamentGame.Prize) : new ImageIdentifierVM(ImageIdentifierType.Null));
            _skipAllRoundsHint = new HintViewModel();
            RefreshValues();
        }

        private void OnMatchEnd(TeamTournamentMatch match)
        {
            if (ActiveRoundIndex < _rounds.Count + 1 && Tournament.NextRound != null)
            {
                _rounds[ActiveRoundIndex + 1].Initialize(Tournament.NextRound);
                RefreshValues();
            }
        }

        public override void RefreshValues()
        {
            base.RefreshValues();

            LeaveText = GameTexts.FindText("str_tournament_leave", null).ToString();
            SkipRoundText = GameTexts.FindText("str_tournament_skip_round", null).ToString();
            WatchRoundText = GameTexts.FindText("str_tournament_watch_round", null).ToString();
            JoinTournamentText = GameTexts.FindText("str_tournament_join_tournament", null).ToString();
            BetText = GameTexts.FindText("str_bet", null).ToString();
            AcceptText = GameTexts.FindText("str_accept", null).ToString();
            CancelText = GameTexts.FindText("str_cancel", null).ToString();
            TournamentWinnerTitle = GameTexts.FindText("str_tournament_winner_title", null).ToString();
            BetTitleText = GameTexts.FindText("str_wager", null).ToString();
            GameTexts.SetVariable("MAX_AMOUNT", Tournament.GetMaximumBet());
            GameTexts.SetVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
            BetDescriptionText = GameTexts.FindText("str_tournament_bet_description", null).ToString();
            TournamentPrizeText = GameTexts.FindText("str_tournament_prize", null).ToString();
            PrizeItemName = Tournament.TournamentGame.Prize.Name.ToString();
            MBTextManager.SetTextVariable("SETTLEMENT_NAME", Tournament.Settlement.Name, false);
            TournamentTitle = GameTexts.FindText("str_tournament", null).ToString();
            CurrentWagerText = GameTexts.FindText("str_tournament_current_wager", null).ToString();
            SkipAllRoundsHint.HintText = new TextObject("{=GaOE4bdd}Skip All Rounds");

            if (_round1 != null)
                _round1.RefreshValues();
            if (_round2 != null)
                _round2.RefreshValues();
            if (_round3 != null)
                _round3.RefreshValues();
            if (_round4 != null)
                _round4.RefreshValues();
            if (_currentMatch != null)
                _currentMatch.RefreshValues();
            if (_tournamentWinner != null)
                _tournamentWinner.RefreshValues();
        }

        private void RefreshBetProperties()
        {
            TextObject textObject = new("{=L9GnQvsq}Stake: {BETTED_DENARS}", null);
            textObject.SetTextVariable("BETTED_DENARS", Tournament.BettedDenars);
            BettedDenarsText = textObject.ToString();
            TextObject textObject2 = new("{=xzzSaN4b}Expected: {OVERALL_EXPECTED_DENARS}", null);
            textObject2.SetTextVariable("OVERALL_EXPECTED_DENARS", Tournament.OverallExpectedDenars);
            OverallExpectedDenarsText = textObject2.ToString();
            TextObject textObject3 = new("{=yF5fpwNE}Total: {TOTAL}", null);
            textObject3.SetTextVariable("TOTAL", Tournament.PlayerDenars);
            TotalDenarsText = textObject3.ToString();
            OnPropertyChanged("IsBetButtonEnabled");
            MaximumBetValue = Math.Min(Tournament.GetMaximumBet() - _thisRoundBettedAmount, Hero.MainHero.Gold);
            GameTexts.SetVariable("NORMALIZED_EXPECTED_GOLD", (int) (Tournament.BetOdd * 100f));
            GameTexts.SetVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
            BetOddsText = GameTexts.FindText("str_tournament_bet_odd", null).ToString();
        }

        private void OnNewRoundStarted(int prevRoundIndex, int currentRoundIndex)
        {
            _isPlayerParticipating = Tournament.IsPlayerParticipating;
            _thisRoundBettedAmount = 0;
        }

        public void Refresh()
        {
            IsCurrentMatchActive = false;
            CurrentMatch = _rounds[Tournament.CurrentRoundIndex].MatchVMs.FirstOrDefault(m => m.IsValid && m.Match == Tournament.CurrentMatch);
            ActiveRoundIndex = Tournament.CurrentRoundIndex;
            CanPlayerJoin = PlayerCanJoinMatch();
            OnPropertyChanged("IsTournamentIncomplete");
            OnPropertyChanged("InitializationOver");
            OnPropertyChanged("IsBetButtonEnabled");
            HasPrizeItem = (Tournament.TournamentGame.Prize != null && !IsOver);
        }

        /// <summary>
        /// TODO: make some better team winning interface
        ///       for now vanilla view with team-leader as winner
        /// </summary>
        private void OnTournamentEnd()
        {
            var winnerTeams = Tournament.LastMatch!.Teams.OrderByDescending(x => x.Score).ToList();
            var firstTeamLeader = new TeamTournamentMemberVM(winnerTeams.ElementAt(0).GetTeamLeader());
            var secondTeamLeader = new TeamTournamentMemberVM(winnerTeams.ElementAt(1).GetTeamLeader());
            TournamentWinner = firstTeamLeader;
            Town tournamentTown = Tournament.TournamentGame.Town;
            int renownReward = TournamentRewardManager.GetTakedownRenownReward(Hero.MainHero, tournamentTown);

            if (TournamentWinner.Member!.Character.IsHero)
            {
                Hero heroObject = TournamentWinner.Member.Character.HeroObject;
                TournamentWinner.Character.ArmorColor1 = heroObject.MapFaction.Color;
                TournamentWinner.Character.ArmorColor2 = heroObject.MapFaction.Color2;
            }
            else
            {
                CultureObject culture = TournamentWinner.Member.Character.Culture;
                TournamentWinner.Character.ArmorColor1 = culture.Color;
                TournamentWinner.Character.ArmorColor2 = culture.Color2;
            }

            IsWinnerHero = TournamentWinner.Member.Character.IsHero;
            if (IsWinnerHero)
                WinnerBanner = new ImageIdentifierVM(BannerCode.CreateFrom(TournamentWinner.Member.Character.HeroObject.ClanBanner), true);

            if (TournamentWinner.IsMainHero)
            {
                GameTexts.SetVariable("TOURNAMENT_FINAL_OPPONENT", (firstTeamLeader == TournamentWinner ? secondTeamLeader : firstTeamLeader).Name);
                WinnerIntro = GameTexts.FindText("str_tournament_result_won", null).ToString();

                if (Tournament.TournamentGame.TournamentWinRenown > 0f)
                {
                    GameTexts.SetVariable("RENOWN", Tournament.TournamentGame.TournamentWinRenown.ToString("F1"));
                    BattleRewards!.Add(new TournamentRewardVM(GameTexts.FindText("str_tournament_renown", null).ToString()));
                }

                if (Tournament.TournamentGame.TournamentWinInfluence > 0f)
                {
                    float tournamentWinInfluence = Tournament.TournamentGame.TournamentWinInfluence;
                    TextObject textObject = GameTexts.FindText("str_tournament_influence", null);
                    textObject.SetTextVariable("INFLUENCE", tournamentWinInfluence.ToString("F1"));
                    textObject.SetTextVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"7\">");
                    BattleRewards!.Add(new TournamentRewardVM(textObject.ToString()));
                }

                if (Tournament.TournamentGame.Prize != null)
                {
                    GameTexts.SetVariable("REWARD", Tournament.TournamentGame.Prize.Name.ToString());
                    BattleRewards!.Add(new TournamentRewardVM(GameTexts.FindText("str_tournament_reward", null).ToString(), new ImageIdentifierVM(Tournament.TournamentGame.Prize)));
                }

                if (Tournament.OverallExpectedDenars > 0)
                {
                    var overallExpectedDenars = Tournament.OverallExpectedDenars;
                    var textObject2 = GameTexts.FindText("str_tournament_bet", null);
                    textObject2.SetTextVariable("BET", overallExpectedDenars);
                    textObject2.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
                    BattleRewards!.Add(new TournamentRewardVM(textObject2.ToString()));
                }
            }
            else
            {
                if (firstTeamLeader.IsMainHero || secondTeamLeader.IsMainHero)
                {
                    GameTexts.SetVariable("TOURNAMENT_FINAL_OPPONENT", (firstTeamLeader == TournamentWinner ? firstTeamLeader : secondTeamLeader).Name);
                    WinnerIntro = GameTexts.FindText("str_tournament_result_eliminated_at_final", null).ToString();
                }
                else
                {
                    GameTexts.SetVariable("TOURNAMENT_FINAL_PARTICIPANT_A", (firstTeamLeader == TournamentWinner ? firstTeamLeader : secondTeamLeader).Name);
                    GameTexts.SetVariable("TOURNAMENT_FINAL_PARTICIPANT_B", (firstTeamLeader == TournamentWinner ? secondTeamLeader : firstTeamLeader).Name);

                    if (_isPlayerParticipating)
                    {
                        GameTexts.SetVariable("TOURNAMENT_ELIMINATED_ROUND", Tournament.PlayerTeamLostAtRound);
                        WinnerIntro = GameTexts.FindText("str_tournament_result_eliminated", null).ToString();
                    }
                    else
                        WinnerIntro = GameTexts.FindText("str_tournament_result_spectator", null).ToString();
                }
                if (renownReward > 0) //this is consolation prize. If player won, he would've the takedown renown reward included in champion's award
                {
                    GameTexts.SetVariable("RENOWN_TAKEDOWN_REWARD", renownReward.ToString());
                    BattleRewards!.Add(new TournamentRewardVM(GameTexts.FindText("str_tournament_renown_takedown_reward").ToString()));
                }
            }
            int playerGoldPrize = TournamentWinner.IsMainHero ? TournamentRewardManager.GetTournamentGoldPrize(tournamentTown) : 0;
            int playerRoundWinnings = TournamentRewardManager.RoundPrizeWinners[tournamentTown].FirstOrDefault(x => x.Participant.IsHumanPlayerCharacter).Winnings;
            if (playerGoldPrize > 0 || playerRoundWinnings > 0)
            {
                if (playerGoldPrize > 0)
                {
                    GameTexts.SetVariable("TOTAL_GOLD_REWARD", (playerGoldPrize + playerRoundWinnings).ToString());
                    BattleRewards!.Add(new TournamentRewardVM(GameTexts.FindText("str_tournament_gold_reward", "3").ToString()));
                }
                else
                {
                    GameTexts.SetVariable("PER_ROUND_REWARD", playerRoundWinnings.ToString());
                    BattleRewards!.Add(new TournamentRewardVM(GameTexts.FindText("str_tournament_gold_reward", TournamentWinner.IsMainHero ? "2" : (renownReward > 0 ? "1" : "0")).ToString()));
                }
            }
            IsOver = true;
        }

        private bool PlayerCanJoinMatch()
        {
            if (IsTournamentIncomplete)
                return Tournament.CurrentMatch!.IsPlayerParticipating;

            return false;
        }

        public void OnAgentRemoved(Agent agent)
        {
            if (IsCurrentMatchActive && agent.IsHuman && _currentMatch != null)
            {
                var teamVM = _currentMatch.Teams.FirstOrDefault(x => x.Team!.Members.Any(m => m.Descriptor.CompareTo(agent.Origin.UniqueSeed) == 0));
                if (teamVM.Team != null && !teamVM.Team.IsAlive)
                    teamVM.GetTeamLeader().IsDead = true;
            }
        }

#region view commands
#pragma warning disable IDE0051 // Remove unused private members
        /// <summary>
        /// DO NOT REMOVE
        /// </summary>
        private void ExecuteShowPrizeItemTooltip()
        {
            if (HasPrizeItem)
            {
#if e172
                InformationManager.AddTooltipInformation(typeof(ItemObject), new object[]
#else
                InformationManager.ShowTooltip(typeof(ItemObject), new object[]
#endif
                {
                    new EquipmentElement(Tournament.TournamentGame.Prize, null, null, false)
                });
            }
        }

        /// <summary>
        /// DO NOT REMOVE
        /// </summary>
        private void ExecuteHidePrizeItemTooltip()
        {
#if e172
            InformationManager.HideInformations();
#else
            InformationManager.HideTooltip();
#endif
        }

        /// <summary>
        /// DO NOT REMOVE
        /// </summary>
        private void ExecuteBet()
        {
            _thisRoundBettedAmount += WageredDenars;
            Tournament.PlaceABet(WageredDenars);
            RefreshBetProperties();
        }

        /// <summary>
        /// DO NOT REMOVE
        /// </summary>
        private void ExecuteJoinTournament()
        {
            if (PlayerCanJoinMatch())
            {
                Tournament.StartMatch();
                IsCurrentMatchActive = true;
                CurrentMatch!.Refresh(true);
                CurrentMatch!.State = 3;
                DisableUI();
                IsCurrentMatchActive = true;
            }
        }

        /// <summary>
        /// DO NOT REMOVE
        /// </summary>
        private void ExecuteSkipRound()
        {
            if (IsTournamentIncomplete)
            {
                Tournament.SkipMatch();
            }
            Refresh();
        }

        /// <summary>
        /// DO NOT REMOVE
        /// </summary>
        public void ExecuteSkipAllRounds()
        {
            int num = 0;
            for (int index = Tournament.Rounds.Sum(r => r.Matches.Count()); !CanPlayerJoin && Tournament.CurrentRound != null && Tournament.CurrentRound.CurrentMatch != null && num < index; ++num)
            {
                ExecuteSkipRound();
            }
        }

        /// <summary>
        /// DO NOT REMOVE
        /// </summary>
        private void ExecuteWatchRound()
        {
            if (!PlayerCanJoinMatch())
            {
                Tournament.StartMatch();
                IsCurrentMatchActive = true;
                CurrentMatch!.Refresh(true);
                CurrentMatch!.State = 3;
                DisableUI();
                IsCurrentMatchActive = true;
            }
        }

        /// <summary>
        /// DO NOT REMOVE
        /// </summary>
        private void ExecuteLeave()
        {
            if (CurrentMatch != null)
            {
                List<TeamTournamentMatch> forthcomingMatches = new List<TeamTournamentMatch>();
                for (int currentRoundIndex = Tournament.CurrentRoundIndex; currentRoundIndex < Tournament.Rounds.Length; ++currentRoundIndex)
                {
                    forthcomingMatches.AddRange(Tournament.Rounds[currentRoundIndex].Matches.Where(x => x.State != TournamentMatch.MatchState.Finished));
                }
                if (forthcomingMatches.Any(x => x.IsPlayerParticipating))
                {
                    InformationManager.ShowInquiry(new InquiryData(
                      GameTexts.FindText("str_forfeit", null).ToString(),
                      GameTexts.FindText("str_tournament_forfeit_game").ToString(),
                      true,
                      true,
                      GameTexts.FindText("str_yes", null).ToString(),
                      GameTexts.FindText("str_no", null).ToString(),
                      ExitFinishTournament,
                      null),
                    true);
                    return; //do nothing on "No" answer
                }
            }
            ExitFinishTournament();
        }

        private void ExitFinishTournament()
        {
            Tournament.EndTournamentViaLeave();
            Mission.Current.EndMission();
        }

        [DataSourceProperty]
        public HintViewModel SkipAllRoundsHint
        {
            get => _skipAllRoundsHint;
            set
            {
                if (value == _skipAllRoundsHint)
                    return;
                _skipAllRoundsHint = value;
                OnPropertyChangedWithValue(value, nameof(SkipAllRoundsHint));
            }
        }

#pragma warning restore IDE0051 // Remove unused private members
#endregion

#region view properties
        [DataSourceProperty]
        public string TournamentWinnerTitle
        {
            get => _tournamentWinnerTitle;
            set
            {
                if (value != _tournamentWinnerTitle)
                {
                    _tournamentWinnerTitle = value;
                    OnPropertyChangedWithValue(value, "TournamentWinnerTitle");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentMemberVM? TournamentWinner
        {
            get => _tournamentWinner;
            set
            {
                if (value != _tournamentWinner)
                {
                    _tournamentWinner = value;
                    OnPropertyChangedWithValue(value, "TournamentWinner");
                }
            }
        }

        [DataSourceProperty]
        public int MaximumBetValue
        {
            get => _maximumBetValue;
            set
            {
                if (value != _maximumBetValue)
                {
                    _maximumBetValue = value;
                    OnPropertyChangedWithValue(value, "MaximumBetValue");
                    _wageredDenars = -1;
                    WageredDenars = 0;
                }
            }
        }

        [DataSourceProperty]
        public bool IsBetButtonEnabled => PlayerCanJoinMatch() && Tournament.GetMaximumBet() > _thisRoundBettedAmount && Hero.MainHero.Gold > 0;

        [DataSourceProperty]
        public string BetText
        {
            get => _betText;
            set
            {
                if (value != _betText)
                {
                    _betText = value;
                    OnPropertyChangedWithValue(value, "BetText");
                }
            }
        }

        [DataSourceProperty]
        public string BetTitleText
        {
            get => _betTitleText;
            set
            {
                if (value != _betTitleText)
                {
                    _betTitleText = value;
                    OnPropertyChangedWithValue(value, "BetTitleText");
                }
            }
        }

        [DataSourceProperty]
        public string CurrentWagerText
        {
            get => _currentWagerText;
            set
            {
                if (value != _currentWagerText)
                {
                    _currentWagerText = value;
                    OnPropertyChangedWithValue(value, "CurrentWagerText");
                }
            }
        }

        [DataSourceProperty]
        public string BetDescriptionText
        {
            get => _betDescriptionText;
            set
            {
                if (value != _betDescriptionText)
                {
                    _betDescriptionText = value;
                    OnPropertyChangedWithValue(value, "BetDescriptionText");
                }
            }
        }

        [DataSourceProperty]
        public ImageIdentifierVM? PrizeVisual
        {
            get => _prizeVisual;
            set
            {
                if (value != _prizeVisual)
                {
                    _prizeVisual = value;
                    OnPropertyChangedWithValue(value, "PrizeVisual");
                }
            }
        }

        [DataSourceProperty]
        public string PrizeItemName
        {
            get => _prizeItemName;
            set
            {
                if (value != _prizeItemName)
                {
                    _prizeItemName = value;
                    OnPropertyChangedWithValue(value, "PrizeItemName");
                }
            }
        }

        [DataSourceProperty]
        public string TournamentPrizeText
        {
            get => _tournamentPrizeText;
            set
            {
                if (value != _tournamentPrizeText)
                {
                    _tournamentPrizeText = value;
                    OnPropertyChangedWithValue(value, "TournamentPrizeText");
                }
            }
        }

        [DataSourceProperty]
        public int WageredDenars
        {
            get => _wageredDenars;
            set
            {
                if (value != _wageredDenars)
                {
                    _wageredDenars = value;
                    OnPropertyChangedWithValue(value, "WageredDenars");
                    ExpectedBetDenars = ((_wageredDenars == 0) ? 0 : Tournament.GetExpectedDenarsForBet(_wageredDenars));
                }
            }
        }

        [DataSourceProperty]
        public int ExpectedBetDenars
        {
            get => _expectedBetDenars;
            set
            {
                if (value != _expectedBetDenars)
                {
                    _expectedBetDenars = value;
                    OnPropertyChangedWithValue(value, "ExpectedBetDenars");
                }
            }
        }

        [DataSourceProperty]
        public string BetOddsText
        {
            get => _betOddsText;
            set
            {
                if (value != _betOddsText)
                {
                    _betOddsText = value;
                    OnPropertyChangedWithValue(value, "BetOddsText");
                }
            }
        }

        [DataSourceProperty]
        public string BettedDenarsText
        {
            get => _bettedDenarsText;
            set
            {
                if (value != _bettedDenarsText)
                {
                    _bettedDenarsText = value;
                    OnPropertyChangedWithValue(value, "BettedDenarsText");
                }
            }
        }

        [DataSourceProperty]
        public string OverallExpectedDenarsText
        {
            get => _overallExpectedDenarsText;
            set
            {
                if (value != _overallExpectedDenarsText)
                {
                    _overallExpectedDenarsText = value;
                    OnPropertyChangedWithValue(value, "OverallExpectedDenarsText");
                }
            }
        }

        [DataSourceProperty]
        public string CurrentExpectedDenarsText
        {
            get => _currentExpectedDenarsText;
            set
            {
                if (value != _currentExpectedDenarsText)
                {
                    _currentExpectedDenarsText = value;
                    OnPropertyChangedWithValue(value, "CurrentExpectedDenarsText");
                }
            }
        }

        [DataSourceProperty]
        public string TotalDenarsText
        {
            get => _totalDenarsText;
            set
            {
                if (value != _totalDenarsText)
                {
                    _totalDenarsText = value;
                    OnPropertyChangedWithValue(value, "TotalDenarsText");
                }
            }
        }

        [DataSourceProperty]
        public string AcceptText
        {
            get => _acceptText;
            set
            {
                if (value != _acceptText)
                {
                    _acceptText = value;
                    OnPropertyChangedWithValue(value, "AcceptText");
                }
            }
        }

        [DataSourceProperty]
        public string CancelText
        {
            get => _cancelText;
            set
            {
                if (value != _cancelText)
                {
                    _cancelText = value;
                    OnPropertyChangedWithValue(value, "CancelText");
                }
            }
        }

        [DataSourceProperty]
        public bool IsCurrentMatchActive
        {
            get => _isCurrentMatchActive;
            set
            {
                _isCurrentMatchActive = value;
                OnPropertyChangedWithValue(value, "IsCurrentMatchActive");
            }
        }

        [DataSourceProperty]
        public TeamTournamentMatchVM? CurrentMatch
        {
            get => _currentMatch;
            set
            {
                if (value != _currentMatch)
                {
                    if (_currentMatch != null && _currentMatch.IsValid)
                    {
                        _currentMatch.State = 2;
                        _currentMatch.Refresh(false);

                        int index = _rounds.FindIndex(r => r.MatchVMs.Any(m => m.Match == Tournament.LastMatch));

                        if (index < Tournament.Rounds.Length - 1)
                            _rounds[index + 1].Initialize();
                    }

                    _currentMatch = value;
                    OnPropertyChangedWithValue(value, "CurrentMatch");

                    if (_currentMatch != null)
                        _currentMatch.State = 1;
                }
            }
        }

        [DataSourceProperty]
        public bool IsTournamentIncomplete => Tournament == null || Tournament.CurrentMatch != null;

        [DataSourceProperty]
        public int ActiveRoundIndex
        {
            get => _activeRoundIndex;
            set
            {
                if (value != _activeRoundIndex)
                {
                    OnNewRoundStarted(_activeRoundIndex, value);
                    _activeRoundIndex = value;
                    OnPropertyChangedWithValue(value, "ActiveRoundIndex");
                    RefreshBetProperties();
                }
            }
        }

        [DataSourceProperty]
        public bool CanPlayerJoin
        {
            get => _canPlayerJoin;
            set
            {
                if (value != _canPlayerJoin)
                {
                    _canPlayerJoin = value;
                    OnPropertyChangedWithValue(value, "CanPlayerJoin");
                }
            }
        }

        [DataSourceProperty]
        public bool HasPrizeItem
        {
            get => _hasPrizeItem;
            set
            {
                if (value != _hasPrizeItem)
                {
                    _hasPrizeItem = value;
                    OnPropertyChangedWithValue(value, "HasPrizeItem");
                }
            }
        }

        [DataSourceProperty]
        public string JoinTournamentText
        {
            get => _joinTournamentText;
            set
            {
                if (value != _joinTournamentText)
                {
                    _joinTournamentText = value;
                    OnPropertyChangedWithValue(value, "JoinTournamentText");
                }
            }
        }

        [DataSourceProperty]
        public string SkipRoundText
        {
            get => _skipRoundText;
            set
            {
                if (value != _skipRoundText)
                {
                    _skipRoundText = value;
                    OnPropertyChangedWithValue(value, "SkipRoundText");
                }
            }
        }

        [DataSourceProperty]
        public string WatchRoundText
        {
            get => _watchRoundText;
            set
            {
                if (value != _watchRoundText)
                {
                    _watchRoundText = value;
                    OnPropertyChangedWithValue(value, "WatchRoundText");
                }
            }
        }

        [DataSourceProperty]
        public string LeaveText
        {
            get => _leaveText;
            set
            {
                if (value != _leaveText)
                {
                    _leaveText = value;
                    OnPropertyChangedWithValue(value, "LeaveText");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentRoundVM? Round1
        {
            get => _round1;
            set
            {
                if (value != _round1)
                {
                    _round1 = value;
                    OnPropertyChangedWithValue(value, "Round1");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentRoundVM? Round2
        {
            get => _round2;
            set
            {
                if (value != _round2)
                {
                    _round2 = value;
                    OnPropertyChangedWithValue(value, "Round2");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentRoundVM? Round3
        {
            get => _round3;
            set
            {
                if (value != _round3)
                {
                    _round3 = value;
                    OnPropertyChangedWithValue(value, "Round3");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentRoundVM? Round4
        {
            get
            {
                return _round4;
            }
            set
            {
                if (value != _round4)
                {
                    _round4 = value;
                    OnPropertyChangedWithValue(value, "Round4");
                }
            }
        }

        [DataSourceProperty]
        public bool InitializationOver
        {
            get
            {
                return true;
            }
        }

        [DataSourceProperty]
        public string TournamentTitle
        {
            get => _tournamentTitle;
            set
            {
                if (value != _tournamentTitle)
                {
                    _tournamentTitle = value;
                    OnPropertyChangedWithValue(value, "TournamentTitle");
                }
            }
        }

        [DataSourceProperty]
        public bool IsOver
        {
            get => _isOver;
            set
            {
                if (_isOver != value)
                {
                    _isOver = value;
                    OnPropertyChangedWithValue(value, "IsOver");
                }
            }
        }

        [DataSourceProperty]
        public string WinnerIntro
        {
            get => _winnerIntro;
            set
            {
                if (value != _winnerIntro)
                {
                    _winnerIntro = value;
                    OnPropertyChangedWithValue(value, "WinnerIntro");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<TournamentRewardVM>? BattleRewards
        {
            get => _battleRewards;
            set
            {
                if (value != _battleRewards)
                {
                    _battleRewards = value;
                    OnPropertyChangedWithValue(value, "BattleRewards");
                }
            }
        }

        [DataSourceProperty]
        public bool IsWinnerHero
        {
            get
            {
                return _isWinnerHero;
            }
            set
            {
                if (value != _isWinnerHero)
                {
                    _isWinnerHero = value;
                    base.OnPropertyChangedWithValue(value, "IsWinnerHero");
                }
            }
        }

        [DataSourceProperty]
        public ImageIdentifierVM? WinnerBanner
        {
            get
            {
                return _winnerBanner;
            }
            set
            {
                if (value != _winnerBanner)
                {
                    _winnerBanner = value;
                    base.OnPropertyChangedWithValue(value, "WinnerBanner");
                }
            }
        }

#endregion view properties

        private readonly List<TeamTournamentRoundVM> _rounds;
        private int _thisRoundBettedAmount;
        private bool _isPlayerParticipating;
        private TeamTournamentRoundVM? _round1;
        private TeamTournamentRoundVM? _round2;
        private TeamTournamentRoundVM? _round3;
        private TeamTournamentRoundVM? _round4;
        private int _activeRoundIndex = -1;
        private string _joinTournamentText = string.Empty;
        private string _skipRoundText = string.Empty;
        private string _watchRoundText = string.Empty;
        private string _leaveText = string.Empty;
        private bool _canPlayerJoin;
        private TeamTournamentMatchVM? _currentMatch;
        private bool _isCurrentMatchActive;
        private string _betTitleText = string.Empty;
        private string _betDescriptionText = string.Empty;
        private string _betOddsText = string.Empty;
        private string _bettedDenarsText = string.Empty;
        private string _overallExpectedDenarsText = string.Empty;
        private string _currentExpectedDenarsText = string.Empty;
        private string _totalDenarsText = string.Empty;
        private string _acceptText = string.Empty;
        private string _cancelText = string.Empty;
        private string _prizeItemName = string.Empty;
        private string _tournamentPrizeText = string.Empty;
        private string _currentWagerText = string.Empty;
        private int _wageredDenars = -1;
        private int _expectedBetDenars = -1;
        private string _betText = string.Empty;
        private int _maximumBetValue;
        private string _tournamentWinnerTitle = string.Empty;
        private TeamTournamentMemberVM? _tournamentWinner;
        private string _tournamentTitle = string.Empty;
        private bool _isOver;
        private bool _hasPrizeItem;
        private bool _isWinnerHero;
        private string _winnerIntro = string.Empty;
        private ImageIdentifierVM? _prizeVisual;
        private ImageIdentifierVM? _winnerBanner;
        private MBBindingList<TournamentRewardVM>? _battleRewards;
        private HintViewModel _skipAllRoundsHint;
    }
}