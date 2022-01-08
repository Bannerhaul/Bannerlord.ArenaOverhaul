using SandBox.View.Menu;

using System;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.SandBox.Source.TournamentGames;
using TaleWorlds.Core;

namespace ArenaOverhaul.TeamTournament
{
    public class TeamTournamentInfo
    {
        public static TeamTournamentInfo? Current { get; private set; }
        public TroopRoster? SelectedRoster { get; private set; }
        public TournamentTeam? PlayerTeam { get; set; }
        public int TeamsCount { get; }
        public int FirstRoundMatches { get; }
        public int Rounds { get; }
        public int TeamSize { get; }
        public bool IsFinished { get; set; }
        public Town Town { get; }
        public bool IsStarted => SelectedRoster != null;

        public TeamTournamentInfo()
        {
            // TODO: if a need arises to show tournament information beforehand, extract creation into upper chain
            //       e.g. when tournaments are generated in the world, also replace "Current" instance
            Town = Settlement.CurrentSettlement.Town;
            TeamSize = MBRandom.Random.Next(2, Settings.Instance!.TeamSizeMax);
            var exponentTeamsMax = (int) Math.Floor(Math.Log(Settings.Instance!.TeamsCountMax.SelectedValue) / 0.30102999f); // log(n) / log(2)
            TeamsCount = (int) Math.Pow(2, MBRandom.Random.Next(3, exponentTeamsMax)); // 8, 16, 32 teams possible -> also 4 but needs more testing and fixing 
            FirstRoundMatches = (TeamsCount == 32 ? 8 : TeamsCount) / (MBRandom.Random.Next(2) * 2 + 2); // if full (32) => 8 rounds, else can be 4 or 8
            Rounds = (int) Math.Min(Math.Log(TeamsCount, 2), 4); // simple log2 round progression (members/2 in every round)
            Current = this;
        }

        public void OpenSelectionMenu(MenuViewContext menuViewContext)
        {
            var mainHeroRoster = TroopRoster.CreateDummyTroopRoster();
            mainHeroRoster.AddToCounts(Hero.MainHero.CharacterObject, 1);

            menuViewContext.MenuContext.OpenTroopSelection(
                GetAvailableSelection(),
                mainHeroRoster,
                delegate (CharacterObject character) { return !character.IsPlayerCharacter; },
                delegate (TroopRoster selection)
                {
                    SelectedRoster = selection;
                    var tournamentGame = Campaign.Current.TournamentManager.GetTournamentGame(Settlement.CurrentSettlement.Town);
                    GameMenu.SwitchToMenu("town");
                    tournamentGame.PrepareForTournamentGame(true);
                    Campaign.Current.TournamentManager.OnPlayerJoinTournament(tournamentGame.GetType(), Settlement.CurrentSettlement);
                },
                TeamSize
            );
        }

        public bool IsTeamTournament => SelectedRoster != null;

        public static TroopRoster GetAvailableSelection()
        {
            var availableRoster = TroopRoster.CreateDummyTroopRoster();

            // we add everyone is settlement that is relevant to the selection list
            var selectableChars = Settlement.CurrentSettlement
              .GetCombatantHeroesInSettlement()
              .Where(x => CanBeSelected(x));

            var flattenTroopRoster = new FlattenedTroopRoster(0);

            // add the main hero at the top
            flattenTroopRoster.Add(Hero.MainHero.CharacterObject, 1, 0);

            // add every other hero afterwards
            foreach (var character in selectableChars)
                flattenTroopRoster.Add(character, 1, 0);

            availableRoster.Add(flattenTroopRoster);

            // now also add own troops in party roster
            availableRoster.Add(
                MobileParty.MainParty.MemberRoster.ToFlattenedRoster()
                    .Where(x => !x.Troop.IsHero || !availableRoster.Contains(x.Troop))
            );

            return availableRoster;
        }

        public static bool CanBeSelected(CharacterObject character)
        {
            var isSameSettlement = character.HeroObject.CurrentSettlement == Hero.MainHero.CurrentSettlement;
            if (!isSameSettlement) return false;

            var isSameClan = Hero.MainHero.Clan == character.HeroObject.Clan;
            if (isSameClan) return true;

            var isSameAlliance = Hero.MainHero.MapFaction == character.HeroObject.MapFaction && (Hero.MainHero.IsFriend(character.HeroObject) || Hero.MainHero.IsFactionLeader);
            if (isSameAlliance) return true;

            return Hero.MainHero.AllLivingRelatedHeroes().Contains(character.HeroObject);
        }

        internal void Finish()
        {
            SelectedRoster = null;
            Current = null;
            IsFinished = true;
        }
    }
}