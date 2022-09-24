using ArenaOverhaul.Helpers;

using SandBox.Tournaments;

using System;
using System.Collections.Generic;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

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

            List<TeamTournamentTeam> similarTeams = _teams.Where(x => x.GetTeamLeader().Character.Name == localLeader.Character.Name).ToList();
            int teamIndex = similarTeams.Any() ? similarTeams.Max(x => x.TeamIndex) + 1 : 0;

            _teams!.Add(new TeamTournamentTeam(members, teamIndex, teamBanner, teamColor, leader));
        }

        private void CreateTournamentTeams()
        {
            // check out if we can form teams locally from other heroes
            var heroesInSettlement = Settlement
              .GetCombatantHeroesInSettlement()
              .Where(x => !CurrentInfo.SelectedRoster!.Contains(x));

            // first try to get teams of every local party, "they arrived just for this event"
            foreach (var partyLeader in heroesInSettlement.Where(x => x.HeroObject.IsPartyLeader && !x.HeroObject.IsNotable))
            {
                var totalCount = partyLeader.HeroObject.PartyBelongedTo.MemberRoster.TotalHealthyCount;

                // if this party can't at least have a team full team, drop them
                if (totalCount < CurrentInfo.TeamSize)
                    continue;

                //var totalHeroes = partyLeader.HeroObject.PartyBelongedTo.MemberRoster.TotalHeroes;
                var topHeroes = partyLeader.HeroObject.PartyBelongedTo.MemberRoster
                    .GetTroopRoster()
                    .Where(x => x.Character.IsHero && x.Character.CanBeAParticipant(true))
                    .OrderByDescending(y => y.Character.GetBattlePower())
                    .Select(z => z.Character)
                    .Take(CurrentInfo.TeamSize)
                    .ToList();

                if (topHeroes.Count == CurrentInfo.TeamSize)
                {
                    AddTournamentTeam(topHeroes.Select(x => new TeamTournamentMember(x)));
                    continue;
                }

                // just heroes wasn't enough, fill up with soldiers from party
                var strongestPartyTeam = partyLeader.HeroObject.PartyBelongedTo.MemberRoster
                    .GetTroopRoster()
                    .Where(x => !x.Character.IsHero && x.WoundedNumber < x.Number && x.Character.CanBeAParticipant(true))
                    .OrderByDescending(z => z.Character.GetBattlePower())
                    .Take(CurrentInfo.TeamSize - topHeroes.Count);

                var flattenRoster = new FlattenedTroopRoster { strongestPartyTeam.ToList() };

                foreach (var flattenedTroopRosterElement in flattenRoster.OrderByDescending(x => x.Troop.GetBattlePower()))
                {
                    topHeroes.Add(flattenedTroopRosterElement.Troop);
                    if (topHeroes.Count == CurrentInfo.TeamSize)
                        break;
                }

                if (topHeroes.Count() == CurrentInfo.TeamSize)
                    AddTournamentTeam(topHeroes.OrderByDescending(x => x.GetBattlePower()).Select(y => new TeamTournamentMember(y)));

                if (_teams!.Count == CurrentInfo.TeamsCount)
                    return;
            }

            //Get soldiers in settlement, we'll use them in the rest of the team building
            var garrisonTroopRoster = Settlement.Town.GarrisonParty?.MemberRoster?.GetTroopRoster();
            List<CharacterObject> troopsAvailable;
            if (garrisonTroopRoster != null && garrisonTroopRoster.Any())
            {
                troopsAvailable = garrisonTroopRoster.Where(x => x.Number > x.WoundedNumber && !x.Character.IsHero && x.Character.CanBeAParticipant(true)).Select(x => x.Character).ToList();
            }
            else
            {
                troopsAvailable = GetSimpletons(Settlement.Culture);
                if (!troopsAvailable.Any())
                {
                    troopsAvailable = GetSimpletons().Where(x => x.CanBeAParticipant(true)).ToList();
                }
            }

            // if we are still not done, create teams with local heroes
            var possibleHeroes = heroesInSettlement.Where(x => !x.HeroObject.IsPartyLeader && !IsAlreadySelected(x)).ToList();
            if (possibleHeroes.Count <= CurrentInfo.TeamsCount - _teams!.Count)
            {
                foreach (var localHero in possibleHeroes)
                {
                    List<TeamTournamentMember> currentTeam = new()
                    {
                        new TeamTournamentMember(localHero)
                    };
                    List<CharacterObject> randomList = new(troopsAvailable);
                    randomList.Shuffle();
                    var simpletonList = randomList.Take(CurrentInfo.TeamSize - currentTeam.Count()).Select(x => new TeamTournamentMember(x)).ToList();
                    currentTeam.AddRange(simpletonList);
                    AddTournamentTeam(currentTeam);
                }
            }
            else
            {
                while (possibleHeroes.Any() && _teams!.Count < CurrentInfo.TeamsCount)
                {
                    List<TeamTournamentMember> currentTeam = new();
                    foreach (var localHero in possibleHeroes)
                    {
                        if (!currentTeam.Any(x => x.Character.IsHero && localHero.HeroObject.IsEnemy(x.Character.HeroObject)) || currentTeam.Any(x => x.Character.IsHero && localHero.HeroObject.IsFriend(x.Character.HeroObject)))
                            currentTeam.Add(new TeamTournamentMember(localHero));

                        if (currentTeam.Count() >= CurrentInfo.TeamSize)
                        {
                            AddTournamentTeam(currentTeam);
                            if (_teams!.Count == CurrentInfo.TeamsCount)
                                return;
                            currentTeam = new();
                        }
                    }
                    if (currentTeam.Count() > 0 && currentTeam.Count() < CurrentInfo.TeamSize) // fill up last hero team with troops
                    {
                        List<CharacterObject> randomList = new(troopsAvailable);
                        randomList.Shuffle();
                        var simpletonList = randomList.Take(CurrentInfo.TeamSize - currentTeam.Count()).Select(x => new TeamTournamentMember(x)).ToList();
                        currentTeam.AddRange(simpletonList);
                        AddTournamentTeam(currentTeam);
                        if (_teams!.Count == CurrentInfo.TeamsCount)
                            return;
                        currentTeam = new();
                    }
                    possibleHeroes = heroesInSettlement.Where(x => !x.HeroObject.IsPartyLeader && !IsAlreadySelected(x)).ToList();
                }
            }

            // still not done, just add troops to fill it
            if (troopsAvailable.Count > 0)
            {
                do
                {
                    var teamToAdd = new List<CharacterObject>();
                    for (var i = 0; i < CurrentInfo.TeamSize; i++)
                    {
                        teamToAdd.Add(troopsAvailable.GetRandomElement());
                    }
                    AddTournamentTeam(teamToAdd.Select(x => new TeamTournamentMember(x)).ToList());
                }
                while (_teams!.Count < CurrentInfo.TeamsCount);

                if (_teams!.Count == CurrentInfo.TeamsCount)
                    return;
            }
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
                    var list = new List<CharacterObject>(_teams.SelectMany(x => x.Members).Select(y => y.Character));
                    CampaignEventDispatcher.Instance.OnTournamentFinished(Winner?.Character, list.GetReadOnlyList<CharacterObject>(), Settlement.Town, TournamentGame.Prize);

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

        public event Action? TournamentEnd;
        public event Action<TeamTournamentMatch>? MatchEnd;
        public const float EndMatchTimerDuration = 6f;
        private List<TeamTournamentTeam>? _teams;
        private int _playerLostAtRound;
        public const float MaximumOdd = 4f;
    }
}