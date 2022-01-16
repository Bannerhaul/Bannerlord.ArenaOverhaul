using System.Collections.Generic;
using System.Linq;

using TaleWorlds.Library;

namespace ArenaOverhaul.TeamTournament.ViewModels
{
    public class TeamTournamentTeamVM : ViewModel
    {
        public IEnumerable<TeamTournamentMemberVM> Members { get => _members; }
        public TeamTournamentTeam? Team { get; private set; }

        public TeamTournamentTeamVM()
        {
            Participant1 = new TeamTournamentMemberVM();
            Participant2 = new TeamTournamentMemberVM();
            Participant3 = new TeamTournamentMemberVM();
            Participant4 = new TeamTournamentMemberVM();
            Participant5 = new TeamTournamentMemberVM();
            Participant6 = new TeamTournamentMemberVM();
            Participant7 = new TeamTournamentMemberVM();
            Participant8 = new TeamTournamentMemberVM();
            _members = new List<TeamTournamentMemberVM>
            {
                Participant1,
                Participant2,
                Participant3,
                Participant4,
                Participant5,
                Participant6,
                Participant7,
                Participant8
            };
        }

        public IEnumerable<TeamTournamentMemberVM> GetMembers() => Members.Where(x => x.IsValid);

        public TeamTournamentMemberVM GetTeamLeader() => Members.FirstOrDefault(x => x.Member == Team!.GetTeamLeader());

        public override void RefreshValues()
        {
            base.RefreshValues();
            _members.ForEach(x => x.RefreshValues());
        }

        public void Initialize()
        {
            IsValid = Team != null;
            _members[0].Refresh(Team!.GetTeamLeader(), Color.FromUint(Team.TeamColor));
        }

        public void Initialize(TeamTournamentTeam team)
        {
            Team = team;
            Count = 1;
            Initialize();
        }

        public void Refresh()
        {
            IsValid = (Team != null);
            OnPropertyChanged("Count");

            int num = 0;
            foreach (var member in Members.Where(x => x.IsValid))
            {
                OnPropertyChanged("Participant" + num++);
                member.Refresh();
            }
        }

        #region view properties
        [DataSourceProperty]
        public bool IsValid
        {
            get => _isValid;
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
        public int Score
        {
            get => _score;
            set
            {
                if (value != _score)
                {
                    _score = value;
                    OnPropertyChangedWithValue(value, "Score");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentMemberVM? Participant1
        {
            get => _participant1;
            set
            {
                if (value != _participant1)
                {
                    _participant1 = value;
                    OnPropertyChangedWithValue(value, "Participant1");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentMemberVM? Participant2
        {
            get => _participant2;
            set
            {
                if (value != _participant2)
                {
                    _participant2 = value;
                    OnPropertyChangedWithValue(value, "Participant2");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentMemberVM? Participant3
        {
            get => _participant3;
            set
            {
                if (value != _participant3)
                {
                    _participant3 = value;
                    OnPropertyChangedWithValue(value, "Participant3");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentMemberVM? Participant4
        {
            get
            {
                return _participant4;
            }
            set
            {
                if (value != _participant4)
                {
                    _participant4 = value;
                    OnPropertyChangedWithValue(value, "Participant4");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentMemberVM? Participant5
        {
            get
            {
                return _participant5;
            }
            set
            {
                if (value != _participant5)
                {
                    _participant5 = value;
                    OnPropertyChangedWithValue(value, "Participant5");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentMemberVM? Participant6
        {
            get => _participant6;
            set
            {
                if (value != _participant6)
                {
                    _participant6 = value;
                    OnPropertyChangedWithValue(value, "Participant6");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentMemberVM? Participant7
        {
            get
            {
                return _participant7;
            }
            set
            {
                if (value != _participant7)
                {
                    _participant7 = value;
                    OnPropertyChangedWithValue(value, "Participant7");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentMemberVM? Participant8
        {
            get
            {
                return _participant8;
            }
            set
            {
                if (value != _participant8)
                {
                    _participant8 = value;
                    OnPropertyChangedWithValue(value, "Participant8");
                }
            }
        }

        [DataSourceProperty]
        public int Count
        {
            get => _count;
            set
            {
                if (value != _count)
                {
                    _count = value;
                    OnPropertyChangedWithValue(value, "Count");
                }
            }
        }
        #endregion

        private int _count = -1;
        private TeamTournamentMemberVM? _participant1;
        private TeamTournamentMemberVM? _participant2;
        private TeamTournamentMemberVM? _participant3;
        private TeamTournamentMemberVM? _participant4;
        private TeamTournamentMemberVM? _participant5;
        private TeamTournamentMemberVM? _participant6;
        private TeamTournamentMemberVM? _participant7;
        private TeamTournamentMemberVM? _participant8;
        private int _score;
        private bool _isValid;
        private List<TeamTournamentMemberVM> _members;
    }
}