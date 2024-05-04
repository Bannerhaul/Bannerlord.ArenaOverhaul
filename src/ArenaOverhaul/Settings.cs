using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

using MCM.Common;

using TaleWorlds.Localization;

namespace ArenaOverhaul
{
    public class Settings : AttributeGlobalSettings<Settings>
    {
        public override string Id => "ArenaOverhaul_v1";
        public override string DisplayName => $"{new TextObject("{=n3lrq7FfM}Arena Overhaul")} {typeof(Settings).Assembly.GetName().Version!.ToString(3)}";
        public override string FolderName => "Arena Overhaul";
        public override string FormatType => "json2";

        //Headings
        private const string HeadingGeneral = "{=}General settings";
        private const string HeadingPractice = "{=pJ3cqM8RV}Arena Practice settings";
        private const string HeadingPracticeRewards = HeadingPractice + "/{=C5nJgxepr}Rewards";
        private const string HeadingPracticeExperience = HeadingPractice + "/{=oywDR1MSm}Experience";
        private const string HeadingExpansivePractice = "{=bPkF9slVr}Expansive Arena Practice settings";
        private const string HeadingExpansivePracticeRewards = HeadingExpansivePractice + "/{=C5nJgxepr}Rewards";
        private const string HeadingExpansivePracticeExperience = HeadingExpansivePractice + "/{=oywDR1MSm}Experience";
        private const string HeadingTournaments = "{=RRmGS7t6x}Tournament settings";
        private const string HeadingTournamentsMaterialRewards = HeadingTournaments + "/{=ZlvTL5D4T}Material rewards";
        private const string HeadingTournamentsIntangibleRewards = HeadingTournaments + "/{=BCzO2WGq9}Intangible rewards";
        private const string HeadingTournamentsExperience = HeadingTournaments + "/{=oywDR1MSm}Experience";
        private const string HeadingTournamentsTeamGame = HeadingTournaments + "/{=h6sPrqfax}Team Tournaments";

        //Reused settings, hints and values
        internal const string DropdownValueStandard = "{=gknaSzMr6}Standard";
        internal const string DropdownValueAdditive = "{=aBVrVSioz}Additive";
        internal const string DropdownValueMultiplicative = "{=TY9SqFLBs}Multiplicative";

        internal const string DropdownValueNever = "{=Hf3fpLNlh}Never";
        internal const string DropdownValueOnPrizeTierImprovement = "{=JHsRs730T}When prize tier can be improved";
        internal const string DropdownValueOnImprovement = "{=oVADnv9sb}When chances for better prize are improved";
        internal const string DropdownValueOnChange = "{=MwOn3n7yC}When situation changed";

        /*
        internal const string DropdownValuePartyBased = "{=}Party based";
        internal const string DropdownValueClanBased = "{=}Clan based";
        internal const string DropdownValueGlobal = "{=}Global";
        internal const string DropdownValueRandom = "{=}Random";

        internal const string DropdownValueEnsuresPower = "{=}Ensures highest power";
        internal const string DropdownValueEnsuresEquality = "{=}Ensures teams equality";
        internal const string DropdownValueEnsuresVariety = "{=}Ensures variety";

        internal const string DropdownValueNoControll = "{=}No controll";
        internal const string DropdownValueOwnTeam = "{=}Only own team";
        internal const string DropdownValueAffiliatedTeams = "{=}All affiliated teams";
        */

        //Arena Practice settings
        [SettingPropertyInteger("{=wtKB2udZJ}Total participants", 10, 150, Order = 0, RequireRestart = false, HintText = "{=89f5zrS7i}The total number of participants in the Arena Practice. It is recommended to be a multiple of 3. Default = 30.")]
        [SettingPropertyGroup(HeadingPractice, GroupOrder = 0)]
        public int PracticeTotalParticipants { get; set; } = 30;

        [SettingPropertyInteger("{=gaGpV2GcX}Active participants", 1, 21, Order = 1, RequireRestart = false, HintText = "{=oqpocFdUs}Еstimated number of the active participants in the Arena Practice. It is recommended to be a multiple of 3. Default = 6.")]
        [SettingPropertyGroup(HeadingPractice, GroupOrder = 0)]
        public int PracticeActiveParticipants { get; set; } = 6;

        [SettingPropertyInteger("{=0IOg4Ureb}Active participants minimum", 1, 10, Order = 2, RequireRestart = false, HintText = "{=iFjrzKw9e}The minimum number of active participants in the Arena Practice. It is recommended to be a multiple of 2. Default = 2.")]
        [SettingPropertyGroup(HeadingPractice, GroupOrder = 0)]
        public int PracticeActiveParticipantsMinimum { get; set; } = 2;

        [SettingPropertyInteger("{=dAhEzcAlx}Initial participants", 1, 15, Order = 3, RequireRestart = false, HintText = "{=UuSkcTQSd}Initial number of the active participants in the Arena Practice. Any number greater than 7 will result in multiple fighters spawning together. Default = 6.")]
        [SettingPropertyGroup(HeadingPractice, GroupOrder = 0)]
        public int PracticeInitialParticipants { get; set; } = 6;

        [SettingPropertyBool("{=BVhryIKKF}Enable loadout choice", Order = 4, RequireRestart = false, HintText = "{=zGQrZqevv}When this option is enabled, you are alowed to choose weapons for the Arena Practice.")]
        [SettingPropertyGroup(HeadingPractice, GroupOrder = 0)]
        public bool PracticeEnableLoadoutChoice { get; set; } = true;

        [SettingPropertyInteger("{=L73kYZTnf}Price for picking weapons", 0, 100, Order = 5, RequireRestart = false, HintText = "{=rZH6qYOrT}You will have to pay this sum per a wepon stage (tier) when picking a weapon for the Arena Practice. Default = 10.")]
        [SettingPropertyGroup(HeadingPractice, GroupOrder = 0)]
        public int PracticeLoadoutChoiceCost { get; set; } = 10;

        [SettingPropertyInteger("{=v7NfMok7L}Valor reward class I", 0, 50, Order = 0, RequireRestart = false, HintText = "{=GrjIc6hKr}Prize for defeating at least 3 fighters in the Arena Practice. Default = 5.")]
        [SettingPropertyGroup(HeadingPracticeRewards, GroupOrder = 0)]
        public int PracticeValorReward1 { get; set; } = 5;

        [SettingPropertyInteger("{=AFzr1BhC4}Valor reward class II", 0, 100, Order = 1, RequireRestart = false, HintText = "{=j86zigBV7}Prize for defeating at least 6 fighters in the Arena Practice. Default = 10.")]
        [SettingPropertyGroup(HeadingPracticeRewards, GroupOrder = 0)]
        public int PracticeValorReward2 { get; set; } = 10;

        [SettingPropertyInteger("{=wjlHVn6cG}Valor reward class III", 0, 250, Order = 2, RequireRestart = false, HintText = "{=0q32VAkkg}Prize for defeating at least 10 fighters in the Arena Practice. Default = 25.")]
        [SettingPropertyGroup(HeadingPracticeRewards, GroupOrder = 0)]
        public int PracticeValorReward3 { get; set; } = 25;

        [SettingPropertyInteger("{=1JAQ3hGAk}Valor reward class IV", 0, 600, Order = 3, RequireRestart = false, HintText = "{=VjGDrAK3N}Prize for defeating at least 20 fighters in the Arena Practice. Default = 60.")]
        [SettingPropertyGroup(HeadingPracticeRewards, GroupOrder = 0)]
        public int PracticeValorReward4 { get; set; } = 60;

        [SettingPropertyInteger("{=MHzw7JXgh}Valor reward class V", 0, 1500, Order = 4, RequireRestart = false, HintText = "{=ws4xpzph6}Prize for defeating at least 35 fighters in the Arena Practice. Can't be achieved with a standard number of participants. Default = 150.")]
        [SettingPropertyGroup(HeadingPracticeRewards, GroupOrder = 0)]
        public int PracticeValorReward5 { get; set; } = 150;

        [SettingPropertyDropdown("{=rQHQBnmZL}Champion prize calculation", Order = 5, RequireRestart = false, HintText = "{=8mGYZd0fL}Specify how last man standing in the Arena Practice should be rewarded. Standard - with a special prize. Additive - with a special prize and any valor reward earned. Multiplicative - with a special prize for each valor class earned. Native is [Standard]. Default is [Additive].")]
        [SettingPropertyGroup(HeadingPracticeRewards, GroupOrder = 0)]
        public Dropdown<string> PracticeChampionPrizeCalculation { get; set; } = new Dropdown<string>(new string[]
        {
            DropdownValueStandard,
            DropdownValueAdditive,
            DropdownValueMultiplicative
        }, 1);

        [SettingPropertyInteger("{=oTOFfuU2c}Champion prize", 0, 2500, Order = 6, RequireRestart = false, HintText = "{=2ovQejh10}Base prize for being the last man standing in the Arena Practice. Default = 250.")]
        [SettingPropertyGroup(HeadingPracticeRewards, GroupOrder = 0)]
        public int PracticeChampionReward { get; set; } = 250;

        [SettingPropertyFloatingInteger("{=9dP3VJpOG}Experience gain rate", 0f, 1f, "#0.0%", Order = 0, RequireRestart = false, HintText = "{=gGEXaGsKV}Experience gain rate in the Arena Practice fights. Native = 6%. Default = 16.5%.")]
        [SettingPropertyGroup(HeadingPracticeExperience, GroupOrder = 1)]
        public float PracticeExperienceRate { get; set; } = 0.165f;

        //Expansive Arena Practice settings
        [SettingPropertyInteger("{=wtKB2udZJ}Total participants", 10, 150, Order = 0, RequireRestart = false, HintText = "{=65Ny4vNEE}The total number of participants in the Expansive Arena Practice. It is recommended to be a multiple of 3. Default = 90.")]
        [SettingPropertyGroup(HeadingExpansivePractice, GroupOrder = 1)]
        public int ExpansivePracticeTotalParticipants { get; set; } = 90;

        [SettingPropertyInteger("{=gaGpV2GcX}Active participants", 1, 21, Order = 1, RequireRestart = false, HintText = "{=sIQdRBwg9}Еstimated number of the active participants in the Expansive Arena Practice. It is recommended to be a multiple of 3. Default = 9.")]
        [SettingPropertyGroup(HeadingExpansivePractice, GroupOrder = 1)]
        public int ExpansivePracticeActiveParticipants { get; set; } = 9;

        [SettingPropertyInteger("{=0IOg4Ureb}Active participants minimum", 1, 10, Order = 2, RequireRestart = false, HintText = "{=ZaLfvG7eX}The minimum number of active participants in the Expansive Arena Practice. It is recommended to be a multiple of 2. Default = 4.")]
        [SettingPropertyGroup(HeadingExpansivePractice, GroupOrder = 1)]
        public int ExpansivePracticeActiveParticipantsMinimum { get; set; } = 4;

        [SettingPropertyInteger("{=dAhEzcAlx}Initial participants", 1, 15, Order = 3, RequireRestart = false, HintText = "{=XwzmSDpAr}Initial number of the active participants in the Expansive Arena Practice. Any number greater than 7 will result in multiple fighters spawning together. Default = 7.")]
        [SettingPropertyGroup(HeadingExpansivePractice, GroupOrder = 1)]
        public int ExpansivePracticeInitialParticipants { get; set; } = 7;

        [SettingPropertyBool("{=BVhryIKKF}Enable loadout choice", Order = 4, RequireRestart = false, HintText = "{=9otxYlTUa}When this option is enabled, you are alowed to choose weapons for the Expansive Arena Practice.")]
        [SettingPropertyGroup(HeadingExpansivePractice, GroupOrder = 1)]
        public bool ExpansivePracticeEnableLoadoutChoice { get; set; } = true;

        [SettingPropertyInteger("{=L73kYZTnf}Price for picking weapons", 0, 100, Order = 5, RequireRestart = false, HintText = "{=B9xXCFneS}You will have to pay this sum per a wepon stage (tier) when picking a weapon for the Expansive Arena Practice. Default = 10.")]
        [SettingPropertyGroup(HeadingExpansivePractice, GroupOrder = 1)]
        public int ExpansivePracticeLoadoutChoiceCost { get; set; } = 10;

        [SettingPropertyInteger("{=v7NfMok7L}Valor reward class I", 0, 50, Order = 0, RequireRestart = false, HintText = "{=M7utBJV6O}Prize for defeating at least 3 fighters in the Expansive Arena Practice. Default = 10.")]
        [SettingPropertyGroup(HeadingExpansivePracticeRewards, GroupOrder = 0)]
        public int ExpansivePracticeValorReward1 { get; set; } = 10;

        [SettingPropertyInteger("{=AFzr1BhC4}Valor reward class II", 0, 100, Order = 1, RequireRestart = false, HintText = "{=5Zkk4IRnM}Prize for defeating at least 6 fighters in the Expansive Arena Practice. Default = 20.")]
        [SettingPropertyGroup(HeadingExpansivePracticeRewards, GroupOrder = 0)]
        public int ExpansivePracticeValorReward2 { get; set; } = 20;

        [SettingPropertyInteger("{=wjlHVn6cG}Valor reward class III", 0, 250, Order = 2, RequireRestart = false, HintText = "{=ivrbCerwG}Prize for defeating at least 10 fighters in the Expansive Arena Practice. Default = 50.")]
        [SettingPropertyGroup(HeadingExpansivePracticeRewards, GroupOrder = 0)]
        public int ExpansivePracticeValorReward3 { get; set; } = 50;

        [SettingPropertyInteger("{=1JAQ3hGAk}Valor reward class IV", 0, 600, Order = 3, RequireRestart = false, HintText = "{=41COOOrtk}Prize for defeating at least 20 fighters in the Expansive Arena Practice. Default = 120.")]
        [SettingPropertyGroup(HeadingExpansivePracticeRewards, GroupOrder = 0)]
        public int ExpansivePracticeValorReward4 { get; set; } = 120;

        [SettingPropertyInteger("{=MHzw7JXgh}Valor reward class V", 0, 1500, Order = 4, RequireRestart = false, HintText = "{=aPjrww0Yz}Prize for defeating at least 35 fighters in the Expansive Arena Practice. Default = 300.")]
        [SettingPropertyGroup(HeadingExpansivePracticeRewards, GroupOrder = 0)]
        public int ExpansivePracticeValorReward5 { get; set; } = 300;

        [SettingPropertyDropdown("{=rQHQBnmZL}Champion prize calculation", Order = 5, RequireRestart = false, HintText = "{=aUEgf6bh5}Specify how last man standing in the Expansive Arena Practice should be rewarded. Standard - with a special prize. Additive - with a special prize and any valor reward earned. Multiplicative - with a special prize for each valor class earned. Default is [Additive].")]
        [SettingPropertyGroup(HeadingExpansivePracticeRewards, GroupOrder = 0)]
        public Dropdown<string> ExpansivePracticeChampionPrizeCalculation { get; set; } = new Dropdown<string>(new string[]
        {
            DropdownValueStandard,
            DropdownValueAdditive,
            DropdownValueMultiplicative
        }, 1);

        [SettingPropertyInteger("{=oTOFfuU2c}Champion prize", 0, 2500, Order = 6, RequireRestart = false, HintText = "{=FP34ZZ1b0}Base prize for being the last man standing in the Expansive Arena Practice. Default = 500.")]
        [SettingPropertyGroup(HeadingExpansivePracticeRewards, GroupOrder = 0)]
        public int ExpansivePracticeChampionReward { get; set; } = 500;

        [SettingPropertyBool("{=zDfS2JtnA}Enable experience gain for viewers", Order = 0, RequireRestart = false, HintText = "{=YyniNTgz5}Only heroes and troops of rank 3 and up are allowed to enter Expansive Arena Practice. When this option is enabled, lower-ranking troops will gain some experience when their comrades successfully land hits and make takedowns in the arena.")]
        [SettingPropertyGroup(HeadingExpansivePracticeExperience, GroupOrder = 1)]
        public bool EnableViewerExperienceGain { get; set; } = true;

        [SettingPropertyFloatingInteger("{=9dP3VJpOG}Experience gain rate", 0f, 1f, "#0.0%", Order = 1, RequireRestart = false, HintText = "{=hTHzr3Q9U}Experience gain rate in the Expansive Arena Practice fights. Native = 6%. Default = 33.3%.")]
        [SettingPropertyGroup(HeadingExpansivePracticeExperience, GroupOrder = 1)]
        public float ExpansivePracticeExperienceRate { get; set; } = 0.333f;

        //Tournament settings
        [SettingPropertyInteger("{=hS7XbrLGG}Maximum bet", 100, 1500, Order = 0, RequireRestart = false, HintText = "{=gQvJ8Vvsk}The maximum amount of gold that is usually allowed to be wagered in each round of a tournament. Native = 150. Default = 500.")]
        [SettingPropertyGroup(HeadingTournaments, GroupOrder = 2)]
        public int TournamentMaximumBet { get; set; } = 500;

        [SettingPropertyBool("{=qPAga7DLd}Enable randomized betting odds", Order = 1, RequireRestart = false, HintText = "{=kGyR7NsKh}When this option is enabled, bet odds are slightly randomized, but still mostly based on the prediction of the player's success.")]
        [SettingPropertyGroup(HeadingTournaments, GroupOrder = 2)]
        public bool EnableRandomizedBettingOdds { get; set; } = true;

        [SettingPropertyDropdown("{=cCAOeRdmt}Prize reroll condition", Order = 2, RequireRestart = false, HintText = "{=CDGmDeYii}Specify when and if tournament prizes should be rerolled. Normally prizes are rerolled when player joins the tournament - if the nubmber of participating nobles changed since the tournament was created (affects prize quality). Native is [When situation changed]. Default is [When prize tier can be improved].")]
        [SettingPropertyGroup(HeadingTournaments, GroupOrder = 2)]
        public Dropdown<string> TournamentPrizeRerollCondition { get; set; } = new Dropdown<string>(new string[]
        {
            DropdownValueNever,
            DropdownValueOnPrizeTierImprovement,
            DropdownValueOnImprovement,
            DropdownValueOnChange
        }, 1);

        [SettingPropertyBool("{=IfsaDqygk}Enable gold prizes", Order = 0, RequireRestart = false, HintText = "{=yVAG4f83d}When this option is enabled, there are also gold prizes in tournaments. This is in addition to the usual prize items and bet wins.")]
        [SettingPropertyGroup(HeadingTournamentsMaterialRewards, GroupOrder = 0)]
        public bool EnableTournamentGoldPrizes { get; set; } = true;

        [SettingPropertyBool("{=fH4Ev0UoC}Enable prize scaling", Order = 1, RequireRestart = false, HintText = "{=1THMacQwa}When this option is enabled, tournament prizes are scaling with player's renown. Slightly unconventional, but helps keep tournaments useful in the mid and late game.")]
        [SettingPropertyGroup(HeadingTournamentsMaterialRewards, GroupOrder = 0)]
        public bool EnableTournamentPrizeScaling { get; set; } = true;

        [SettingPropertyInteger("{=DyNe9qH4v}Reward for the won round", 0, 1000, Order = 3, RequireRestart = false, HintText = "{=aAquVsgYJ}The amount of gold that participants receive for each round of the tournament they won. Native = 0. Default = 0.")]
        [SettingPropertyGroup(HeadingTournamentsMaterialRewards, GroupOrder = 0)]
        public int TournamentRoundWonReward { get; set; } = 0;

        [SettingPropertyInteger("{=lkgaScPft}Renown reward for the champion", 0, 10, Order = 0, RequireRestart = false, HintText = "{=GWmYOkfOs}The amount of renown that tournament campion receives. Native = 3. Default = 3.")]
        [SettingPropertyGroup(HeadingTournamentsIntangibleRewards, GroupOrder = 1)]
        public int TournamentBaseRenownReward { get; set; } = 3;

        [SettingPropertyInteger("{=1AR1Jq493}Renown reward for defeated heroes", 0, 5, Order = 1, RequireRestart = false, HintText = "{=Fbt8Iewl4}Whenever a tournament participant strikes down a hero who has a comparable or higher renown or a higher overall tournament win record, he gets the specified amount of renown. Can't exceed double the champion renown reward over the course of tournament. Native = 0. Default = 1.")]
        [SettingPropertyGroup(HeadingTournamentsIntangibleRewards, GroupOrder = 1)]
        public int TournamentTakedownRenownReward { get; set; } = 1;

        [SettingPropertyInteger("{=eLKi8Ptgm}Influence reward for the champion", 0, 100, Order = 2, RequireRestart = false, HintText = "{=uYmFPwO8z}The amount of influence that tournament campion receives, if he is a noble lord of any kingdom. Native = 1. Default = 10.")]
        [SettingPropertyGroup(HeadingTournamentsIntangibleRewards, GroupOrder = 1)]
        public int TournamentInfluenceReward { get; set; } = 10;

        [SettingPropertyFloatingInteger("{=9dP3VJpOG}Experience gain rate", 0f, 1f, "#0.0%", Order = 0, RequireRestart = false, HintText = "{=C5fcetmfG}Experience gain rate in the tournament fights. Native = 33%. Default = 66.6%.")]
        [SettingPropertyGroup(HeadingTournamentsExperience, GroupOrder = 2)]
        public float TournamentExperienceRate { get; set; } = 0.666f;

        // Team Tournament settings
        [SettingPropertyBool("{=PSnYupZhT}Enable Team Tournaments", RequireRestart = false, HintText = "{=qHyqXp9tK}Adds an option for joining and participating in the tournaments as a team.", Order = 0)]
        [SettingPropertyGroup(HeadingTournamentsTeamGame, GroupOrder = 3)]
        public bool EnableTeamTournaments { get; set; } = true;

        [SettingPropertyInteger("{=Q6qQge312}Maximum team size", 2, 16, RequireRestart = false, HintText = "{=4q3hSTZPZ}The maximum number of members in a team. A team has to consist of at least two members.", Order = 5)]
        [SettingPropertyGroup(HeadingTournamentsTeamGame, GroupOrder = 3)]
        public int TeamSizeMax { get; set; } = 8;

        [SettingPropertyDropdown("{=6o45aTuZi}Maximum number of teams", RequireRestart = false, HintText = "{=34r9jsdJq}The maximum number of teams in a tournament. A tournament has to have at least 8 teams.", Order = 10)]
        [SettingPropertyGroup(HeadingTournamentsTeamGame, GroupOrder = 3)]
        public Dropdown<int> TeamsCountMax { get; set; } = new Dropdown<int>(new int[]
        {
            8,
            16,
            32
        }, 1);

        /*
        [SettingPropertyDropdown("{=}Teams genesis", Order = 15, RequireRestart = false, HintText = "{=} Default is [Clan based].")]
        [SettingPropertyGroup(HeadingTournamentsTeamGame, GroupOrder = 3)]
        public Dropdown<string> TeamGenesis { get; set; } = new Dropdown<string>(new string[]
        {
            DropdownValuePartyBased,
            DropdownValueClanBased,
            DropdownValueGlobal,
            DropdownValueRandom
        }, 1);

        [SettingPropertyDropdown("{=}Teams composition", Order = 16, RequireRestart = false, HintText = "{=} Default is [Ensures highest power].")]
        [SettingPropertyGroup(HeadingTournamentsTeamGame, GroupOrder = 3)]
        public Dropdown<string> TeamComposition { get; set; } = new Dropdown<string>(new string[]
        {
            DropdownValueEnsuresPower,
            DropdownValueEnsuresEquality,
            DropdownValueEnsuresVariety,
            DropdownValueRandom
        }, 0);

        [SettingPropertyDropdown("{=}Player controll over teams composition", Order = 17, RequireRestart = false, HintText = "{=} Default is [Only own team].")]
        [SettingPropertyGroup(HeadingTournamentsTeamGame, GroupOrder = 3)]
        public Dropdown<string> PlayerControllOverComposition { get; set; } = new Dropdown<string>(new string[]
        {
            DropdownValueNoControll,
            DropdownValueOwnTeam,
            DropdownValueAffiliatedTeams,
            DropdownValueRandom
        }, 0);
        */

        [SettingPropertyInteger("{=l2FTrzzXX}Team 1 Color Index", 0, 157, HintText = "{=3r3TStZPU}Set Team's banner color by index value. Check https://bannerlord.party/banner-colors/ for the list of available colors.", Order = 25)]
        [SettingPropertyGroup(HeadingTournamentsTeamGame, GroupOrder = 3)]
        public int TeamOneColor { get; set; } = 83;

        [SettingPropertyInteger("{=jWYXBiEZp}Team 2 Color Index", 0, 157, HintText = "{=3r3TStZPU}Set Team's banner color by index value. Check https://bannerlord.party/banner-colors/ for the list of available colors.", Order = 26)]
        [SettingPropertyGroup(HeadingTournamentsTeamGame, GroupOrder = 3)]
        public int TeamTwoColor { get; set; } = 119;

        [SettingPropertyInteger("{=iRvUExaRI}Team 3 Color Index", 0, 157, HintText = "{=3r3TStZPU}Set Team's banner color by index value. Check https://bannerlord.party/banner-colors/ for the list of available colors.", Order = 27)]
        [SettingPropertyGroup(HeadingTournamentsTeamGame, GroupOrder = 3)]
        public int TeamThreeColor { get; set; } = 88;

        [SettingPropertyInteger("{=F6qBgeAOk}Team 4 Color Index", 0, 157, HintText = "{=3r3TStZPU}Set Team's banner color by index value. Check https://bannerlord.party/banner-colors/ for the list of available colors.", Order = 28)]
        [SettingPropertyGroup(HeadingTournamentsTeamGame, GroupOrder = 3)]
        public int TeamFourColor { get; set; } = 84;
    }
}