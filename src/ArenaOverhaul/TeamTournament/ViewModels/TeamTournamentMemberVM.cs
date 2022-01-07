using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;

namespace ArenaOverhaul.TeamTournament.ViewModels
{
    public class TeamTournamentMemberVM : ViewModel
    {
        public TeamTournamentMember Member { get; private set; }

        public TeamTournamentMemberVM()
        {
            _visual = new ImageIdentifierVM(ImageIdentifierType.Null);
            _character = new CharacterViewModel(CharacterViewModel.StanceTypes.CelebrateVictory);
        }

        public TeamTournamentMemberVM(TeamTournamentMember member) : this()
        {
            Refresh(member, Color.FromUint(member.Team.TeamColor));
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            if (IsInitialized)
                Refresh(Member, TeamColor);
        }

        public void Refresh(TeamTournamentMember member, Color teamColor)
        {
            Member = member;
            TeamColor = teamColor;
            State = member == null ? 0 : (member.IsPlayer ? 2 : 1);
            IsInitialized = true;
            if (member != null)
            {
                Name = member.Character.Name.ToString() + "'s Team";
                Character = new CharacterViewModel(CharacterViewModel.StanceTypes.CelebrateVictory);
                Character.FillFrom(member.Character, -1);
                Visual = new ImageIdentifierVM(CharacterCode.CreateFrom(member.Character));
                IsValid = true;
                IsMainHero = member.IsPlayer;
            }
        }

        public void Refresh()
        {
            OnPropertyChanged("Name");
            OnPropertyChanged("Visual");
            OnPropertyChanged("Score");
            OnPropertyChanged("State");
            OnPropertyChanged("TeamColor");
            OnPropertyChanged("IsDead");
            IsMainHero = (Member != null && Member.IsPlayer);
        }

        #region view properties
        [DataSourceProperty]
        public bool IsInitialized
        {
            get => _isInitialized;
            set
            {
                if (value != _isInitialized)
                {
                    _isInitialized = value;
                    OnPropertyChangedWithValue(value, "IsInitialized");
                }
            }
        }

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
        public bool IsDead
        {
            get => _isDead;
            set
            {
                if (value != _isDead)
                {
                    _isDead = value;
                    OnPropertyChangedWithValue(value, "IsDead");
                }
            }
        }

        [DataSourceProperty]
        public bool IsMainHero
        {
            get => _isMainHero;
            set
            {
                if (value != _isMainHero)
                {
                    _isMainHero = value;
                    OnPropertyChangedWithValue(value, "IsMainHero");
                }
            }
        }

        [DataSourceProperty]
        public Color TeamColor
        {
            get => _teamColor;
            set
            {
                if (value != _teamColor)
                {
                    _teamColor = value;
                    OnPropertyChangedWithValue(value, "TeamColor");
                }
            }
        }

        [DataSourceProperty]
        public ImageIdentifierVM Visual
        {
            get => _visual;
            set
            {
                if (value != _visual)
                {
                    _visual = value;
                    OnPropertyChangedWithValue(value, "Visual");
                }
            }
        }

        [DataSourceProperty]
        public int State
        {
            get => _state;
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
        public bool IsQualifiedForNextRound
        {
            get => _isQualifiedForNextRound;
            set
            {
                if (value != _isQualifiedForNextRound)
                {
                    _isQualifiedForNextRound = value;
                    OnPropertyChangedWithValue(value, "IsQualifiedForNextRound");
                }
            }
        }

        [DataSourceProperty]
        public string Score
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
        public string Name
        {
            get => _name;
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
        public CharacterViewModel Character
        {
            get => _character;
            set
            {
                if (value != _character)
                {
                    _character = value;
                    OnPropertyChangedWithValue(value, "Character");
                }
            }
        }
        #endregion

        private bool _isInitialized;
        private bool _isValid;
        private string _name = "";
        private string _score = "-";
        private bool _isQualifiedForNextRound;
        private int _state = -1;
        private ImageIdentifierVM _visual;
        private Color _teamColor;
        private bool _isDead;
        private bool _isMainHero;
        private CharacterViewModel _character;

#pragma warning disable IDE0051 // Remove unused private members
        /// <summary>
        /// DO NOT REMOVE CALLED DYNAMICALLY
        /// </summary>
        private void ExecuteOpenEncyclopedia()
        {
            if (Member != null && Member.Character != null)
            {
                Campaign.Current.EncyclopediaManager.GoToLink(Member.Character.EncyclopediaLink);
            }
        }
#pragma warning restore IDE0051 // Remove unused private members
    }
}
