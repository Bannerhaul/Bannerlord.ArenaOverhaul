using System.Collections.Generic;
using System.Linq;

using TaleWorlds.Core;

namespace ArenaOverhaul.TeamTournament
{
    public class TeamTournamentTeam
    {
        private List<TeamTournamentMember> _members;
        private int _score;
        private TeamTournamentMember? _leader;

        public IEnumerable<TeamTournamentMember> Members { get => _members; }
        public int Score { get => _score; }
        public Banner? TeamBanner { get; set; }
        public uint TeamColor { get; set; }
        public bool IsPlayerTeam => Members.Any(x => x.IsPlayer);
        public bool IsAlive { get; internal set; }

        public TeamTournamentTeam(IEnumerable<TeamTournamentMember> members, Banner? teamBanner = null, uint teamColor = 0, TeamTournamentMember? leader = null)
        {
            _members = new List<TeamTournamentMember>(members);
            TeamBanner = teamBanner;
            TeamColor = teamColor;

            foreach (var el in _members)
                el.SetTeam(this);

            _leader = leader;
        }

        public void AddScore(int score)
        {
            _score += score;
            Members.ToList().ForEach(x => x.AddScore(score));
        }

        public void ResetScore()
        {
            _score = 0;
            Members.ToList().ForEach(x => x.ResetScore());
        }

        public TeamTournamentMember GetTeamLeader()
        {
            if (_leader != null)
                return _leader;

            if (IsPlayerTeam)
                return Members.First(x => x.IsPlayer);
            else
            {
#if e165
                return
                  Members.Where(x => x.Character.IsHero).OrderByDescending(x => x.Character.GetPower()).FirstOrDefault()
                  ?? Members.OrderByDescending(x => x.Character.GetPower()).First();
#else
                return
                  Members.Where(x => x.Character.IsHero).OrderByDescending(x => x.Character.GetBattlePower()).FirstOrDefault()
                  ?? Members.OrderByDescending(x => x.Character.GetBattlePower()).First();
#endif
            }
        }
    }
}