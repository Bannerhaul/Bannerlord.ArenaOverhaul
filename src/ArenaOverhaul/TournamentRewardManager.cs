using ArenaOverhaul.TeamTournament;

using SandBox.Tournaments.MissionLogics;

using System;
using System.Collections.Generic;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace ArenaOverhaul
{
    public static class TournamentRewardManager
    {
        private static readonly Dictionary<Town, List<(int Round, CharacterObject Winner)>> _roundWinners = new();
        private static readonly Dictionary<Town, List<(Hero Participant, int Winnings)>> _roundPrizeWinners = new();

        private static readonly Dictionary<Town, List<(Hero AffectorHero, Hero AffectedHero)>> _noticableTakedowns = new();
        private static readonly Dictionary<Town, List<(Hero Awardee, int RenownAward)>> _renownAwardees = new();

        public static Dictionary<Town, List<(Hero Participant, int Winnings)>> RoundPrizeWinners => _roundPrizeWinners;
        public static Dictionary<Town, List<(Hero Participant, int Winnings)>> RenownAwardees => _renownAwardees;

        internal static void Initialize()
        {
            _roundWinners.Clear();
            _roundPrizeWinners.Clear();
            _noticableTakedowns.Clear();
            _renownAwardees.Clear();
        }

        public static void InitiateTournament(Town town)
        {
            _roundWinners.Remove(town);
            _roundPrizeWinners.Remove(town);
            _noticableTakedowns.Remove(town);
            _renownAwardees.Remove(town);

            _roundWinners.Add(town, new());
            _roundPrizeWinners.Add(town, new());
            _noticableTakedowns.Add(town, new());
            _renownAwardees.Add(town, new());
        }

        internal static void UpdateRoundWinnings(TournamentBehavior instance)
        {
            _roundWinners[instance.TournamentGame.Town].AddRange(instance.LastMatch.Winners.Where(x => x.Character.IsHero).Select(x => (Round: instance.CurrentRoundIndex, Winner: x.Character)).ToList());
        }

        internal static void UpdateRoundWinnings(TeamTournamentBehavior instance)
        {
            _roundWinners[instance.TournamentGame.Town].AddRange(instance.LastMatch!.Winners.Where(x => x.GetTeamLeader().Character.IsHero).Select(x => (Round: instance.CurrentRoundIndex, Winner: x.GetTeamLeader().Character)).ToList());
        }

        internal static void UpdateNoticableTakedowns(CharacterObject affectorCharacter, CharacterObject affectedCharacter)
        {
            if (affectorCharacter.IsHero && affectedCharacter.IsHero)
            {
                Hero affectorHero = affectorCharacter.HeroObject;
                Hero affectedHero = affectedCharacter.HeroObject;
                UpdateNoticableTakedowns(affectorHero, affectedHero);
            }
        }

        internal static void UpdateNoticableTakedowns(Agent affectorAgent, Agent affectedAgent)
        {
            if (affectedAgent.Health < 1.0)
            {
                UpdateNoticableTakedowns((CharacterObject) affectorAgent.Character, (CharacterObject) affectedAgent.Character);
            }
        }

        internal static void UpdateNoticableTakedowns(TournamentParticipant simulatedPuncher, TournamentParticipant simulatedVictim)
        {
            UpdateNoticableTakedowns(simulatedPuncher.Character, simulatedVictim.Character);
        }

        internal static void UpdateNoticableTakedowns(TeamTournamentMember simulatedPuncher, TeamTournamentMember simulatedVictim)
        {
            UpdateNoticableTakedowns(simulatedPuncher.Character, simulatedVictim.Character);
        }

        private static void UpdateNoticableTakedowns(Hero affectorHero, Hero affectedHero)
        {
            List<KeyValuePair<Hero, int>> leaderboard = Campaign.Current.TournamentManager.GetLeaderboard();
            KeyValuePair<Hero, int>? affectorHeroPositionPair = leaderboard.FirstOrDefault(x => x.Key == affectorHero);
            KeyValuePair<Hero, int>? affectedHeroPositionPair = leaderboard.FirstOrDefault(x => x.Key == affectedHero);
            int affectorHeroPosition = affectorHeroPositionPair?.Value ?? 0;
            int affectedHeroPosition = affectedHeroPositionPair?.Value ?? 0;

            float affectedHeroRenown = affectedHero.Clan?.Renown ?? 0f;
            float affectorHeroRenown = affectorHero.Clan?.Renown ?? 0f;

            if ((affectorHero.CurrentSettlement?.Town != null) && ((affectedHeroRenown > 0 && affectedHeroRenown >= (affectorHeroRenown * 0.85)) || (affectedHeroPosition > 1 && affectedHeroPosition > affectorHeroPosition)))
            {
                _noticableTakedowns[affectorHero.CurrentSettlement.Town].Add((affectorHero, affectedHero));
            }
        }

        public static int GetTournamentGoldPrize(Town tournamentTown)
        {
            return (int) (Math.Floor((Settings.Instance!.EnableTournamentGoldPrizes ? tournamentTown.Settlement.Prosperity + (Settings.Instance!.EnableTournamentPrizeScaling ? Clan.PlayerClan.Renown : 0.0) : 0.0) / 50.0) * 50.0);
        }

        public static void ResolveTournament(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town)
        {
            CaluclateWinningsAndCloseTournament(town);
            if (winner.IsHero)
            {
                Hero winnerHero = winner.HeroObject;
                int goldPrize = GetTournamentGoldPrize(town) + RoundPrizeWinners[town].FirstOrDefault(x => x.Participant == winnerHero).Winnings;
                if (goldPrize > 0)
                {
                    GiveGoldAction.ApplyBetweenCharacters(null, winnerHero, goldPrize);
                }
                GiveRenownReward(winnerHero, town);
                GiveInfluenceReward(winnerHero);
            }
            GiveConsolationPrizes(winner, participants, town);
        }

        public static int GetTakedownRenownReward(Hero hero, Town town) =>
            Math.Min(_renownAwardees[town].FirstOrDefault(x => x.Awardee == hero).RenownAward, Campaign.Current.Models.TournamentModel.GetRenownReward(hero, town) * 2);

        private static void GiveRenownReward(Hero winnerHero, Town town)
        {
            if (winnerHero.Clan != null)
            {
                int basicRenown = Campaign.Current.Models.TournamentModel.GetRenownReward(winnerHero, town);
                int takedownRenown = GetTakedownRenownReward(winnerHero, town);
                GainRenownAction.Apply(winnerHero, basicRenown + takedownRenown);
            }
        }

        private static void GiveInfluenceReward(Hero winnerHero)
        {
            if (winnerHero.IsNoble && winnerHero.Clan?.Kingdom != null)
            {
                GainKingdomInfluenceAction.ApplyForDefault(winnerHero, Settings.Instance!.TournamentInfluenceReward);
            }
        }

        private static void GiveConsolationPrizes(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town)
        {
            foreach (Hero participantHero in participants.Where(participant => participant.IsHero && participant != winner).Select(characterObject => characterObject.HeroObject).ToList())
            {
                int goldPrize = RoundPrizeWinners[town].FirstOrDefault(x => x.Participant == participantHero).Winnings;
                if (goldPrize > 0)
                {
                    GiveGoldAction.ApplyBetweenCharacters(null, participantHero, goldPrize);
                }
                int takedownRenown = GetTakedownRenownReward(participantHero, town);
                if (takedownRenown > 0 && participantHero.Clan != null)
                {
                    GainRenownAction.Apply(participantHero, takedownRenown);
                }
            }
        }

        private static void CaluclateWinningsAndCloseTournament(Town town)
        {
            if (_roundWinners.TryGetValue(town, out var listOfWinners))
            {
                _roundWinners.Remove(town);
                _roundPrizeWinners[town] = listOfWinners.GroupBy(x => x.Winner).Select(grouping => (Winner: grouping.Key, Count: grouping.Count())).Select(x => (Participant: x.Winner.HeroObject, Winnings: x.Count * GetGetTournamentGoldPrizePerRoundWon())).ToList();
            }
            else
            {
                _roundPrizeWinners[town] = new();
            }

            if (_noticableTakedowns.TryGetValue(town, out var listOfNoticableTakedowns))
            {
                _noticableTakedowns.Remove(town);
                _renownAwardees[town] = listOfNoticableTakedowns.GroupBy(x => x.AffectorHero).Select(grouping => (Winner: grouping.Key, Count: grouping.Count())).Select(x => (Participant: x.Winner, Winnings: x.Count * GetTournamentRenownPerTakedown())).ToList();
            }
            else
            {
                _renownAwardees[town] = new();
            }
        }

        private static int GetGetTournamentGoldPrizePerRoundWon()
        {
            return Settings.Instance!.TournamentRoundWonReward;
        }

        private static int GetTournamentRenownPerTakedown()
        {
            return Settings.Instance!.TournamentTakedownRenownReward;
        }
    }
}