using System.Collections.Generic;
using System.Linq;

using TaleWorlds.Core;

namespace ArenaOverhaul.TeamTournament
{
    public class TeamTournamentRound
    {
        public IEnumerable<TeamTournamentMatch> Matches { get => _matches; }
        public int MatchCount { get => _matches.Count; }
        public int CurrentMatchIndex { get; private set; }
        public TeamTournamentMatch? CurrentMatch => (CurrentMatchIndex >= _matches.Count) ? null : _matches[CurrentMatchIndex];
        public IEnumerable<TeamTournamentTeam>? Teams => Matches?.SelectMany(x => x.Teams);

        public TeamTournamentRound(int teamsInRound, int numberOfMatches, int numerOfWinnerTeams)
        {
            CurrentMatchIndex = 0;
            _matches = new List<TeamTournamentMatch>(numberOfMatches);
            for (var i = 0; i < numberOfMatches; i++)
                _matches.Add(new TeamTournamentMatch(teamsInRound / numberOfMatches, numerOfWinnerTeams));
        }

        public int AddTeam(TeamTournamentTeam team)
        {
            int matchNum;
            int tryNum = 0;
            do
            {
                if (tryNum++ == 64)
                {
                    matchNum = _matches.FindIndex(x => !x.IsFullMatch);
                    break;
                }
                matchNum = MBRandom.Random.Next(_matches.Count);
            } while (_matches[matchNum].IsFullMatch);

            _matches[matchNum].AddTeam(team);
            return matchNum;
        }

        public void EndMatch()
        {
            CurrentMatch?.End();
            CurrentMatchIndex++;
        }

        private List<TeamTournamentMatch> _matches;
    }
}

