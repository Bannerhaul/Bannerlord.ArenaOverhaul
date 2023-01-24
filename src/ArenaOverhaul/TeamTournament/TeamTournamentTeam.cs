using System.Collections.Generic;
using System.Linq;

using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace ArenaOverhaul.TeamTournament
{
    public class TeamTournamentTeam
    {
        private List<TeamTournamentMember> _members;
        private int _score;
        private TeamTournamentMember? _leader;
        private int _TeamIndex;

        public IEnumerable<TeamTournamentMember> Members { get => _members; }
        public int Score { get => _score; }
        public Banner? TeamBanner { get; set; }
        public uint TeamColor { get; set; }
        public bool IsPlayerTeam => Members.Any(x => x.IsPlayer);
        public bool IsAlive { get; internal set; }
        public string Name => GetName();
        internal int TeamIndex { get => _TeamIndex; }

        public TeamTournamentTeam(IEnumerable<TeamTournamentMember> members, int teamIndex, Banner? teamBanner = null, uint teamColor = 0, TeamTournamentMember? leader = null)
        {
            _members = new List<TeamTournamentMember>(members);
            TeamBanner = teamBanner;
            TeamColor = teamColor;
            _TeamIndex = teamIndex;

            foreach (var member in _members)
            {
                member.SetTeam(this);
            }

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
            {
                return _leader;
            }

            if (IsPlayerTeam)
            {
                return Members.First(x => x.IsPlayer);
            }
            else
            {
                return
                  Members.Where(x => x.Character.IsHero).OrderByDescending(x => x.Character.GetBattlePower()).FirstOrDefault()
                  ?? Members.OrderByDescending(x => x.Character.GetBattlePower()).First();
            }
        }

        private string GetName()
        {
            TeamTournamentMember leader = GetTeamLeader();

            TextObject teamName = new TextObject("{=gVZq43GDI}{LEADER_NAME}'s team {TEAM_CALL_SIGN}", null);
            teamName.SetTextVariable("LEADER_NAME", GetTeamLeader().Character.Name);
            if (!leader.Character.IsHero)
            {
                if (_TeamIndex <= 7)
                {
                    teamName.SetTextVariable("TEAM_CALL_SIGN", GameTexts.FindText("str_team_tournament_call_sign", _TeamIndex.ToString()));
                }
                else
                {
                    teamName.SetTextVariable("TEAM_CALL_SIGN", (_TeamIndex + 1).ToString());
                }
            }

            return teamName.ToString().TrimEnd();
        }
    }
}