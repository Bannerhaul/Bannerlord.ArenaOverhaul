using ArenaOverhaul.Helpers;

using Bannerlord.ButterLib.Common.Helpers;

using SandBox;
using SandBox.Source.Towns;

using System;
using System.Collections.Generic;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.SandBox.Source.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

using static TaleWorlds.Core.ItemObject;

namespace ArenaOverhaul.CampaignBehaviors
{
    public class AOArenaBehavior : CampaignBehaviorBase
    {
        private bool _inExpansivePractice;
        private bool _enteredPracticeFightFromMenu;
        private ArenaMaster? _arenaMasterBehavior;

        private int _tournamentListOffset;
        private int _tournamentListTotalCount;
        private const int _tournamentListEntriesPerPage = 4;

        private CampaignGameStarter? _campaignGame;
        private readonly List<CultureObject> _visitedCultures = new();
        private int _chosenLoadout = -1;
        private int _currentLoadoutStage = 1;

        public bool InExpansivePractice { get => _inExpansivePractice; internal set => _inExpansivePractice = value; }
        public int ChosenLoadout { get => _chosenLoadout; internal set => _chosenLoadout = value; }
        public int ChosenLoadoutStage { get => _currentLoadoutStage; internal set => _currentLoadoutStage = value; }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.AfterMissionStarted.AddNonSerializedListener(this, AfterMissionStarted);
            CampaignEvents.TournamentFinished.AddNonSerializedListener(this, OnTournamentFinished);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.ClanTierIncrease.AddNonSerializedListener(this, OnClanTierChanged);
        }

        public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            _arenaMasterBehavior = Campaign.Current.CampaignBehaviorManager.GetBehavior<ArenaMaster>();
            _inExpansivePractice = false;
            _campaignGame = campaignGameStarter;
            ResetWeaponChoice();

            AddDialogs(campaignGameStarter);
            AddGameMenus(campaignGameStarter);
            TournamentRewardManager.Initialize();

            if (Settlement.CurrentSettlement != null)
            {
                AddLoadoutDialogues(_campaignGame, Settlement.CurrentSettlement);
            }
        }

        private void AddGameMenus(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenuOption("town_arena", "town_arena_enter_expansive_practice_fight", "{=a3uuVmMKR}Expansive practice fight", new GameMenuOption.OnConditionDelegate(game_menu_enter_expansive_practice_fight_on_condition), new GameMenuOption.OnConsequenceDelegate(game_menu_enter_expansive_practice_fight_on_consequence), false, 1, false);
            campaignGameStarter.AddGameMenuOption("town_arena", "town_arena_nearby_tournaments", "{=aiDNBFQ4U}Nearby Tournaments", args => { _tournamentListOffset = 0; args.optionLeaveType = GameMenuOption.LeaveType.Submenu; return true; }, x => GameMenu.SwitchToMenu("nearby_tournaments_list"), false, 2, false);

#if e164 || e165
            campaignGameStarter.AddGameMenu("nearby_tournaments_list", "{=!}{MENU_TEXT}", new OnInitDelegate(game_menu_nearby_tournaments_list_on_init), GameOverlays.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.none, null);
#else
            campaignGameStarter.AddGameMenu("nearby_tournaments_list", "{=!}{MENU_TEXT}", new OnInitDelegate(game_menu_nearby_tournaments_list_on_init), GameOverlays.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.None, null);
#endif
            campaignGameStarter.AddGameMenuOption("nearby_tournaments_list", "nearby_tournaments_list_nextpage", "{=uBC62Jdh1}Next page...", args => { args.optionLeaveType = GameMenuOption.LeaveType.Continue; return _tournamentListOffset * _tournamentListEntriesPerPage + _tournamentListEntriesPerPage < _tournamentListTotalCount; }, x => { ++_tournamentListOffset; GameMenu.SwitchToMenu("nearby_tournaments_list"); }, false, 30, false);
            campaignGameStarter.AddGameMenuOption("nearby_tournaments_list", "nearby_tournaments_list_previouspage", "{=De0boqLm0}Previous page...", args => { args.optionLeaveType = GameMenuOption.LeaveType.LeaveTroopsAndFlee; return _tournamentListOffset > 0; }, x => { --_tournamentListOffset; GameMenu.SwitchToMenu("nearby_tournaments_list"); }, false, 20, false);
            campaignGameStarter.AddGameMenuOption("nearby_tournaments_list", "nearby_tournaments_list_leave", "{=fakGolQMf}Back to arena", args => { args.optionLeaveType = GameMenuOption.LeaveType.Leave; return true; }, x => GameMenu.SwitchToMenu("town_arena"), true, 10, false);
        }

        protected void AddDialogs(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddDialogLine("arena_training_expansive_practice_fight_intro_1a", "arena_expansive_practice_fight_rules", "arena_intro_4", "{=ForHVzTkJ}Also, from time to time, so-called extensive practice fights are held. Basically, it's the same thing, but longer and much more intense. I would advise you to bring some of your men with you, it might be good practice for them. Just do not forget - no teams in there, otherwise you will be disqualified.[ib:closed][if:convo_bared_teeth]", null, null, 100, null);
            campaignGameStarter.AddPlayerLine("arena_training_expansive_practice_fight_intro_3", "arena_prizes_amounts", "arena_expansive_practice_fight_reward", "{=nZVbLJnVn}And what about these expansive practice fights, are they rewarded too? How much are the prizes?", null, null, 95, null, null);
            campaignGameStarter.AddDialogLine("arena_training_expansive_practice_fight_intro_reward", "arena_expansive_practice_fight_reward", "arena_joining_ask", "{=!}{ARENA_REWARD}", new ConversationSentence.OnConditionDelegate(conversation_arena_expansive_practice_fight_explain_reward_on_condition), null, 100, null);

            campaignGameStarter.AddPlayerLine("arena_master_practice_choose_weapon", "arena_master_enter_practice_fight_confirm", "arena_master_practice_choose_weapon_request", "{=MqTDJG0uG}I'd like to choose my gear.", new ConversationSentence.OnConditionDelegate(conversation_arena_weapon_choice_allowed_on_condition), null, 200, null, null);
            campaignGameStarter.AddDialogLine("arena_master_practice_choose_weapon_master_confirm", "arena_master_practice_choose_weapon_request", "arena_master_practice_weapons_list", "{=iDGR9F0Gn}Alright{?WEAPON_CHOICE_HAS_PRICE}, but it will cost you {WEAPON_CHOICE_PRICE}{GOLD_ICON}{?}{\\?}! Which weapon set are you taking?[ib:closed][if:convo_bared_teeth]", new ConversationSentence.OnConditionDelegate(conversation_town_arena_weapon_choice_request_confirm_on_condition), null, 100, null);
            campaignGameStarter.AddPlayerLine("arena_master_practice_choose_better_weapon", "arena_master_practice_weapons_list", "arena_master_practice_choose_weapon_request", "{=DGsGeb4LA}I'd like to get better gear.", new ConversationSentence.OnConditionDelegate(conversation_town_arena_weapons_list_on_condition), new ConversationSentence.OnConsequenceDelegate(conversation_town_arena_weapons_list_get_better_on_consequence), 20, null, null);
            campaignGameStarter.AddPlayerLine("arena_master_practice_choose_weapon_return", "arena_master_practice_weapons_list", "arena_master_enter_practice_fight", "{=lIBwkFipY}Actually, nevermind.", null, new ConversationSentence.OnConsequenceDelegate(ResetWeaponChoice), 10, null, null);

            campaignGameStarter.AddPlayerLine("arena_master_ask_for_expansive_practice_fight_fight", "arena_master_talk", "arena_master_enter_expansive_practice_fight", "{=7f21TSn5W}I'd like to participate in an expansive practice fight...", null, new ConversationSentence.OnConsequenceDelegate(SetExpansivePracticeMode), 100, new ConversationSentence.OnClickableConditionDelegate(conversation_town_arena_fight_join_check_on_condition), null);
            campaignGameStarter.AddDialogLine("arena_master_enter_expansive_practice_fight_master_confirm", "arena_master_enter_expansive_practice_fight", "arena_master_enter_practice_fight_confirm", "{=MnhLtl9Nn}Well, gather your men and go to it! Don't forget to grab a practice weapon on your way down.[if:convo_approving]", new ConversationSentence.OnConditionDelegate(conversation_arena_join_practice_fight_confirm_on_condition), null, 100, null);
            campaignGameStarter.AddDialogLine("arena_master_enter_expansive_practice_fight_master_decline", "arena_master_enter_expansive_practice_fight", "close_window", "{=FguHzavX}You can't practice in the arena because there is a tournament going on right now.", null, null, 100, null);

            campaignGameStarter.AddPlayerLine("arena_master_post_practice_fight_take_default_loadout", "arena_master_post_practice_fight_talk", "close_window", "{=WRO1rFtQm}I'll do that with standard gear.", new ConversationSentence.OnConditionDelegate(conversation_arena_return_to_default_choice_allowed_on_condition), new ConversationSentence.OnConsequenceDelegate(conversation_arena_join_fight_with_default_weapons_on_consequence), 95, null, null);
            campaignGameStarter.AddPlayerLine("arena_master_post_practice_fight_take_new_loadout", "arena_master_post_practice_fight_talk", "arena_master_practice_choose_weapon_request", "{=uLNYivCXl}Sure. Although I'd like to take a new loadout.", new ConversationSentence.OnConditionDelegate(conversation_arena_weapon_choice_allowed_on_condition), new ConversationSentence.OnConsequenceDelegate(ResetWeaponChoice), 90, null, null);
        }
        public void AfterMissionStarted(IMission obj)
        {
            if (!_enteredPracticeFightFromMenu)
            {
                return;
            }
            Mission.Current.SetMissionMode(MissionMode.Battle, true);
            Mission.Current.GetMissionBehavior<ArenaPracticeFightMissionController>().StartPlayerPractice();
            _enteredPracticeFightFromMenu = false;
        }

        public void OnTournamentFinished(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)
        {
            TournamentRewardManager.ResolveTournament(winner, participants, town);
        }

        public void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
        {
            if (mobileParty != MobileParty.MainParty || !settlement.IsTown)
            {
                return;
            }
            ResetWeaponChoice();
            AddLoadoutDialogues(_campaignGame!, settlement);
        }

        public void OnClanTierChanged(Clan clan, bool shouldNotify = true)
        {
            if (clan == Clan.PlayerClan)
            {
                UpdateTournamentPrizes();
            }
        }

        private void UpdateTournamentPrizes()
        {
            var fightTournamentGames = Town.AllTowns.Where(x => Campaign.Current.TournamentManager.GetTournamentGame(x) is FightTournamentGame).Select(x => (FightTournamentGame) Campaign.Current.TournamentManager.GetTournamentGame(x)).ToList();
            foreach (FightTournamentGame fightTournamentGame in fightTournamentGames)
            {
                FieldAccessHelper.FTGPossibleRegularRewardItemObjectsCacheByRef(fightTournamentGame)?.Clear();
                FieldAccessHelper.FTGPossibleEliteRewardItemObjectsCacheByRef(fightTournamentGame)?.Clear();
                if (Settlement.CurrentSettlement is null || !Settlement.CurrentSettlement.IsTown || fightTournamentGame.Town != Settlement.CurrentSettlement.Town)
                {
                    fightTournamentGame.UpdateTournamentPrize(true);
                }
            }
        }

        protected void AddLoadoutDialogues(CampaignGameStarter campaignGameStarter, Settlement settlement)
        {
            if (!_visitedCultures.Contains(settlement.MapFaction.Culture))
            {
                for (int practiceStage = 1; practiceStage < 4; practiceStage++)
                {
                    CharacterObject characterObject =
                        Game.Current.ObjectManager.GetObject<CharacterObject>("weapon_practice_stage_" + practiceStage.ToString() + "_" + settlement.MapFaction.Culture.StringId)
                        ?? Game.Current.ObjectManager.GetObject<CharacterObject>("weapon_practice_stage_" + practiceStage.ToString() + "_empire");

                    List<(int EquipmentStage, string Loadout)> listOfExistingLoadouts = new();
                    for (int i = 0; i < characterObject.BattleEquipments.Count(); i++)
                    {
                        string[] dialogueIdArr = new string[4];
                        string[] dialogueTextArr = new string[4];
                        string dialogueID = "arena_practice_stage_" + practiceStage.ToString() + "_loadout_";
                        string dialogueText = "";
                        int loadout = i;
                        int equipmentStage = practiceStage;

                        for (int x = 0; x < 4; x++)
                        {
                            EquipmentElement equipmentFromSlot = characterObject.BattleEquipments.ToList<Equipment>()[i].GetEquipmentFromSlot((EquipmentIndex) x);
                            if (equipmentFromSlot.Item != null)
                            {
                                dialogueIdArr[x] = equipmentFromSlot.Item.StringId;
                                dialogueTextArr[x] = equipmentFromSlot.Item.Name.ToString();
                            }
                        }
                        dialogueID = string.Join("_", dialogueIdArr.Where(s => !string.IsNullOrEmpty(s)));
                        dialogueText = string.Join(", ", dialogueTextArr.Where(s => !string.IsNullOrEmpty(s)));

                        var equipmentEntry = (equipmentStage, dialogueText);
                        if (!listOfExistingLoadouts.Contains(equipmentEntry))
                        {
                            listOfExistingLoadouts.Add(equipmentEntry);
                            campaignGameStarter.AddPlayerLine(dialogueID, "arena_master_practice_weapons_list", "close_window", dialogueText, new ConversationSentence.OnConditionDelegate(() => conversation_town_arena_culture_match_on_condition(settlement.MapFaction.Culture, equipmentStage)), new ConversationSentence.OnConsequenceDelegate(() => conversation_arena_join_fight_with_selected_loadout_on_consequence(loadout)), 100, null, null);
                        }
                    }
                }
                _visitedCultures.Add(settlement.MapFaction.Culture);
            }
        }

#pragma warning disable IDE1006 // Naming Styles
        public bool conversation_arena_weapon_choice_allowed_on_condition() => _inExpansivePractice ? Settings.Instance!.ExpansivePracticeEnableLoadoutChoice : Settings.Instance!.PracticeEnableLoadoutChoice;

        public bool conversation_arena_weapon_choice_forbidden_on_condition() => !(_inExpansivePractice ? Settings.Instance!.ExpansivePracticeEnableLoadoutChoice : Settings.Instance!.PracticeEnableLoadoutChoice);

        private bool conversation_arena_return_to_default_choice_allowed_on_condition() => conversation_arena_weapon_choice_allowed_on_condition() && _chosenLoadout >= 0;

        private bool conversation_town_arena_culture_match_on_condition(CultureObject culture, int stage) => culture.Equals(Settlement.CurrentSettlement.MapFaction.Culture) && stage == _currentLoadoutStage;

        private bool conversation_town_arena_weapon_choice_request_confirm_on_condition()
        {
            int price = GetWeaponLoadoutChoiceCost();
            MBTextManager.SetTextVariable("WEAPON_CHOICE_HAS_PRICE", (price > 0 ? 1 : 0).ToString(), false);
            MBTextManager.SetTextVariable("WEAPON_CHOICE_PRICE", (_currentLoadoutStage * price).ToString(), false);
            return true;
        }

        public int GetWeaponLoadoutChoiceCost()
        {
            return _inExpansivePractice ? Settings.Instance!.ExpansivePracticeLoadoutChoiceCost : Settings.Instance!.PracticeLoadoutChoiceCost;
        }


        private bool conversation_town_arena_weapons_list_on_condition() => _currentLoadoutStage < 3;

        private bool conversation_town_arena_fight_join_check_on_condition(out TextObject? explanation)
        {
            if (Hero.MainHero.IsWounded && Campaign.Current.IsMainHeroDisguised)
            {
                explanation = new TextObject("{=DqZtRBXR}You are wounded and in disguise.", null);
                return false;
            }
            if (Hero.MainHero.IsWounded)
            {
                explanation = new TextObject("{=yNMrF2QF}You are wounded", null);
                return false;
            }
            if (Campaign.Current.IsMainHeroDisguised)
            {
                explanation = new TextObject("{=jcEoUPCB}You are in disguise.", null);
                return false;
            }
            explanation = null;
            return true;
        }

        private static bool conversation_arena_expansive_practice_fight_explain_reward_on_condition()
        {
            PracticePrizeManager.ExplainPracticeReward(true);
            return true;
        }

        private bool conversation_arena_join_practice_fight_confirm_on_condition() => !Settlement.CurrentSettlement.Town.HasTournament;

        private void conversation_arena_join_fight_with_selected_loadout_on_consequence(int loadout)
        {
            _chosenLoadout = loadout;
            conversation_arena_join_fight_with_pre_selected_loadout_on_consequence();
        }

        public void conversation_arena_join_fight_with_pre_selected_loadout_on_consequence()
        {
            PayForWeaponLoadout();
            Campaign.Current.ConversationManager.ConversationEndOneShot += new Action(StartPlayerPracticeAfterConversationEnd);
        }

        private void conversation_town_arena_weapons_list_get_better_on_consequence() => _currentLoadoutStage++;

        public void conversation_arena_join_fight_with_default_weapons_on_consequence()
        {
            ResetWeaponChoice();
            Campaign.Current.ConversationManager.ConversationEndOneShot += new Action(StartPlayerPracticeAfterConversationEnd);
        }

        public void ResetWeaponChoice()
        {
            _chosenLoadout = -1;
            _currentLoadoutStage = 1;
        }

        public void SetStandardPracticeMode()
        {
            _inExpansivePractice = false;
            ResetWeaponChoice();
        }

        public void SetExpansivePracticeMode()
        {
            _inExpansivePractice = true;
            ResetWeaponChoice();
        }

        public void PayForWeaponLoadout()
        {
            if (_chosenLoadout >= 0)
            {
                int price = GetWeaponLoadoutChoiceCost();
                GiveGoldAction.ApplyForCharacterToSettlement(Hero.MainHero, Settlement.CurrentSettlement, _currentLoadoutStage * price);
            }
        }

        private static void StartPlayerPracticeAfterConversationEnd()
        {
            Mission.Current.SetMissionMode(MissionMode.Battle, false);
            Mission.Current.GetMissionBehavior<ArenaPracticeFightMissionController>().StartPlayerPractice();
        }

        private void game_menu_nearby_tournaments_list_on_init(MenuCallbackArgs args)
        {
            List<Town> nearbyTournamentTowns = GetNearbyTournaments();
            List<float> nearbyTownDistances = Town.AllTowns.Where(x => x != Settlement.CurrentSettlement.Town).Select(town => town.Settlement.Position2D.DistanceSquared(Settlement.CurrentSettlement.Position2D)).OrderBy(dist => dist).ToList();
            _tournamentListTotalCount = nearbyTournamentTowns.Count;

            TextObject tournamentList = new("{=zvb5PZ5OA}Tournaments are currently being held in {TOURNAMENTS} {?TOURNAMENTS.PLURAL_FORM}towns{?}town{\\?}.{NEW_LINE}{TORNAMENT_LIST.START}{?TORNAMENT_LIST.IS_PLURAL}{NEW_LINE}{?}{\\?}{TORNAMENT_LIST.END}");
            tournamentList.SetTextVariable("NEW_LINE", Environment.NewLine);
            LocalizationHelper.SetNumericVariable(tournamentList, "TOURNAMENTS", nearbyTournamentTowns.Count);
            LocalizationHelper.SetListVariable(tournamentList, "TORNAMENT_LIST", GetTournamentDescriptions(nearbyTournamentTowns, nearbyTownDistances, _tournamentListOffset, _tournamentListEntriesPerPage), Environment.NewLine);

            MBTextManager.SetTextVariable("MENU_TEXT", tournamentList.ToString(), false);
            return;

            //local methods
            static List<Town> GetNearbyTournaments()
            {
                return Town.AllTowns.Where(x => Campaign.Current.TournamentManager.GetTournamentGame(x) != null && x != Settlement.CurrentSettlement.Town).OrderBy(x => x.Settlement.Position2D.DistanceSquared(Settlement.CurrentSettlement.Position2D)).ToList();
            }

            static string GetDistanceEstimate(Town town, List<float> nearbyTownDistances, out bool isCloseBy)
            {
                int oneFourthIdx = (nearbyTownDistances.Count / 4) - 1;
                int twoFourthIdx = (nearbyTownDistances.Count / 2) - 1;
                int threeFourthIdx = (nearbyTownDistances.Count * 3 / 4) - 1;
                float oneFourthDistance = nearbyTownDistances[oneFourthIdx];
                float twoFourthDistance = nearbyTownDistances[twoFourthIdx];
                float threeFourthDistance = nearbyTownDistances[threeFourthIdx];

                float distanceInQuestion = town.Settlement.Position2D.DistanceSquared(Settlement.CurrentSettlement.Position2D);
                if (distanceInQuestion < twoFourthDistance)
                {
                    isCloseBy = true;
                    return distanceInQuestion < oneFourthDistance ? new TextObject("{=sotK7sewA}nearby").ToString() : new TextObject("{=J5jTiUvt8}reasonably close from here").ToString();
                }
                else
                {
                    isCloseBy = false;
                    return distanceInQuestion < threeFourthDistance ? new TextObject("{=ULKHRKsSV}reasonably far from here").ToString() : new TextObject("{=2JXPNLOm3}very far from here").ToString();
                }
            }

            static List<string> GetTournamentDescriptions(List<Town> nearbyTournamentTowns, List<float> nearbyTownDistances, int offset, int entriesCount)
            {
                List<string> nearbyTournamentDescriptions = new();
                int startEntry = offset * entriesCount;
                for (int i = startEntry; i < nearbyTournamentTowns.Count && i < startEntry + entriesCount; i++)
                {
                    Town town = nearbyTournamentTowns[i];

                    string distanceEstimate = GetDistanceEstimate(town, nearbyTownDistances, out bool isCloseBy);
                    int tournamentAge = (int) Campaign.Current.TournamentManager.GetTournamentGame(town).CreationTime.ElapsedDaysUntilNow;
                    int textVariation = (isCloseBy ? 0 : 3) + (tournamentAge <= 4 ? 0 : tournamentAge > 12 ? 2 : 1);

                    ItemObject prizeItemObject = Campaign.Current.TournamentManager.GetTournamentGame(town).Prize;
                    TextObject prizeTextObject = new("{=UOpsoG57t}tier {TIER} {TYPE}, {NAME}, worth {GOLD}{GOLD_ICON}");
                    prizeTextObject.SetTextVariable("TIER", (int) prizeItemObject.Tier + 1);
                    prizeTextObject.SetTextVariable("TYPE", GetItemTypeName(prizeItemObject.Type));
                    prizeTextObject.SetTextVariable("NAME", prizeItemObject.Name);
                    prizeTextObject.SetTextVariable("GOLD", prizeItemObject.Value);

                    TextObject textObject = GameTexts.FindText("str_menu_nearby_tournaments_list_entry", textVariation.ToString());
                    textObject.SetTextVariable("INDEX", i + 1);
                    textObject.SetTextVariable("TOWN_LINK", town.Settlement.EncyclopediaLinkWithName);
                    textObject.SetTextVariable("DISTANCE_ESTIMATE", distanceEstimate);
                    LocalizationHelper.SetNumericVariable(textObject, "TOURNAMENT_AGE", tournamentAge);
                    textObject.SetTextVariable("ITEM_PRIZE", prizeTextObject);
                    textObject.SetTextVariable("GOLD_PRIZES_ENABLED", Settings.Instance!.EnableTournamentGoldPrizes ? 1 : 0);
                    textObject.SetTextVariable("GOLD_PRIZE", TournamentRewardManager.GetTournamentGoldPrize(town));

                    nearbyTournamentDescriptions.Add(textObject.ToString());
                }
                return nearbyTournamentDescriptions;
            }

            static string GetItemTypeName(ItemTypeEnum itemTypeEnum) =>
                itemTypeEnum switch
                {
                    ItemTypeEnum.Invalid => "{=HxdMLgPrk}invalid item",
                    ItemTypeEnum.Horse => "{=ZMHaMWEhM}horse",
                    ItemTypeEnum.OneHandedWeapon => "{=8G7OMyMbp}one-handed weapon",
                    ItemTypeEnum.TwoHandedWeapon => "{=XuZG0Aoio}two-handed weapon",
                    ItemTypeEnum.Polearm => "{=CqFjTkLpa}polearm",
                    ItemTypeEnum.Arrows => "{=a0BgoRSGt}arrows",
                    ItemTypeEnum.Bolts => "{=cGVcwzcts}bolts",
                    ItemTypeEnum.Shield => "{=1w2htuOn2}shield",
                    ItemTypeEnum.Bow => "{=KsH4vdaKZ}bow",
                    ItemTypeEnum.Crossbow => "{=wXthM7Mmt}crossbow",
                    ItemTypeEnum.Thrown => "{=t3EkeJDz6}thrown weapon",
                    ItemTypeEnum.Goods => "{=FjujPkPsk}goods",
                    ItemTypeEnum.HeadArmor => "{=6p4MDI4FN}head armor",
                    ItemTypeEnum.BodyArmor => "{=JH2tZrJmd}body armor",
                    ItemTypeEnum.LegArmor => "{=aMMn9E0nk}leg armor",
                    ItemTypeEnum.HandArmor => "{=3LtYT36O8}hand armor",
                    ItemTypeEnum.Pistol => "{=OBZsswRvq}pistol",
                    ItemTypeEnum.Musket => "{=mwmowr3Pl}musket",
                    ItemTypeEnum.Bullets => "{=RnZzjAmRe}bullets",
                    ItemTypeEnum.Animal => "{=5PmQkkEOf}animal",
                    ItemTypeEnum.Book => "{=IA8SfaiLs}book",
                    ItemTypeEnum.ChestArmor => "{=zWPK2mgHx}chest armor",
                    ItemTypeEnum.Cape => "{=z3x97kWhJ}cape",
                    ItemTypeEnum.HorseHarness => "{=PxZJzp7zk}horse harness",
                    ItemTypeEnum.Banner => "{=G5Tk9ZJwy}banner",
                    _ => "{=hWj4T9N6e}item"
                };
        }

        private void game_menu_enter_expansive_practice_fight_on_consequence(MenuCallbackArgs args)
        {
            if (!FieldAccessHelper.ArenaMasterHasMetInSettlementsByRef(_arenaMasterBehavior!).Contains(Settlement.CurrentSettlement))
            {
                FieldAccessHelper.ArenaMasterHasMetInSettlementsByRef(_arenaMasterBehavior!).Add(Settlement.CurrentSettlement);
            }
            _inExpansivePractice = true;
            ResetWeaponChoice();
            PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(LocationComplex.Current.GetLocationWithId("arena"), null, null, null);
            _enteredPracticeFightFromMenu = true;
        }

        private bool game_menu_enter_expansive_practice_fight_on_condition(MenuCallbackArgs args)
        {
            Settlement currentSettlement = Settlement.CurrentSettlement;
            args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
            if (!FieldAccessHelper.ArenaMasterKnowTournamentsByRef(_arenaMasterBehavior!))
            {
                args.Tooltip = new TextObject("{=Sph9Nliz}You need to learn more about the arena by talking with the arena master.", null);
                args.IsEnabled = false;
                return true;
            }
            if (Hero.MainHero.IsWounded && Campaign.Current.IsMainHeroDisguised)
            {
                args.Tooltip = new TextObject("{=DqZtRBXR}You are wounded and in disguise.", null);
                args.IsEnabled = false;
                return true;
            }
            if (Hero.MainHero.IsWounded)
            {
                args.Tooltip = new TextObject("{=yNMrF2QF}You are wounded", null);
                args.IsEnabled = false;
                return true;
            }
            if (Campaign.Current.IsMainHeroDisguised)
            {
                args.Tooltip = new TextObject("{=jcEoUPCB}You are in disguise.", null);
                args.IsEnabled = false;
                return true;
            }
            if (!currentSettlement.Town.HasTournament)
            {
                return true;
            }
            args.Tooltip = new TextObject("{=NESB0CVc}There is no practice fight because of the Tournament.", null);
            args.IsEnabled = false;
            return true;
        }
#pragma warning restore IDE1006 // Naming Styles

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}