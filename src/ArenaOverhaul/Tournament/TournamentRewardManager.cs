using ArenaOverhaul.CampaignBehaviors.BehaviorManagers;
using ArenaOverhaul.Helpers;
using ArenaOverhaul.ModSettings;

using ArenaOverhaul.TeamTournament;

using SandBox.Tournaments.MissionLogics;

using System;
using System.Collections.Generic;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace ArenaOverhaul.Tournament
{
    public static class TournamentRewardManager
    {
        private static readonly Dictionary<Town, List<(int Round, CharacterObject Winner)>> _roundWinners = new();
        private static readonly Dictionary<Town, List<(Hero Participant, int Winnings)>> _roundPrizeWinners = new();

        private static readonly Dictionary<Town, List<(Hero AffectorHero, Hero AffectedHero)>> _noticableTakedowns = new();
        private static readonly Dictionary<Town, List<(Hero Awardee, int RenownAward)>> _renownAwardees = new();
        private static readonly Dictionary<Town, PrizeItemInfo?> _tournamentPrizeAwards = new();

        public static Dictionary<Town, List<(Hero Participant, int Winnings)>> RoundPrizeWinners => _roundPrizeWinners;
        public static Dictionary<Town, List<(Hero Participant, int Winnings)>> RenownAwardees => _renownAwardees;
        public static Dictionary<Town, PrizeItemInfo?> PlannedTournamentPrizes => AOArenaBehaviorManager.Instance!.TournamentPrizes;
        public static Dictionary<Town, PrizeItemInfo?> TournamentPrizeAwards => _tournamentPrizeAwards;

        internal static void Initialize()
        {
            _roundWinners.Clear();
            _roundPrizeWinners.Clear();
            _noticableTakedowns.Clear();
            _renownAwardees.Clear();
            _tournamentPrizeAwards.Clear();
        }

        public static void InitiateTournament(Town town)
        {
            _roundWinners.Remove(town);
            _roundPrizeWinners.Remove(town);
            _noticableTakedowns.Remove(town);
            _renownAwardees.Remove(town);
            _tournamentPrizeAwards.Remove(town);

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

            if (affectorHero.CurrentSettlement?.Town != null && ((affectedHeroRenown > 0 && affectedHeroRenown >= affectorHeroRenown * 0.85) || (affectedHeroPosition > 1 && affectedHeroPosition > affectorHeroPosition)))
            {
                if (_noticableTakedowns.TryGetValue(affectorHero.CurrentSettlement.Town, out var listOfTakedowns) && listOfTakedowns != null)
                {
                    listOfTakedowns.Add((affectorHero, affectedHero));
                }
                else
                {
                    _noticableTakedowns[affectorHero.CurrentSettlement.Town] = new List<(Hero AffectorHero, Hero AffectedHero)> { (affectorHero, affectedHero) };
                }
            }
        }

        public static int GetTournamentGoldPrize(Town tournamentTown)
        {
            return (int) (Math.Floor((Settings.Instance!.EnableTournamentGoldPrizes ? MathHelper.GetSoftCappedValue(tournamentTown.Settlement.Town.Prosperity) + (Settings.Instance!.EnableTournamentPrizeScaling ? MathHelper.GetSoftCappedValue(Clan.PlayerClan.Renown) : 0.0) : 0.0) / 50.0) * 50.0);
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
            if (winnerHero.IsLord && winnerHero.Clan?.Kingdom != null)
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

            if (PlannedTournamentPrizes.TryGetValue(town, out var prizeItemInfo))
            {
                _tournamentPrizeAwards[town] = prizeItemInfo;
                PlannedTournamentPrizes[town] = null;
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

        #region Prize Item Quality
        public static void RegisterPrizeModifier(Town town, ItemObject? itemObject, ItemModifier? itemModifier)
        {
            if (CurrentPrizeHasRegisteredModifier(town, itemObject))
            {
                return;
            }
            PlannedTournamentPrizes[town] = itemObject != null ? new(itemObject, itemModifier) : null;
        }

        internal static bool CurrentPrizeHasRegisteredModifier(TournamentGame tournamentGame) => CurrentPrizeHasRegisteredModifier(tournamentGame.Town, tournamentGame.Prize);

        internal static bool CurrentPrizeHasRegisteredModifier(Town? town, ItemObject? itemObject)
        {
            return town != null && PlannedTournamentPrizes.TryGetValue(town, out var prizeItemInfo) && prizeItemInfo?.ItemObject?.StringId == itemObject?.StringId;
        }

        public static string GetPrizeItemName(TournamentGame tournamentGame)
        {
            return TryGetPrizeItemModifier(tournamentGame, out var prizeItemInfo)
                ? new EquipmentElement(prizeItemInfo!.ItemObject, prizeItemInfo.ItemModifier).GetModifiedItemName().ToString()
                : tournamentGame.Prize.Name.ToString();
        }

        public static void ShowPrizeItemHint(TournamentGame tournamentGame)
        {
            EquipmentElement equipmentElement = TryGetPrizeItemModifier(tournamentGame, out var prizeItemInfo)
                ? new EquipmentElement(prizeItemInfo!.ItemObject, prizeItemInfo.ItemModifier)
                : new EquipmentElement(tournamentGame.Prize);
            InformationManager.ShowTooltip(typeof(ItemObject), equipmentElement);
        }

        public static bool TryGetPrizeItemModifier(TournamentGame tournamentGame, out PrizeItemInfo? prizeItemInfo)
        {
            return PlannedTournamentPrizes.TryGetValue(tournamentGame.Town, out prizeItemInfo) && prizeItemInfo != null && prizeItemInfo.ItemObject.StringId == tournamentGame.Prize.StringId && prizeItemInfo.ItemModifier != null;
        }

        public static ItemModifier? GetRandomItemModifier(ItemObject? __result)
        {
            if (!Settings.Instance!.EnableHighQualityPrizes)
            {
                return null;
            }

            ItemModifier? itemModifier = null;
            ItemModifierGroup? itemModifierGroup = __result?.ItemComponent.ItemModifierGroup;
            if (itemModifierGroup != null)
            {
                var desiredItemQuality = GetDesiredItemQuality();
                for (ItemQuality itemQuality = desiredItemQuality.MaxQuality; itemQuality >= desiredItemQuality.MinQuality; --itemQuality)
                {
                    itemModifier = itemModifierGroup?.GetModifiersBasedOnQuality(itemQuality).FirstOrDefault();
                    if (itemModifier != null)
                    {
                        break;
                    }
                }
            }
            if (itemModifier?.ItemQuality <= ItemQuality.Common)
            {
                itemModifier = null;
            }

            return itemModifier;
        }

        private static (ItemQuality MaxQuality, ItemQuality MinQuality) GetDesiredItemQuality()
        {
            (int MaxQuality, int MinQuality) qualityRestrictions;
            if (Settings.Instance!.EnableTournamentPrizeScaling)
            {
                qualityRestrictions = Clan.PlayerClan.Tier switch
                {
                    > 5 => ((int) ItemQuality.Legendary + 2, (int) ItemQuality.Common),
                    5 => ((int) ItemQuality.Legendary + 1, (int) ItemQuality.Common),
                    4 => ((int) ItemQuality.Legendary, (int) ItemQuality.Common),
                    3 => ((int) ItemQuality.Masterwork, (int) ItemQuality.Common),
                    >= 1 => ((int) ItemQuality.Fine, (int) ItemQuality.Inferior),
                    _ => ((int) ItemQuality.Fine, (int) ItemQuality.Poor)
                };
            }
            else
            {
                qualityRestrictions = ((int) ItemQuality.Legendary, (int) ItemQuality.Poor);
            }
            //The tournament prize should never be worse than of Common quality, lower values ​​are only used to calculate the drop chance for higher qualities.
            var maxQuality = (ItemQuality) MBMath.ClampInt(MBRandom.RandomInt(qualityRestrictions.MinQuality, qualityRestrictions.MaxQuality + 1), (int) ItemQuality.Common, (int) ItemQuality.Legendary);
            return (maxQuality, ItemQuality.Fine); //Since we default to Common quality, there's no need to check for anything lower than ItemQuality.Fine.
        }
        #endregion Prize Item Quality
    }
}