using ArenaOverhaul.Extensions;

using MCM.Abstractions;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using MCM.Common;

using System.Collections.Generic;

using TaleWorlds.Localization;

namespace ArenaOverhaul.ModSettings
{
    public class Settings : AttributeGlobalSettings<Settings>
    {
        public override string Id => "ArenaOverhaul_v1";
        public override string DisplayName => $"{new TextObject("{=n3lrq7FfM}Arena Overhaul")} {typeof(Settings).Assembly.GetName().Version!.ToString(3)}";
        public override string FolderName => "Arena Overhaul";
        public override string FormatType => "json2";

        //Presets
        private const string PresetSavingMoney = "{=}Optimized to save money";
        private const string PresetBetterTraining = "{=}Optimized to train heroes";

        //Headings
        private const string HeadingGeneral = "{=}General settings";

        private const string HeadingPractice = "{=pJ3cqM8RV}Arena Practice settings";
        private const string HeadingPracticeRewards = HeadingPractice + "/{=C5nJgxepr}Rewards";
        private const string HeadingPracticeExperience = HeadingPractice + "/{=oywDR1MSm}Experience";

        private const string HeadingExpansivePractice = "{=bPkF9slVr}Expansive Arena Practice settings";
        private const string HeadingExpansivePracticeRewards = HeadingExpansivePractice + "/{=C5nJgxepr}Rewards";
        private const string HeadingExpansivePracticeExperience = HeadingExpansivePractice + "/{=oywDR1MSm}Experience";

        private const string HeadingParryPractice = "{=}Parry Practice settings";
        private const string HeadingParryPracticeExperience = HeadingParryPractice + "/{=oywDR1MSm}Experience";

        private const string HeadingTeamPractice = "{=}Team Arena Practice settings";
        private const string HeadingTeamPracticeRewards = HeadingTeamPractice + "/{=C5nJgxepr}Rewards";
        private const string HeadingTeamPracticeExperience = HeadingTeamPractice + "/{=oywDR1MSm}Experience";
        private const string HeadingTeamPracticeTechnicalSettings = HeadingTeamPractice + "/{=}Technical settings";

        private const string HeadingPracticeCompanionDefaults = "{=}Default Arena Practice settings for heroes";

        private const string HeadingTournaments = "{=RRmGS7t6x}Tournament settings";
        private const string HeadingTournamentsMaterialRewards = HeadingTournaments + "/{=ZlvTL5D4T}Material rewards";
        private const string HeadingTournamentsIntangibleRewards = HeadingTournaments + "/{=BCzO2WGq9}Intangible rewards";
        private const string HeadingTournamentsExperience = HeadingTournaments + "/{=oywDR1MSm}Experience";
        private const string HeadingTournamentsTeamGame = HeadingTournaments + "/{=h6sPrqfax}Team Tournaments";

        private const string HeadingCompatibility = "{=WnQ4qOI7d}Compatibility settings";
        private const string HeadingLogging = "{=}Logging settings";

        //Reused settings, hints and values
        internal const string DropdownValueStandard = "{=gknaSzMr6}Standard";
        internal const string DropdownValueAdditive = "{=aBVrVSioz}Additive";
        internal const string DropdownValueMultiplicative = "{=TY9SqFLBs}Multiplicative";

        internal const string DropdownValueNever = "{=Hf3fpLNlh}Never";
        internal const string DropdownValueOnPrizeTierImprovement = "{=JHsRs730T}When prize tier can be improved";
        internal const string DropdownValueOnImprovement = "{=oVADnv9sb}When chances for better prize are improved";
        internal const string DropdownValueOnChange = "{=MwOn3n7yC}When situation changed";

        internal const string DropdownValueStandardItemsOnly = "{=}Non-unique items only";
        internal const string DropdownValueAlwaysExceptForMounts = "{=}Always, except for unique mounts";
        internal const string DropdownValueAlways = "{=}Always";

        //Default Arena Practice settings for Companions
        [SettingPropertyBool("{=}Enable loadout choice", Order = 0, RequireRestart = true, HintText = "{=}This is a default value that will be used to configure hero loadout options. When enabled, corresponding hero is allowed to choose weapons for arena practice matches, and you will have to pay the usual fee for this. Otherwise, they will use random weapons like everyone else. Default is True.")]
        [SettingPropertyGroup(HeadingPracticeCompanionDefaults, GroupOrder = 0)]
        public bool EnableLoadoutChoice { get; set; } = true;

        [SettingPropertyBool("{=}Only priority loadouts", Order = 1, RequireRestart = true, HintText = "{=}This is a default value that will be used to configure hero loadout options. When enabled, corresponding hero will only choose practice weapons if any weapon preference is set and at least one matching weapon loadout is available in the arena. Otherwise, they will use random weapons like everyone else. Default is True.")]
        [SettingPropertyGroup(HeadingPracticeCompanionDefaults, GroupOrder = 0)]
        public bool OnlyPriorityLoadouts { get; set; } = true;

        [SettingPropertyBool("{=}Prioritize expensive equipment", Order = 2, RequireRestart = true, HintText = "{=}This is a default value that will be used to configure hero loadout options. When enabled, corresponding hero will select weapons for arena practice matches, prioritizing better and more expensive equipment. Otherwise, the least expensive set of weapons that meets the given preferences will be selected. Default is False.")]
        [SettingPropertyGroup(HeadingPracticeCompanionDefaults, GroupOrder = 0)]
        public bool PrioritizeExpensiveEquipment { get; set; } = false;

        //Arena Practice settings
        [SettingPropertyBool("{=}Enable playing as a companion hero", Order = 0, RequireRestart = false, HintText = "{=}When this option is enabled, you will be allowed to send a companion to participate in the Arena Practice in your place and control that companion in the arena. Native is False. Default is True.")]
        [SettingPropertyGroup(HeadingPractice, GroupOrder = 10)]
        public bool EnablePracticeAgentSwitching { get; set; } = true;

        [SettingPropertyInteger("{=wtKB2udZJ}Total participants", 10, 150, Order = 10, RequireRestart = false, HintText = "{=89f5zrS7i}The total number of participants in the Arena Practice. It is recommended to be a multiple of 3. Default = 30.")]
        [SettingPropertyGroup(HeadingPractice, GroupOrder = 10)]
        public int PracticeTotalParticipants { get; set; } = 30;

        [SettingPropertyInteger("{=gaGpV2GcX}Active participants", 1, 21, Order = 11, RequireRestart = false, HintText = "{=oqpocFdUs}Еstimated number of the active participants in the Arena Practice. It is recommended to be a multiple of 3. Default = 6.")]
        [SettingPropertyGroup(HeadingPractice, GroupOrder = 0)]
        public int PracticeActiveParticipants { get; set; } = 6;

        [SettingPropertyInteger("{=0IOg4Ureb}Active participants minimum", 1, 10, Order = 12, RequireRestart = false, HintText = "{=iFjrzKw9e}The minimum number of active participants in the Arena Practice. It is recommended to be a multiple of 2. Default = 2.")]
        [SettingPropertyGroup(HeadingPractice, GroupOrder = 10)]
        public int PracticeActiveParticipantsMinimum { get; set; } = 2;

        [SettingPropertyInteger("{=dAhEzcAlx}Initial participants", 1, 15, Order = 13, RequireRestart = false, HintText = "{=UuSkcTQSd}Initial number of the active participants in the Arena Practice. Any number greater than 7 will result in multiple fighters spawning together. Default = 6.")]
        [SettingPropertyGroup(HeadingPractice, GroupOrder = 10)]
        public int PracticeInitialParticipants { get; set; } = 6;

        [SettingPropertyInteger("{=}AI teams", 0, 21, Order = 14, RequireRestart = false, HintText = "{=}Number of AI teams in the Arena Practice match. Zero means it will be equal to the number of active participants, which minimizes the chances of encountering AI teammates working together. Setting it to 1 means that everyone will fight you. Default = 0.")]
        [SettingPropertyGroup(HeadingPractice, GroupOrder = 10)]
        public int PracticeAITeamsCount { get; set; } = 0;

        [SettingPropertyBool("{=BVhryIKKF}Enable loadout choice", Order = 20, RequireRestart = false, HintText = "{=zGQrZqevv}When this option is enabled, you are alowed to choose weapons for the Arena Practice.")]
        [SettingPropertyGroup(HeadingPractice, GroupOrder = 10)]
        public bool PracticeEnableLoadoutChoice { get; set; } = true;

        [SettingPropertyInteger("{=L73kYZTnf}Price for picking weapons", 0, 100, Order = 21, RequireRestart = false, HintText = "{=rZH6qYOrT}You will have to pay this sum per a wepon stage (tier) when picking a weapon for the Arena Practice. Default = 10.")]
        [SettingPropertyGroup(HeadingPractice, GroupOrder = 10)]
        public int PracticeLoadoutChoiceCost { get; set; } = 10;

        [SettingPropertyDropdown("{=}Defensive practice equipment", Order = 22, RequireRestart = false, HintText = "{=}Specify what armor or clothing should be worn by Arena Practice participants. Choosing [Tournament Armor] will give the same result as [Battle Equipment] unless changed by other mods. It's not recommended to change this setting unless you have a specific goal in mind. While it can be fun, wearing armor renders most ranged practice weapons ineffective. Native is [Practice clothes]. Default is [Practice Clothes].")]
        [SettingPropertyGroup(HeadingPractice, GroupOrder = 10)]
        public Dropdown<DropdownEnumItem<PracticeEquipmentType>> PracticeEquipment { get; set; } = new Dropdown<DropdownEnumItem<PracticeEquipmentType>>(DropdownEnumItem<PracticeEquipmentType>.SetDropdownListFromEnum(), 0);

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

        [SettingPropertyDropdown("{=rQHQBnmZL}Champion prize calculation", Order = 5, RequireRestart = false, HintText = "{=8mGYZd0fL}Specify how last man standing in the Arena Practice should be rewarded. Standard - with a special prize. Additive - with a special prize and any valor reward earned. Multiplicative - with a special prize for each valor class earned above class I. Native is [Standard]. Default is [Additive].")]
        [SettingPropertyGroup(HeadingPracticeRewards, GroupOrder = 0)]
        public Dropdown<string> PracticeChampionPrizeCalculation { get; set; } = new Dropdown<string>(
        [
            DropdownValueStandard,
            DropdownValueAdditive,
            DropdownValueMultiplicative
        ], 1);

        [SettingPropertyInteger("{=oTOFfuU2c}Champion prize", 0, 2500, Order = 6, RequireRestart = false, HintText = "{=2ovQejh10}Base prize for being the last man standing in the Arena Practice. Default = 250.")]
        [SettingPropertyGroup(HeadingPracticeRewards, GroupOrder = 0)]
        public int PracticeChampionReward { get; set; } = 250;

        [SettingPropertyFloatingInteger("{=9dP3VJpOG}Experience gain rate", 0f, 1f, "#0.0%", Order = 0, RequireRestart = false, HintText = "{=gGEXaGsKV}Experience gain rate in the Arena Practice fights. Native = 6%. Default = 16.5%.")]
        [SettingPropertyGroup(HeadingPracticeExperience, GroupOrder = 1)]
        public float PracticeExperienceRate { get; set; } = 0.165f;

        //Expansive Arena Practice settings
        [SettingPropertyBool("{=}Enable playing as a companion hero", Order = 0, RequireRestart = false, HintText = "{=}When this option is enabled, you will be allowed to send a companion to participate in the Expansive Arena Practice in your place and control that companion in the arena. Native is False. Default is True.")]
        [SettingPropertyGroup(HeadingPracticeExperience, GroupOrder = 11)]
        public bool EnableExpansivePracticeAgentSwitching { get; set; } = true;

        [SettingPropertyInteger("{=wtKB2udZJ}Total participants", 10, 300, Order = 10, RequireRestart = false, HintText = "{=65Ny4vNEE}The total number of participants in the Expansive Arena Practice. It is recommended to be a multiple of 3. Default = 90.")]
        [SettingPropertyGroup(HeadingExpansivePractice, GroupOrder = 11)]
        public int ExpansivePracticeTotalParticipants { get; set; } = 90;

        [SettingPropertyInteger("{=gaGpV2GcX}Active participants", 1, 30, Order = 11, RequireRestart = false, HintText = "{=sIQdRBwg9}Еstimated number of the active participants in the Expansive Arena Practice. It is recommended to be a multiple of 3. Default = 9.")]
        [SettingPropertyGroup(HeadingExpansivePractice, GroupOrder = 11)]
        public int ExpansivePracticeActiveParticipants { get; set; } = 9;

        [SettingPropertyInteger("{=0IOg4Ureb}Active participants minimum", 1, 20, Order = 12, RequireRestart = false, HintText = "{=ZaLfvG7eX}The minimum number of active participants in the Expansive Arena Practice. It is recommended to be a multiple of 2. Default = 4.")]
        [SettingPropertyGroup(HeadingExpansivePractice, GroupOrder = 11)]
        public int ExpansivePracticeActiveParticipantsMinimum { get; set; } = 4;

        [SettingPropertyInteger("{=dAhEzcAlx}Initial participants", 1, 15, Order = 13, RequireRestart = false, HintText = "{=XwzmSDpAr}Initial number of the active participants in the Expansive Arena Practice. Any number greater than 7 will result in multiple fighters spawning together. Default = 7.")]
        [SettingPropertyGroup(HeadingExpansivePractice, GroupOrder = 11)]
        public int ExpansivePracticeInitialParticipants { get; set; } = 7;

        [SettingPropertyInteger("{=}AI teams", 0, 30, Order = 14, RequireRestart = false, HintText = "{=}Number of AI teams in the Expansive Arena Practice match. Zero means it will be equal to the number of active participants, which minimizes the chances of encountering AI teammates working together. Setting it to 1 means that everyone will fight you. Default = 0.")]
        [SettingPropertyGroup(HeadingExpansivePractice, GroupOrder = 11)]
        public int ExpansivePracticeAITeamsCount { get; set; } = 0;

        [SettingPropertyBool("{=BVhryIKKF}Enable loadout choice", Order = 20, RequireRestart = false, HintText = "{=9otxYlTUa}When this option is enabled, you are alowed to choose weapons for the Expansive Arena Practice.")]
        [SettingPropertyGroup(HeadingExpansivePractice, GroupOrder = 11)]
        public bool ExpansivePracticeEnableLoadoutChoice { get; set; } = true;

        [SettingPropertyInteger("{=L73kYZTnf}Price for picking weapons", 0, 100, Order = 21, RequireRestart = false, HintText = "{=B9xXCFneS}You will have to pay this sum per a wepon stage (tier) when picking a weapon for the Expansive Arena Practice. Default = 10.")]
        [SettingPropertyGroup(HeadingExpansivePractice, GroupOrder = 11)]
        public int ExpansivePracticeLoadoutChoiceCost { get; set; } = 10;

        [SettingPropertyDropdown("{=}Defensive practice equipment", Order = 22, RequireRestart = false, HintText = "{=}Specify what armor or clothing should be worn by Expansive Arena Practice participants. Choosing [Tournament Armor] will give the same result as [Battle Equipment] unless changed by other mods. It's not recommended to change this setting unless you have a specific goal in mind. While it can be fun, wearing armor renders most ranged practice weapons ineffective. Default is [Practice Clothes].")]
        [SettingPropertyGroup(HeadingExpansivePractice, GroupOrder = 11)]
        public Dropdown<DropdownEnumItem<PracticeEquipmentType>> ExpansivePracticeEquipment { get; set; } = new Dropdown<DropdownEnumItem<PracticeEquipmentType>>(DropdownEnumItem<PracticeEquipmentType>.SetDropdownListFromEnum(), 0);

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

        [SettingPropertyDropdown("{=rQHQBnmZL}Champion prize calculation", Order = 5, RequireRestart = false, HintText = "{=aUEgf6bh5}Specify how last man standing in the Expansive Arena Practice should be rewarded. Standard - with a special prize. Additive - with a special prize and any valor reward earned. Multiplicative - with a special prize for each valor class earned above class I. Default is [Additive].")]
        [SettingPropertyGroup(HeadingExpansivePracticeRewards, GroupOrder = 0)]
        public Dropdown<string> ExpansivePracticeChampionPrizeCalculation { get; set; } = new Dropdown<string>(
        [
            DropdownValueStandard,
            DropdownValueAdditive,
            DropdownValueMultiplicative
        ], 1);

        [SettingPropertyInteger("{=oTOFfuU2c}Champion prize", 0, 2500, Order = 6, RequireRestart = false, HintText = "{=FP34ZZ1b0}Base prize for being the last man standing in the Expansive Arena Practice. Default = 500.")]
        [SettingPropertyGroup(HeadingExpansivePracticeRewards, GroupOrder = 0)]
        public int ExpansivePracticeChampionReward { get; set; } = 500;

        [SettingPropertyBool("{=zDfS2JtnA}Enable experience gain for viewers", Order = 0, RequireRestart = false, HintText = "{=YyniNTgz5}Only heroes and troops of rank 3 and up are allowed to enter Expansive Arena Practice. When this option is enabled, lower-ranking troops will gain some experience when their comrades successfully land hits and make takedowns in the arena.")]
        [SettingPropertyGroup(HeadingExpansivePracticeExperience, GroupOrder = 1)]
        public bool EnableViewerExperienceGain { get; set; } = true;

        [SettingPropertyFloatingInteger("{=9dP3VJpOG}Experience gain rate", 0f, 1f, "#0.0%", Order = 1, RequireRestart = false, HintText = "{=hTHzr3Q9U}Experience gain rate in the Expansive Arena Practice fights. Native = 6%. Default = 33.3%.")]
        [SettingPropertyGroup(HeadingExpansivePracticeExperience, GroupOrder = 1)]
        public float ExpansivePracticeExperienceRate { get; set; } = 0.333f;

        //Parry Practice settings
        [SettingPropertyBool("{=}Enable parry practice", Order = 0, RequireRestart = false, HintText = "{=}When this option is enabled, Parry Practice mode is enabled in the Arena. Native is False. Default is True.")]
        [SettingPropertyGroup(HeadingParryPractice, GroupOrder = 12)]
        public bool EnableParryPractice { get; set; } = true;

        [SettingPropertyInteger("{=}Parry practice setup cost", 0, 1000, Order = 1, RequireRestart = false, HintText = "{=}The cost Arena Master charges for organizing a Parry Practice match. Default = 200.")]
        [SettingPropertyGroup(HeadingParryPractice, GroupOrder = 12)]
        public int ParryPracticeSetupCost { get; set; } = 200;

        [SettingPropertyBool("{=}Enable playing as a companion hero", Order = 2, RequireRestart = false, HintText = "{=}When this option is enabled, you will be allowed to send a companion to practice parrying in your place and control that companion in the arena. Native is False. Default is True.")]
        [SettingPropertyGroup(HeadingParryPractice, GroupOrder = 12)]
        public bool EnableParryPracticeAgentSwitching { get; set; } = true;

        [SettingPropertyInteger("{=wtKB2udZJ}Total participants", 3, 30, Order = 10, RequireRestart = false, HintText = "{=}The total number of participants in the Parry Practice. Default = 15.")]
        [SettingPropertyGroup(HeadingParryPractice, GroupOrder = 12)]
        public int ParryPracticeTotalParticipants { get; set; } = 15;

        [SettingPropertyInteger("{=gaGpV2GcX}Active participants", 1, 5, Order = 11, RequireRestart = false, HintText = "{=}Еstimated number of the active participants in the Parry Practice. Default = 1.")]
        [SettingPropertyGroup(HeadingParryPractice, GroupOrder = 12)]
        public int ParryPracticeActiveParticipants { get; set; } = 1;

        [SettingPropertyInteger("{=0IOg4Ureb}Active participants minimum", 1, 5, Order = 12, RequireRestart = false, HintText = "{=}The minimum number of active participants in the Parry Practice. Default = 1.")]
        [SettingPropertyGroup(HeadingParryPractice, GroupOrder = 12)]
        public int ParryPracticeActiveParticipantsMinimum { get; set; } = 1;

        [SettingPropertyInteger("{=dAhEzcAlx}Initial participants", 1, 5, Order = 13, RequireRestart = false, HintText = "{=}Initial number of the active participants in the Parry Practice. Default = 1.")]
        [SettingPropertyGroup(HeadingParryPractice, GroupOrder = 21)]
        public int ParryPracticeInitialParticipants { get; set; } = 1;

        [SettingPropertyInteger("{=}AI teams", 1, 5, Order = 14, RequireRestart = false, HintText = "{=}Number of AI teams in the Parry Practice match. Zero means it will be equal to the number of active participants, which minimizes the chances of encountering AI teammates working together. Setting it to 1 means that everyone will fight you. Default = 1.")]
        [SettingPropertyGroup(HeadingParryPractice, GroupOrder = 12)]
        public int ParryPracticeAITeamsCount { get; set; } = 1;

        [SettingPropertyBool("{=BVhryIKKF}Enable loadout choice", Order = 20, RequireRestart = false, HintText = "{=}When this option is enabled, you are alowed to choose weapons for the Parry Practice.")]
        [SettingPropertyGroup(HeadingParryPractice, GroupOrder = 12)]
        public bool ParryPracticeEnableLoadoutChoice { get; set; } = true;

        [SettingPropertyInteger("{=L73kYZTnf}Price for picking weapons", 0, 100, Order = 21, RequireRestart = false, HintText = "{=}You will have to pay this sum per a wepon stage (tier) when picking a weapon for the Parry Practice. Default = 10.")]
        [SettingPropertyGroup(HeadingParryPractice, GroupOrder = 12)]
        public int ParryPracticeLoadoutChoiceCost { get; set; } = 10;

        [SettingPropertyDropdown("{=}Defensive practice equipment", Order = 22, RequireRestart = false, HintText = "{=}Specify what armor or clothing should be worn by Parry Practice participants. Choosing [Tournament Armor] will give the same result as [Battle Equipment] unless changed by other mods. Default is [Battle equipment].")]
        [SettingPropertyGroup(HeadingParryPractice, GroupOrder = 12)]
        public Dropdown<DropdownEnumItem<PracticeEquipmentType>> ParryPracticeEquipment { get; set; } = new Dropdown<DropdownEnumItem<PracticeEquipmentType>>(DropdownEnumItem<PracticeEquipmentType>.SetDropdownListFromEnum(), 3);

        [SettingPropertyFloatingInteger("{=9dP3VJpOG}Experience gain rate", 0f, 1f, "#0.0%", Order = 0, RequireRestart = false, HintText = "{=}Experience gain rate in the Parry Practice fights. Native = 6%. Default = 6%.")]
        [SettingPropertyGroup(HeadingParryPracticeExperience, GroupOrder = 0)]
        public float ParryPracticeExperienceRate { get; set; } = 0.06f;

        //Team Arena Practice settings
        [SettingPropertyBool("{=}Enable parry practice", Order = 0, RequireRestart = false, HintText = "{=}When this option is enabled, Team Practice mode is enabled in the Arena. Native is False. Default is True.")]
        [SettingPropertyGroup(HeadingTeamPractice, GroupOrder = 3)]
        public bool EnableTeamPractice { get; set; } = true;

        [SettingPropertyInteger("{=}Parry practice setup cost", 0, 1000, Order = 1, RequireRestart = false, HintText = "{=}The cost Arena Master charges for organizing a Team Practice match. Default = 200.")]
        [SettingPropertyGroup(HeadingTeamPractice, GroupOrder = 13)]
        public int TeamPracticeSetupCost { get; set; } = 200;

        [SettingPropertyBool("{=}Enable playing as a companion hero", Order = 2, RequireRestart = false, HintText = "{=}When this option is enabled, you will be allowed to send a companion to participate in the Team Arena Practice in your place and control that companion in the arena. Native is False. Default is True.")]
        [SettingPropertyGroup(HeadingTeamPractice, GroupOrder = 13)]
        public bool EnableTeamPracticeAgentSwitching { get; set; } = true;

        [SettingPropertyInteger("{=wtKB2udZJ}Total participants", 50, 600, Order = 10, RequireRestart = false, HintText = "{=}The total number of participants in the Team Arena Practice. It is recommended to be a multiple of 3 and AI teams count. Default = 300.")]
        [SettingPropertyGroup(HeadingTeamPractice, GroupOrder = 13)]
        public int TeamPracticeTotalParticipants { get; set; } = 300;

        [SettingPropertyInteger("{=gaGpV2GcX}Active participants", 10, 120, Order = 11, RequireRestart = false, HintText = "{=}Еstimated number of the active participants in the Team Arena Practice. It is recommended to be a multiple of AI teams count. Default = 20.")]
        [SettingPropertyGroup(HeadingTeamPractice, GroupOrder = 13)]
        public int TeamPracticeActiveParticipants { get; set; } = 20;

        [SettingPropertyInteger("{=0IOg4Ureb}Active participants minimum", 5, 90, Order = 12, RequireRestart = false, HintText = "{=}The minimum number of active participants in the Team Arena Practice. It is recommended to be a multiple of 2 and AI teams count. Default = 12.")]
        [SettingPropertyGroup(HeadingTeamPractice, GroupOrder = 13)]
        public int TeamPracticeActiveParticipantsMinimum { get; set; } = 12;

        [SettingPropertyInteger("{=dAhEzcAlx}Initial participants", 5, 90, Order = 13, RequireRestart = false, HintText = "{=}Initial number of the active participants in the Team Arena Practice. It is recommended to be a multiple of AI teams count. Default = 20.")]
        [SettingPropertyGroup(HeadingTeamPractice, GroupOrder = 13)]
        public int TeamPracticeInitialParticipants { get; set; } = 20;

        [SettingPropertyInteger("{=}AI teams", 1, 6, Order = 14, RequireRestart = false, HintText = "{=}Number of AI teams in the Team Arena Practice match. Setting it to 1 means that everyone will fight your team. Default = 4.")]
        [SettingPropertyGroup(HeadingTeamPractice, GroupOrder = 13)]
        public int TeamPracticeAITeamsCount { get; set; } = 4;

        [SettingPropertyBool("{=BVhryIKKF}Enable loadout choice", Order = 20, RequireRestart = false, HintText = "{=}When this option is enabled, you are alowed to choose weapons for the Team Arena Practice.")]
        [SettingPropertyGroup(HeadingTeamPractice, GroupOrder = 13)]
        public bool TeamPracticeEnableLoadoutChoice { get; set; } = true;

        [SettingPropertyInteger("{=L73kYZTnf}Price for picking weapons", 0, 100, Order = 21, RequireRestart = false, HintText = "{=}You will have to pay this sum per a wepon stage (tier) when picking a weapon for the Team Arena Practice. Default = 50.")]
        [SettingPropertyGroup(HeadingTeamPractice, GroupOrder = 13)]
        public int TeamPracticeLoadoutChoiceCost { get; set; } = 50;

        [SettingPropertyDropdown("{=}Defensive practice equipment", Order = 22, RequireRestart = false, HintText = "{=}Specify what armor or clothing should be worn by Team Arena Practice participants. Choosing [Tournament Armor] will give the same result as [Battle Equipment] unless changed by other mods. It's not recommended to change this setting unless you have a specific goal in mind. While it can be fun, wearing armor renders most ranged practice weapons ineffective. Default is [Practice Clothes].")]
        [SettingPropertyGroup(HeadingTeamPractice, GroupOrder = 13)]
        public Dropdown<DropdownEnumItem<PracticeEquipmentType>> TeamPracticeEquipment { get; set; } = new Dropdown<DropdownEnumItem<PracticeEquipmentType>>(DropdownEnumItem<PracticeEquipmentType>.SetDropdownListFromEnum(), 0);

        [SettingPropertyInteger("{=v7NfMok7L}Valor reward class I", 0, 50, Order = 0, RequireRestart = false, HintText = "{=}Prize for your team defeating at least 30 fighters in the Team Arena Practice. Default = 30.")]
        [SettingPropertyGroup(HeadingTeamPracticeRewards, GroupOrder = 0)]
        public int TeamPracticeValorReward1 { get; set; } = 30;

        [SettingPropertyInteger("{=AFzr1BhC4}Valor reward class II", 0, 100, Order = 1, RequireRestart = false, HintText = "{=}Prize for your team defeating at least 50 fighters in the Team Arena Practice. Default = 60.")]
        [SettingPropertyGroup(HeadingTeamPracticeRewards, GroupOrder = 0)]
        public int TeamPracticeValorReward2 { get; set; } = 60;

        [SettingPropertyInteger("{=wjlHVn6cG}Valor reward class III", 0, 250, Order = 2, RequireRestart = false, HintText = "{=}Prize for your team defeating at least 75 fighters in the Team Arena Practice. Default = 150.")]
        [SettingPropertyGroup(HeadingTeamPracticeRewards, GroupOrder = 0)]
        public int TeamPracticeValorReward3 { get; set; } = 150;

        [SettingPropertyInteger("{=1JAQ3hGAk}Valor reward class IV", 0, 600, Order = 3, RequireRestart = false, HintText = "{=}Prize for your team defeating at least 100 fighters in the Team Arena Practice. Default = 350.")]
        [SettingPropertyGroup(HeadingTeamPracticeRewards, GroupOrder = 0)]
        public int TeamPracticeValorReward4 { get; set; } = 350;

        [SettingPropertyInteger("{=MHzw7JXgh}Valor reward class V", 0, 1500, Order = 4, RequireRestart = false, HintText = "{=}Prize for your team defeating at least 135 fighters in the Team Arena Practice. Default = 800.")]
        [SettingPropertyGroup(HeadingTeamPracticeRewards, GroupOrder = 0)]
        public int TeamPracticeValorReward5 { get; set; } = 800;

        [SettingPropertyDropdown("{=rQHQBnmZL}Champion prize calculation", Order = 5, RequireRestart = false, HintText = "{=}Specify how last team standing in the Team Arena Practice should be rewarded. Standard - with a special prize. Additive - with a special prize and any valor reward earned. Multiplicative - with a special prize for each valor class earned above class I. Default is [Additive].")]
        [SettingPropertyGroup(HeadingTeamPracticeRewards, GroupOrder = 0)]
        public Dropdown<string> TeamPracticeChampionPrizeCalculation { get; set; } = new Dropdown<string>(
        [
            DropdownValueStandard,
            DropdownValueAdditive,
            DropdownValueMultiplicative
        ], 1);

        [SettingPropertyInteger("{=oTOFfuU2c}Champion prize", 0, 2500, Order = 6, RequireRestart = false, HintText = "{=}Base prize for being the last team standing in the Team Arena Practice. Default = 1000.")]
        [SettingPropertyGroup(HeadingTeamPracticeRewards, GroupOrder = 0)]
        public int TeamPracticeChampionReward { get; set; } = 1000;

        [SettingPropertyBool("{=zDfS2JtnA}Enable experience gain for viewers", Order = 0, RequireRestart = false, HintText = "{=}Only heroes and troops of rank 3 and up are allowed to enter Team Arena Practice. When this option is enabled, lower-ranking troops will gain some experience when their comrades successfully land hits and make takedowns in the arena.")]
        [SettingPropertyGroup(HeadingTeamPracticeExperience, GroupOrder = 1)]
        public bool TeamEnableViewerExperienceGain { get; set; } = true;

        [SettingPropertyFloatingInteger("{=9dP3VJpOG}Experience gain rate", 0f, 1f, "#0.0%", Order = 1, RequireRestart = false, HintText = "{=}Experience gain rate in the Team Arena Practice fights. Native = 6%. Default = 33.3%.")]
        [SettingPropertyGroup(HeadingTeamPracticeExperience, GroupOrder = 1)]
        public float TeamPracticeExperienceRate { get; set; } = 0.333f;

        [SettingPropertyInteger("{=}AI Team 1 Color Index", 0, 157, Order = 10, RequireRestart = false, HintText = "{=3r3TStZPU}Set Team's banner color by index value. Check https://bannerlord.party/banner-colors/ for the list of available colors.")]
        [SettingPropertyGroup(HeadingTeamPracticeTechnicalSettings, GroupOrder = 10)]
        public int TeamPracticeTeamOneColor { get; set; } = 83;

        [SettingPropertyInteger("{=}AI Team 2 Color Index", 0, 157, Order = 11, RequireRestart = false, HintText = "{=3r3TStZPU}Set Team's banner color by index value. Check https://bannerlord.party/banner-colors/ for the list of available colors.")]
        [SettingPropertyGroup(HeadingTeamPracticeTechnicalSettings, GroupOrder = 10)]
        public int TeamPracticeTeamTwoColor { get; set; } = 119;

        [SettingPropertyInteger("{=}AI Team 3 Color Index", 0, 157, Order = 12, RequireRestart = false, HintText = "{=3r3TStZPU}Set Team's banner color by index value. Check https://bannerlord.party/banner-colors/ for the list of available colors.")]
        [SettingPropertyGroup(HeadingTeamPracticeTechnicalSettings, GroupOrder = 10)]
        public int TeamPracticeTeamThreeColor { get; set; } = 88;

        [SettingPropertyInteger("{=}AI Team 4 Color Index", 0, 157, Order = 13, RequireRestart = false, HintText = "{=3r3TStZPU}Set Team's banner color by index value. Check https://bannerlord.party/banner-colors/ for the list of available colors.")]
        [SettingPropertyGroup(HeadingTeamPracticeTechnicalSettings, GroupOrder = 10)]
        public int TeamPracticeTeamFourColor { get; set; } = 84;

        [SettingPropertyInteger("{=}AI Team 5 Color Index", 0, 157, Order = 14, RequireRestart = false, HintText = "{=3r3TStZPU}Set Team's banner color by index value. Check https://bannerlord.party/banner-colors/ for the list of available colors.")]
        [SettingPropertyGroup(HeadingTeamPracticeTechnicalSettings, GroupOrder = 10)]
        public int TeamPracticeTeamFiveColor { get; set; } = 82;

        [SettingPropertyInteger("{=}AI Team 6 Color Index", 0, 157, Order = 15, RequireRestart = false, HintText = "{=3r3TStZPU}Set Team's banner color by index value. Check https://bannerlord.party/banner-colors/ for the list of available colors.")]
        [SettingPropertyGroup(HeadingTeamPracticeTechnicalSettings, GroupOrder = 10)]
        public int TeamPracticeTeamSixColor { get; set; } = 35;

        //Tournament settings
        [SettingPropertyInteger("{=hS7XbrLGG}Maximum bet", 100, 1500, Order = 0, RequireRestart = false, HintText = "{=gQvJ8Vvsk}The maximum amount of gold that is usually allowed to be wagered in each round of a tournament. Native = 150. Default = 500.")]
        [SettingPropertyGroup(HeadingTournaments, GroupOrder = 20)]
        public int TournamentMaximumBet { get; set; } = 500;

        [SettingPropertyBool("{=qPAga7DLd}Enable randomized betting odds", Order = 1, RequireRestart = false, HintText = "{=kGyR7NsKh}When this option is enabled, bet odds are slightly randomized, but still mostly based on the prediction of the player's success. Native is False. Default is True.")]
        [SettingPropertyGroup(HeadingTournaments, GroupOrder = 20)]
        public bool EnableRandomizedBettingOdds { get; set; } = true;

        [SettingPropertyBool("{=hARcKe6Jt}Allow notables participation", Order = 2, RequireRestart = false, HintText = "{=YqIOYTV0P}Allow settlement notables to participate in Tournaments. Recommended with tournament armor mods. Native is False. Default is True.")]
        [SettingPropertyGroup(HeadingTournaments, GroupOrder = 20)]
        public bool AllowNotablesParticipation { get; set; } = true;

        [SettingPropertyDropdown("{=cCAOeRdmt}Prize reroll condition", Order = 3, RequireRestart = false, HintText = "{=CDGmDeYii}Specify when and if tournament prizes should be rerolled. Normally prizes are rerolled when player joins the tournament - if the nubmber of participating nobles changed since the tournament was created (affects prize quality). Native is [When situation changed]. Default is [When prize tier can be improved].")]
        [SettingPropertyGroup(HeadingTournaments, GroupOrder = 20)]
        public Dropdown<string> TournamentPrizeRerollCondition { get; set; } = new Dropdown<string>(
        [
            DropdownValueNever,
            DropdownValueOnPrizeTierImprovement,
            DropdownValueOnImprovement,
            DropdownValueOnChange
        ], 1);

        [SettingPropertyBool("{=IfsaDqygk}Enable gold prizes", Order = 0, RequireRestart = false, HintText = "{=yVAG4f83d}When this option is enabled, there are also gold prizes in tournaments. This is in addition to the usual prize items and bet wins. Native is False. Default is True.")]
        [SettingPropertyGroup(HeadingTournamentsMaterialRewards, GroupOrder = 0)]
        public bool EnableTournamentGoldPrizes { get; set; } = true;

        [SettingPropertyBool("{=fH4Ev0UoC}Enable prize scaling", Order = 10, RequireRestart = false, HintText = "{=1THMacQwa}When this option is enabled, tournament prizes are scaling with player's renown. Slightly unconventional, but helps keep tournaments useful in the mid and late game. Native is False. Default is True.")]
        [SettingPropertyGroup(HeadingTournamentsMaterialRewards, GroupOrder = 0)]
        public bool EnableTournamentPrizeScaling { get; set; } = true;

        [SettingPropertyBool("{=}Enable high quality prizes", Order = 11, RequireRestart = false, HintText = "{=}When this option is enabled, high quality versions of items (masterwork, lordly, etc) can be awarded as tournament prizes. Native is False. Default is True.")]
        [SettingPropertyGroup(HeadingTournamentsMaterialRewards, GroupOrder = 0)]
        public bool EnableHighQualityPrizes { get; set; } = true;

        [SettingPropertyDropdown("{=}Culture restricted elite prizes", Order = 20, RequireRestart = false, HintText = "{=}Specify when and whether elite tournament prizes should be limited to match the culture of the hosting settlement in a same manner as basic prizes are. Native is [Never]. Default is [Always, except for unique mounts].")]
        [SettingPropertyGroup(HeadingTournamentsMaterialRewards, GroupOrder = 0)]
        public Dropdown<string> CultureRestrictedTournamentPrizes { get; set; } = new Dropdown<string>(
        [
            DropdownValueNever,
            DropdownValueStandardItemsOnly,
            DropdownValueAlwaysExceptForMounts,
            DropdownValueAlways
        ], 2);

        [SettingPropertyInteger("{=DyNe9qH4v}Reward for the won round", 0, 1000, Order = 30, RequireRestart = false, HintText = "{=aAquVsgYJ}The amount of gold that participants receive for each round of the tournament they won. Native = 0. Default = 0.")]
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
        [SettingPropertyGroup(HeadingTournamentsTeamGame, GroupOrder = 10)]
        public bool EnableTeamTournaments { get; set; } = true;

        [SettingPropertyInteger("{=lDuOZkktY}Minimum team size", 2, 16, RequireRestart = false, HintText = "{=Nv4MJ3oXq}Minimum number of tournament team members. A team must consist of at least two participants.", Order = 5)]
        [SettingPropertyGroup(HeadingTournamentsTeamGame, GroupOrder = 10)]
        public int TeamSizeMin { get; set; } = 2;

        [SettingPropertyInteger("{=Q6qQge312}Maximum team size", 2, 16, RequireRestart = false, HintText = "{=4q3hSTZPZ}Maximum number of tournament team members. A team must consist of at least two participants. This setting takes precedence over Minimum Team Size in case of conflicting values.", Order = 6)]
        [SettingPropertyGroup(HeadingTournamentsTeamGame, GroupOrder = 10)]
        public int TeamSizeMax { get; set; } = 8;

        [SettingPropertyDropdown("{=6o45aTuZi}Maximum number of teams", RequireRestart = false, HintText = "{=34r9jsdJq}The maximum number of teams in a tournament. A tournament has to have at least 8 teams.", Order = 10)]
        [SettingPropertyGroup(HeadingTournamentsTeamGame, GroupOrder = 10)]
        public Dropdown<int> TeamsCountMax { get; set; } = new Dropdown<int>(
        [
            8,
            16,
            32
        ], 1);

        [SettingPropertyBool("{=rzIC6zMIv}Scores for the winning team", RequireRestart = false, HintText = "{=lA71kgTie}When enabled, every hero in the winning team will get a Tournament Leaderboard score. When disabled, only a team leader will get a score.", Order = 15)]
        [SettingPropertyGroup(HeadingTournamentsTeamGame, GroupOrder = 10)]
        public bool ScoresForWinningTeam { get; set; } = true;

        [SettingPropertyInteger("{=l2FTrzzXX}Team 1 Color Index", 0, 157, HintText = "{=3r3TStZPU}Set Team's banner color by index value. Check https://bannerlord.party/banner-colors/ for the list of available colors.", Order = 25)]
        [SettingPropertyGroup(HeadingTournamentsTeamGame, GroupOrder = 10)]
        public int TeamOneColor { get; set; } = 83;

        [SettingPropertyInteger("{=jWYXBiEZp}Team 2 Color Index", 0, 157, HintText = "{=3r3TStZPU}Set Team's banner color by index value. Check https://bannerlord.party/banner-colors/ for the list of available colors.", Order = 26)]
        [SettingPropertyGroup(HeadingTournamentsTeamGame, GroupOrder = 10)]
        public int TeamTwoColor { get; set; } = 119;

        [SettingPropertyInteger("{=iRvUExaRI}Team 3 Color Index", 0, 157, HintText = "{=3r3TStZPU}Set Team's banner color by index value. Check https://bannerlord.party/banner-colors/ for the list of available colors.", Order = 27)]
        [SettingPropertyGroup(HeadingTournamentsTeamGame, GroupOrder = 10)]
        public int TeamThreeColor { get; set; } = 88;

        [SettingPropertyInteger("{=F6qBgeAOk}Team 4 Color Index", 0, 157, HintText = "{=3r3TStZPU}Set Team's banner color by index value. Check https://bannerlord.party/banner-colors/ for the list of available colors.", Order = 28)]
        [SettingPropertyGroup(HeadingTournamentsTeamGame, GroupOrder = 10)]
        public int TeamFourColor { get; set; } = 84;

        //Compatibility settings
        [SettingPropertyInteger("{=Ndc21b2NO}Practice loadout stages", 0, 10, Order = 0, RequireRestart = false, HintText = "{=9SY3aAtv1}The amount of practice loadout stages that are searched for in ObjectManager. Do not alter this setting unless you have mods that change the stages of arena practice and add corresponding character models. Native = 3. Default = 3.")]
        [SettingPropertyGroup(HeadingCompatibility, GroupOrder = 100)]
        public int PracticeLoadoutStages { get; set; } = 3;

        //Logging settings
        [SettingPropertyBool("{=}Log technical Transpiler info", RequireRestart = false, HintText = "{=}When enabled, logs a lot of technical data about failed Harmony Transpiler calls.", Order = 15)]
        [SettingPropertyGroup(HeadingLogging, GroupOrder = 101)]
        public bool LogTechnicalTranspilerInfo { get; set; } = false;

        public override IEnumerable<ISettingsPreset> GetBuiltInPresets()
        {
            // include all the presets that MCM provides
            foreach (var preset in base.GetBuiltInPresets())
            {
                yield return preset;
            }

            yield return new MemorySettingsPreset(Id, "saving_money", PresetSavingMoney.ToLocalizedString(), () => new Settings()
            {
                //Default Arena Practice settings for Companions
                EnableLoadoutChoice = false,
            });

            yield return new MemorySettingsPreset(Id, "better_training", PresetBetterTraining.ToLocalizedString(), () => new Settings()
            {
                //Default Arena Practice settings for Companions
                EnableLoadoutChoice = true,
                OnlyPriorityLoadouts = false,
                PrioritizeExpensiveEquipment = true
            });
        }
    }
}