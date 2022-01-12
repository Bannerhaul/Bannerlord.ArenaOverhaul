using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace ArenaOverhaul.TeamTournament
{
    public class TeamTournamentMember
    {
        public int Score { get; private set; }
        public CharacterObject Character { get; private set; }
        public UniqueTroopDescriptor Descriptor { get; private set; }
        public TeamTournamentTeam? Team { get => _team; private set => _team = value; }
        public Equipment? MatchEquipment { get; set; }
        public bool IsPlayer => Character != null && Character.IsPlayerCharacter;

        public TeamTournamentMember(CharacterObject character)
        {
            Character = character;
            Descriptor = new UniqueTroopDescriptor(Game.Current.NextUniqueTroopSeed);
        }

        public int AddScore(int score)
        {
            Score += score;
            return Score;
        }

        public void SetTeam(TeamTournamentTeam team) => Team = team;

        private TeamTournamentTeam? _team;

        public void ResetScore() => Score = 0;

        public bool IsCharWithDescriptor(int uniqueTroopSeed) => Descriptor.CompareTo(uniqueTroopSeed) == 0;
    }
}