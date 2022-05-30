using System.Collections.Generic;
using System.Linq;

using TaleWorlds.Library;
using TaleWorlds.Localization;

using static TaleWorlds.CampaignSystem.TournamentGames.TournamentMatch;

namespace ArenaOverhaul.TeamTournament.ViewModels
{
    public class TeamTournamentRoundVM : ViewModel
    {
        public TeamTournamentRound? Round { get; private set; }
        public bool IsFinished => MatchVMs.All(m => m.Match!.State == MatchState.Finished);
        public IEnumerable<TeamTournamentMatchVM> MatchVMs { get => _matchVMs; }

        public TeamTournamentRoundVM()
        {
            Match1 = new TeamTournamentMatchVM();
            Match2 = new TeamTournamentMatchVM();
            Match3 = new TeamTournamentMatchVM();
            Match4 = new TeamTournamentMatchVM();
            Match5 = new TeamTournamentMatchVM();
            Match6 = new TeamTournamentMatchVM();
            Match7 = new TeamTournamentMatchVM();
            Match8 = new TeamTournamentMatchVM();
            _matchVMs = new List<TeamTournamentMatchVM>
            {
                Match1,
                Match2,
                Match3,
                Match4,
                Match5,
                Match6,
                Match7,
                Match8
            };
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            _matchVMs.ForEach(x => x.RefreshValues());
        }

        public void Initialize() => _matchVMs.ForEach(x => x.Initialize());

        public void Initialize(TeamTournamentRound round, TextObject name)
        {
            Initialize(round);
            Name = name.ToString();
        }

        public void Initialize(TeamTournamentRound round)
        {
            IsValid = round != null;

            if (round != null)
            {
                Round = round;
                Count = round.MatchCount; // count of machtes
                var index = 0;
                foreach (var match in round.Matches)
                    _matchVMs[index++].Initialize(match);
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
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChangedWithValue(value, "Name");
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
        public TeamTournamentMatchVM? Match1
        {
            get
            {
                return _match1;
            }
            set
            {
                if (value != _match1)
                {
                    _match1 = value;
                    OnPropertyChangedWithValue(value, "Match1");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentMatchVM? Match2
        {
            get
            {
                return _match2;
            }
            set
            {
                if (value != _match2)
                {
                    _match2 = value;
                    OnPropertyChangedWithValue(value, "Match2");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentMatchVM? Match3
        {
            get
            {
                return _match3;
            }
            set
            {
                if (value != _match3)
                {
                    _match3 = value;
                    OnPropertyChangedWithValue(value, "Match3");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentMatchVM? Match4
        {
            get
            {
                return _match4;
            }
            set
            {
                if (value != _match4)
                {
                    _match4 = value;
                    OnPropertyChangedWithValue(value, "Match4");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentMatchVM? Match5
        {
            get
            {
                return _match5;
            }
            set
            {
                if (value != _match5)
                {
                    _match5 = value;
                    OnPropertyChangedWithValue(value, "Match5");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentMatchVM? Match6
        {
            get
            {
                return _match6;
            }
            set
            {
                if (value != _match6)
                {
                    _match6 = value;
                    OnPropertyChangedWithValue(value, "Match6");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentMatchVM? Match7
        {
            get
            {
                return _match7;
            }
            set
            {
                if (value != _match7)
                {
                    _match7 = value;
                    OnPropertyChangedWithValue(value, "Match7");
                }
            }
        }

        [DataSourceProperty]
        public TeamTournamentMatchVM? Match8
        {
            get
            {
                return _match8;
            }
            set
            {
                if (value != _match8)
                {
                    _match8 = value;
                    OnPropertyChangedWithValue(value, "Match8");
                }
            }
        }
        #endregion view properties

        private TeamTournamentMatchVM? _match1;
        private TeamTournamentMatchVM? _match2;
        private TeamTournamentMatchVM? _match3;
        private TeamTournamentMatchVM? _match4;
        private TeamTournamentMatchVM? _match5;
        private TeamTournamentMatchVM? _match6;
        private TeamTournamentMatchVM? _match7;
        private TeamTournamentMatchVM? _match8;
        private int _count = -1;
        private string _name = string.Empty;
        private bool _isValid;
        private readonly List<TeamTournamentMatchVM> _matchVMs;
    }
}