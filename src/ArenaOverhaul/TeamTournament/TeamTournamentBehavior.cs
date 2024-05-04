using ArenaOverhaul.Helpers;

using SandBox.Tournaments;

using System;
using System.Collections.Generic;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

using MathF= TaleWorlds.Library.MathF;

namespace ArenaOverhaul.TeamTournament
{
    public class TeamTournamentBehavior : MissionLogic, ICameraModeLogic
    {
        public TournamentGame TournamentGame { get; private set; }
        public int PlayerTeamLostAtRound { get => _playerLostAtRound; }
        public bool IsPlayerEliminated { get; private set; }
        public int CurrentRoundIndex { get; private set; } = -1;
        public TeamTournamentMatch? LastMatch { get; private set; }
        public TeamTournamentRound[] Rounds { get; private set; } = new TeamTournamentRound[TeamTournamentInfo.Current!.Rounds];
        public TeamTournamentMember? Winner { get; private set; }
        public bool IsPlayerParticipating { get; }
        public Settlement Settlement { get; private set; }
        public float BetOdd { get; private set; }
        public int BettedDenars { get; private set; }
        public int OverallExpectedDenars { get; private set; }
        private TeamTournamentInfo CurrentInfo { get; set; } = TeamTournamentInfo.Current!;
        private TeamTournamentMissionController MissionBehavior { get; set; }

        public SpectatorCameraTypes GetMissionCameraLockMode(bool lockedToMainPlayer)
          => !IsPlayerParticipating ? SpectatorCameraTypes.LockToAnyAgent : SpectatorCameraTypes.Invalid;
        public TeamTournamentRound CurrentRound => Rounds[CurrentRoundIndex];
        public TeamTournamentRound? NextRound => CurrentRoundIndex < Rounds.Length - 1 ? Rounds[CurrentRoundIndex + 1] : null;
        public TeamTournamentMatch? CurrentMatch => CurrentRound?.CurrentMatch;
        public int PlayerDenars => Hero.MainHero.Gold;
        public int MaximumBetInstance => Math.Min(150, PlayerDenars);

        public TeamTournamentBehavior(TournamentGame tournamentGame, Settlement settlement, ITournamentGameBehavior gameBehavior, bool isPlayerParticipating)
        {
            Settlement = settlement;
            TournamentGame = tournamentGame;
            MissionBehavior = (gameBehavior as TeamTournamentMissionController)!;
            CreateTeams();
            LastMatch = null;
            Winner = null;
            IsPlayerParticipating = isPlayerParticipating;
        }

        private void CreateTeams()
        {
            // first we take in our selected team
            var mainParticipants = new List<TeamTournamentMember>();
            foreach (var flattenedTroopRosterElement in CurrentInfo.SelectedRoster!.ToFlattenedRoster())
                mainParticipants.Add(new TeamTournamentMember(flattenedTroopRosterElement.Troop));
            _teams = new List<TeamTournamentTeam>() {
                new TeamTournamentTeam(mainParticipants, 0, Hero.MainHero.ClanBanner, Hero.MainHero.ClanBanner.GetPrimaryColor())
            };

            // create rest
            CreateTournamentTeams();
        }

        private bool IsAlreadySelected(CharacterObject c)
        {
            return _teams!.TrueForAll(x => x.Members.All(y => y.Character == c));
        }

        private void AddTournamentTeam(IEnumerable<TeamTournamentMember> members, Banner? teamBanner = null, uint teamColor = 0, TeamTournamentMember? leader = null)
        {
            TeamTournamentMember? localLeader = leader;
            if (localLeader is null)
            {
                localLeader =
                  members.Where(x => x.Character.IsHero).OrderByDescending(x => x.Character.GetBattlePower()).FirstOrDefault()
                  ?? members.OrderByDescending(x => x.Character.GetBattlePower()).First();
            }

            int teamIndex = 0;
            if (!localLeader.Character.IsHero)
            {
                List<TeamTournamentTeam> similarTeams = _teams!.Where(x => x.GetTeamLeader().Character.Name == localLeader.Character.Name).ToList();
                teamIndex = similarTeams.Any() ? similarTeams.Max(x => x.TeamIndex) + 1 : 0;
            }

            _teams!.Add(new TeamTournamentTeam(members, teamIndex, teamBanner, teamColor, leader));
        }

        private void CreateTournamentTeams()
        {
            var totalTroopsNeeded = (CurrentInfo.TeamsCount - 1) * CurrentInfo.TeamSize;

            //All Heroes
            //Primary sort by isPartyLeader - they will have preference to being in charge of teams
            //Primary sort by isLord - non-party leader lords (player companions)
            //Secondary sort is by battle power
            var combatantHeroes = Settlement.GetCombatantHeroesInSettlement().
                Where(x => !IsAlreadySelected(x) && !CurrentInfo.SelectedRoster!.Contains(x)).
                OrderByDescending(x => x.GetBattlePower()).
                OrderByDescending(x => x.HeroObject.IsLord).
                OrderByDescending(x => x.HeroObject.IsPartyLeader);

            //All Regular Troops
            //ownership of troops will not be taken into account when generating teams
            var garrisonTroops = Settlement.Town.GarrisonParty?.MemberRoster.GetTroopRoster().
                Where(x => !x.Character.IsHero && x.WoundedNumber < x.Number && x.Character.CanBeAParticipant(true)).
                Select(x => x.Character).ToList()
                ?? new List<CharacterObject>();

            foreach (var hero in Settlement.GetHeroesInSettlement().
                Where(x => x.HeroObject.IsPartyLeader && !x.HeroObject.IsPrisoner))
            {
                garrisonTroops.AddRange(hero.HeroObject.PartyBelongedTo.MemberRoster.GetTroopRoster().
                    Where(y => !y.Character.IsHero && y.WoundedNumber < y.Number && y.Character.CanBeAParticipant(true)).
                    Select(y => y.Character));
            }
            var distinctTroops = garrisonTroops.
                Distinct().
                OrderByDescending(x => x.GetBattlePower());

            //Create list of combatants
            var charsToUse = combatantHeroes.
                Take(totalTroopsNeeded).
                Concat(distinctTroops.Take(totalTroopsNeeded - combatantHeroes.Count()));

            //Keep adding troops until we have enough to fill the tournament
            while (charsToUse.Count() < totalTroopsNeeded)
                charsToUse = charsToUse.Concat(distinctTroops.Take(totalTroopsNeeded - charsToUse.Count()));

            //Now create the Teams
            List<List<TeamTournamentMember>> teams = new();
            for (int n = 0; n < CurrentInfo.TeamsCount - 1; n++) teams.Add(new List<TeamTournamentMember>());

            //Place troops in teams in a zig-zag pattern to try to make the team strength as even as possible
            int teamNum = 0;
            int increment = 1;
            foreach (var troop in charsToUse)
            {
                teams[teamNum].Add(new TeamTournamentMember(troop));
                teamNum += increment;

                if (teamNum == -1)
                {
                    increment = 1;
                    teamNum = 0;
                }
                else if (teamNum == teams.Count)
                {
                    increment = -1;
                    teamNum = teams.Count - 1;
                }
            }

            teams.ForEach(curTeam => AddTournamentTeam(curTeam, curTeam.First().Character?.HeroObject?.ClanBanner, 0, curTeam.First()));
        }

        private List<CharacterObject> GetSimpletons(CultureObject? culture = null, CharacterObject? baseChar = null)
        {
            if (baseChar == null)
                baseChar = CharacterObject.FindFirst(x => x.IsBasicTroop && x.UpgradeTargets != null && (culture == null || x.Culture == culture));

            var simpletons = new List<CharacterObject>() { baseChar };

            if (baseChar.UpgradeTargets != null && baseChar.UpgradeTargets.Length > 0)
            {
                for (var i = 0; i < baseChar.UpgradeTargets.Length; i++)
                    simpletons.AddRange(GetSimpletons(culture, baseChar.UpgradeTargets[i]));
            }

            return simpletons;
        }

        public override void AfterStart()
        {
            CurrentRoundIndex = 0;
            CreateTorunamentTree();
            CalculateBet();
            TournamentRewardManager.InitiateTournament(TournamentGame.Town);
        }

        public override void OnMissionTick(float dt)
        {
            if (CurrentMatch != null && CurrentMatch.State == TournamentMatch.MatchState.Started && MissionBehavior.IsMatchEnded())
                EndCurrentMatch(false);
        }

        public void StartMatch()
        {
            if (CurrentMatch!.IsPlayerParticipating)
            {
                Campaign.Current.TournamentManager.OnPlayerJoinMatch(TournamentGame.GetType());
            }
            CurrentMatch.Start();
            base.Mission.SetMissionMode(MissionMode.Tournament, true);
            MissionBehavior.StartMatch(CurrentMatch, NextRound == null);
            CampaignEventDispatcher.Instance.OnPlayerStartedTournamentMatch(Settlement.Town);
        }

        public void SkipMatch(bool isLeave = false)
        {
            if (CurrentMatch!.IsReady)
                CurrentMatch.Start();

            MissionBehavior.SkipMatch(CurrentMatch);
            EndCurrentMatch(isLeave);
        }

        private void EndCurrentMatch(bool isLeave)
        {
            LastMatch = CurrentMatch;
            CurrentRound.EndMatch();
            MissionBehavior.OnMatchEnded();

            // Update round rewards
            TournamentRewardManager.UpdateRoundWinnings(this);

            // add winners to next round
            if (NextRound != null)
            {
                // fill in round
                LastMatch!.Winners.ToList().ForEach(x => NextRound.AddTeam(x));
                MatchEnd?.Invoke(LastMatch);
            }

            // fire off events if player was disqualified 
            if (LastMatch!.IsPlayerParticipating)
            {
                if (!LastMatch.IsPlayerTeamWinner)
                    OnPlayerTeamEliminated();
                else
                    OnPlayerTeamWinMatch();
            }

            if (CurrentRound.CurrentMatch == null) // done with this round
            {
                // check if done with Tournament or not
                if (CurrentRoundIndex < Rounds.Length - 1)
                {
                    // not done yet, go to next round
                    CurrentRoundIndex++;
                    CalculateBet();
                }
                else
                {
                    // done with Tournament
                    CalculateBet();
                    MessageHelper.QuickInformationMessage(new TextObject("{=tWzLqegB}Tournament is over.", null), 0, null, "");
                    Winner = LastMatch.Winners?.FirstOrDefault()?.GetTeamLeader();
                    if (Winner?.Character.IsHero ?? false)
                    {
                        if (Winner.Character == CharacterObject.PlayerCharacter)
                            OnPlayerWinTournament();

                        Campaign.Current.TournamentManager.GivePrizeToWinner(TournamentGame, Winner.Character.HeroObject, true);
                        Campaign.Current.TournamentManager.AddLeaderboardEntry(Winner.Character.HeroObject);
                    }
                    var list = new List<CharacterObject>(_teams!.SelectMany(x => x.Members).Select(y => y.Character));
#if v100 || v101 || v102 || v103
                    CampaignEventDispatcher.Instance.OnTournamentFinished(Winner?.Character, list.GetReadOnlyList<CharacterObject>(), Settlement.Town, TournamentGame.Prize);
#else   
                    CampaignEventDispatcher.Instance.OnTournamentFinished(Winner?.Character, new(list), Settlement.Town, TournamentGame.Prize);
#endif

                    CurrentInfo.Finish();

                    if (!isLeave)
                        TournamentEnd?.Invoke();
                }
            }
        }

        public void EndTournamentViaLeave()
        {
            while (CurrentMatch != null)
            {
                SkipMatch(true);
            }
        }

        private void OnPlayerTeamEliminated()
        {
            _playerLostAtRound = CurrentRoundIndex + 1;
            IsPlayerEliminated = true;
            BetOdd = 0f;
            if (BettedDenars > 0)
            {
                GiveGoldAction.ApplyForCharacterToSettlement(null, Settlement.CurrentSettlement, BettedDenars, false);
            }
            OverallExpectedDenars = 0;
            CampaignEventDispatcher.Instance.OnPlayerEliminatedFromTournament(CurrentRoundIndex, Settlement.Town);
        }

        private void OnPlayerTeamWinMatch() => Campaign.Current.TournamentManager.OnPlayerWinMatch(TournamentGame.GetType());

        private void OnPlayerWinTournament()
        {
            if (Campaign.Current.GameMode != CampaignGameMode.Campaign)
                return;

            //Renown, influence and gold prizes are awarded by the TournamentRewardManager, so we only account bet winnings here
            if (OverallExpectedDenars > 0)
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, OverallExpectedDenars, false);

            Campaign.Current.TournamentManager.OnPlayerWinTournament(TournamentGame.GetType());
        }

        private void CreateTorunamentTree()
        {
            var T = CurrentInfo.TeamsCount;          // Teams count
            var R = CurrentInfo.Rounds;              // Rounds count 
            var M = CurrentInfo.FirstRoundMatches;   // Matches count
            var TPM = T / M;                         // Teams per match
            var W = TPM / 2;                         // Winners per match

            if (Math.Log(T, 2) > 4) // current interface allows 4 rounds max
                W = 1;

            for (int r = 0; r < R; r++)
            {
                Rounds[r] = new TeamTournamentRound(T, M, W);

                if (r < R)
                {
                    T = W * M;
                    TPM = Math.Max(Math.Min(MBRandom.RandomInt(0, 2) * 2 + 2, T / 2), 2);
                    M = T / TPM;
                    W = TPM / 2;
                }
            }

            // fill in first round
            _teams!.ForEach(x => Rounds[0].AddTeam(x));
        }

        public override InquiryData? OnEndMissionRequest(out bool canPlayerLeave)
        {
            canPlayerLeave = false;
            return null;
        }

        public void PlaceABet(int bet)
        {
            BettedDenars += bet;
            OverallExpectedDenars += GetExpectedDenarsForBet(bet);
            GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, bet, true);
        }

        public int GetExpectedDenarsForBet(int bet) => (int) (BetOdd * bet);

        public int GetMaximumBet()
        {
            int defaultMax = Settings.Instance!.TournamentMaximumBet;
            if (Hero.MainHero.GetPerkValue(DefaultPerks.Roguery.DeepPockets))
            {
                return defaultMax * (int) DefaultPerks.Roguery.DeepPockets.PrimaryBonus;
            }
            return defaultMax;
        }

        //Vanilla bet calculation actually maps nicely to the team tournaments
        private void CalculateBet()
        {
            if (!IsPlayerParticipating)
            {
                return;
            }

            if (CurrentRound.CurrentMatch == null)
            {
                BetOdd = 0.0f;
            }
            else if (IsPlayerEliminated)
            {
                OverallExpectedDenars = 0;
                BetOdd = 0.0f;
            }
            else
            {
                List<KeyValuePair<Hero, int>> leaderboard = Campaign.Current.TournamentManager.GetLeaderboard();
                int playerTournamentWins = 0;
                int maxLeaderbordWins = 0;
                for (int index = 0; index < leaderboard.Count; ++index)
                {
                    if (leaderboard[index].Key == Hero.MainHero)
                        playerTournamentWins = leaderboard[index].Value;
                    if (leaderboard[index].Value > maxLeaderbordWins)
                        maxLeaderbordWins = leaderboard[index].Value;
                }
                float playerRating = 30f + Hero.MainHero.Level + Math.Max(0, playerTournamentWins * 12 - maxLeaderbordWins * 2);
                float totalParticipantsRating = 0.0f;
                float playerTeamRating = 0.0f;
                float otherTeamsRating = 0.0f;
                foreach (TeamTournamentMatch match in CurrentRound.Matches)
                {
                    foreach (TeamTournamentTeam tournamentTeam in match.Teams)
                    {
                        float teamRating = 0.0f;
                        foreach (TeamTournamentMember participant in tournamentTeam.Members)
                        {
                            if (participant.Character != CharacterObject.PlayerCharacter)
                            {
                                int participantTournamentWins = 0;
                                if (participant.Character.IsHero)
                                {
                                    for (int index = 0; index < leaderboard.Count; ++index)
                                    {
                                        if (leaderboard[index].Key == participant.Character.HeroObject)
                                            participantTournamentWins = leaderboard[index].Value;
                                    }
                                }
                                teamRating += participant.Character.Level + Math.Max(0, participantTournamentWins * 8 - maxLeaderbordWins * 2);
                            }
                        }
                        if (tournamentTeam.IsPlayerTeam)
                        {
                            playerTeamRating = teamRating;
                            foreach (TeamTournamentTeam otherTournamentTeam in match.Teams)
                            {
                                if (tournamentTeam != otherTournamentTeam)
                                {
                                    foreach (TeamTournamentMember participant in otherTournamentTeam.Members)
                                    {
                                        int participantTournamentWins = 0;
                                        if (participant.Character.IsHero)
                                        {
                                            for (int index = 0; index < leaderboard.Count; ++index)
                                            {
                                                if (leaderboard[index].Key == participant.Character.HeroObject)
                                                    participantTournamentWins = leaderboard[index].Value;
                                            }
                                        }
                                        otherTeamsRating += participant.Character.Level + Math.Max(0, participantTournamentWins * 8 - maxLeaderbordWins * 2);
                                    }
                                }
                            }
                        }
                        totalParticipantsRating += teamRating;
                    }
                }
                float randomFactor = Settings.Instance!.EnableRandomizedBettingOdds ? MBRandom.RandomFloatRanged(0.75f, 1.25f) : 1f;
                BetOdd = (int) (MathF.Clamp(MathF.Pow(1f / ((playerTeamRating + playerRating) / totalParticipantsRating * (playerRating / (playerTeamRating + playerRating + 0.5f * (totalParticipantsRating - (playerTeamRating + otherTeamsRating))))), 0.75f) * randomFactor, 1.1f, MaximumOdd) * 10.0) / 10f;
            }
        }

        internal List<CharacterObject> GetAllPossibleParticipants() => _teams!.SelectMany(t => t.Members.Select(m => m.Character)).ToList();

        public event Action? TournamentEnd;
        public event Action<TeamTournamentMatch>? MatchEnd;
        public const float EndMatchTimerDuration = 6f;
        private List<TeamTournamentTeam>? _teams;
        private int _playerLostAtRound;
        public const float MaximumOdd = 4f;
    }
}