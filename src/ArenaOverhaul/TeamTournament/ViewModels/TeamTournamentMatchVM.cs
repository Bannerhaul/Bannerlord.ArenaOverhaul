using System;
using System.Collections.Generic;
using System.Linq;

using TaleWorlds.Library;

namespace ArenaOverhaul.TeamTournament.ViewModels
{
    public class TeamTournamentMatchVM : ViewModel
    {
        public TeamTournamentMatch? Match { get; private set; }
        public IEnumerable<TeamTournamentTeamVM> Teams { get => _teams; }

        public TeamTournamentMatchVM()
        {
            Team1 = new TeamTournamentTeamVM();
            Team2 = new TeamTournamentTeamVM();
            Team3 = new TeamTournamentTeamVM();
            Team4 = new TeamTournamentTeamVM();
            _teams = new List<TeamTournamentTeamVM>
            {
                Team1,
                Team2,
                Team3,
                Team4
            };
        }

        public IEnumerable<TeamTournamentMemberVM> GetMatchMemberVMs() => Teams.SelectMany(x => x.Members);

        public override void RefreshValues()
        {
            base.RefreshValues();
            _teams.ForEach(x => x.RefreshValues());
        }

        public void Initialize()
        {
            foreach (var tournamentTeamVM in Teams)
            {
                if (tournamentTeamVM.IsValid)
                    tournamentTeamVM.Initialize();
            }
        }

        public void Initialize(TeamTournamentMatch match)
        {
            int index = 0;
            Match = match;
            IsValid = (Match != null);
            Count = match.Teams.Count();

            foreach (var team in match.Teams)
                _teams[index++].Initialize(team);

            State = 0;
        }

        public void Refresh(bool forceRefresh)
        {
            if (forceRefresh)
            {
                OnPropertyChanged("Count");
            }
            for (int i = 0; i < Count; i++)
            {
                var tournamentTeamVM = _teams[i];
                if (forceRefresh)
                {
                    OnPropertyChanged("Team" + i + 1);
                }
                tournamentTeamVM.Refresh();
                for (int j = 0; j < tournamentTeamVM.Count; j++)
                {
                    var teamMemberVM = tournamentTeamVM.Members.ElementAt(j);
                    teamMemberVM.Score = teamMemberVM.Member?.Score.ToString() ?? "0";
                    teamMemberVM.IsQualifiedForNextRound = Match!.Winners != null && Match.Winners.Any(x => x.Members.Contains(teamMemberVM.Member));
                }
            }
        }

        public void RefreshActiveMatch()
        {
            for (int i = 0; i < Count; i++)
            {
                var tournamentTeamVM = _teams[i];
                for (int j = 0; j < tournamentTeamVM.Count; j++)
                {
                    var tournamentParticipantVM = tournamentTeamVM.Members.ElementAt(j);
                    tournamentParticipantVM.Score = tournamentParticipantVM.Member!.Score.ToString();
                }
            }
        }

        public void Refresh(TeamTournamentMatchVM target)
        {
            OnPropertyChanged("Count");
            int num = 0;
            foreach (var tournamentTeamVM in from t in Teams
                                             where t.IsValid
                                             select t)
            {
                OnPropertyChanged("Team" + num + 1);
                tournamentTeamVM.Refresh();
                num++;
            }
        }

        #region view properties

        [DataSourceProperty]
        public bool IsValid
        {
            get
            {
                return _isValid;
            }
            set
            {
                if (value != _isValid)
                {
                    _isValid = value;
                    OnPropertyChangedWithValue(value, "IsValid");
                }
            }
        }

        [DataSourceProperty]
        public int State
        {
            get
            {
                return _state;
            }
            set
            {
                if (value != _state)
                {
                    _state = value;
                    OnPropertyChangedWithValue(value, "State");
                }
            }
        }

        [DataSourceProperty]
        public int Count
        {
            get
            {
                return _count;
            }
            set
            {
                if (value != _count)
                {
                    _count = value;
                    OnPropertyChangedWithValue(value, "Count");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentTeamVM? Team1
        {
            get
            {
                return _team1;
            }
            set
            {
                if (value != _team1)
                {
                    _team1 = value;
                    OnPropertyChangedWithValue(value, "Team1");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentTeamVM? Team2
        {
            get
            {
                return _team2;
            }
            set
            {
                if (value != _team2)
                {
                    _team2 = value;
                    OnPropertyChangedWithValue(value, "Team2");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentTeamVM? Team3
        {
            get
            {
                return _team3;
            }
            set
            {
                if (value != _team3)
                {
                    _team3 = value;
                    OnPropertyChangedWithValue(value, "Team3");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentTeamVM? Team4
        {
            get
            {
                return _team4;
            }
            set
            {
                if (value != _team4)
                {
                    _team4 = value;
                    OnPropertyChangedWithValue(value, "Team4");
                }
            }
        }
        #endregion

        private TeamTournamentTeamVM? _team1;
        private TeamTournamentTeamVM? _team2;
        private TeamTournamentTeamVM? _team3;
        private TeamTournamentTeamVM? _team4;
        private int _count = -1;
        private int _state = -1;
        private bool _isValid;
        private List<TeamTournamentTeamVM> _teams;
    }
}
