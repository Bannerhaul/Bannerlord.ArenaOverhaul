﻿using ArenaOverhaul.ArenaPractice;
using ArenaOverhaul.CampaignBehaviors.BehaviorManagers;
using ArenaOverhaul.Extensions;
using ArenaOverhaul.Helpers;
using ArenaOverhaul.ModSettings;
using ArenaOverhaul.TeamTournament;
using ArenaOverhaul.Tournament;

using Bannerlord.ButterLib.Common.Helpers;
using Bannerlord.ButterLib.SaveSystem.Extensions;

using Helpers;

using SandBox.CampaignBehaviors;
using SandBox.Missions.MissionLogics.Arena;
using SandBox.View.Menu;

using System;
using System.Collections.Generic;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

using static TaleWorlds.Core.ItemObject;

using TWHelpers = Helpers;

namespace ArenaOverhaul.CampaignBehaviors
{
    public class AOArenaBehavior : CampaignBehaviorBase
    {
        private ArenaMasterCampaignBehavior? _arenaMasterBehavior;

        private int _tournamentListOffset;
        private int _tournamentListTotalCount;
        private const int _tournamentListEntriesPerPage = 4;

        private CampaignGameStarter? _campaignGame;
        private readonly List<CultureObject> _visitedCultures = [];

        private AOArenaBehaviorManager _AOArenaBehaviorManager = new();

        public AOArenaBehaviorManager BehaviorManager => _AOArenaBehaviorManager!;

        public AOArenaBehavior()
        {
            AOArenaBehaviorManager.Instance = _AOArenaBehaviorManager;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
            CampaignEvents.AfterMissionStarted.AddNonSerializedListener(this, AfterMissionStarted);
            CampaignEvents.TournamentFinished.AddNonSerializedListener(this, OnTournamentFinished);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);

            //UpdateCompanionSettings
            CampaignEvents.HeroComesOfAgeEvent.AddNonSerializedListener(this, OnHeroComesOfAge);
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilledEvent);
            CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, OnCompanionRemoved);
            CampaignEvents.NewCompanionAdded.AddNonSerializedListener(this, OnNewCompanionAdded);
        }

        private void OnAfterSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            AddGameMenus(campaignGameStarter);
        }

        public void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            _arenaMasterBehavior = Campaign.Current.CampaignBehaviorManager.GetBehavior<ArenaMasterCampaignBehavior>();
            _campaignGame = campaignGameStarter;
            _AOArenaBehaviorManager.SetStandardPracticeMode();

            AddDialogs(campaignGameStarter);
            TournamentRewardManager.Initialize();

            if (Settlement.CurrentSettlement != null)
            {
                AddLoadoutDialogues(_campaignGame, Settlement.CurrentSettlement);
            }

            TeamTournamentInfo.Current?.Finish();
        }

        private void AddGameMenus(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddGameMenuOption("town_arena", "town_arena_enter_expansive_practice_fight", "{=a3uuVmMKR}Expansive practice fight", new GameMenuOption.OnConditionDelegate(game_menu_enter_expansive_practice_fight_on_condition), new GameMenuOption.OnConsequenceDelegate(game_menu_enter_expansive_practice_fight_on_consequence), false, 1, false);
            campaignGameStarter.AddGameMenuOption("town_arena", "town_arena_enter_parry_practice_fight", "{=QI6a1mxh9}Parry practice fight", new GameMenuOption.OnConditionDelegate(game_menu_enter_parry_practice_fight_on_condition), new GameMenuOption.OnConsequenceDelegate(game_menu_enter_parry_practice_fight_on_consequence), false, 2, false);
            campaignGameStarter.AddGameMenuOption("town_arena", "town_arena_enter_team_practice_fight", "{=wWZ82hgqV}Team practice fight", new GameMenuOption.OnConditionDelegate(game_menu_enter_team_practice_fight_on_condition), new GameMenuOption.OnConsequenceDelegate(game_menu_enter_team_practice_fight_on_consequence), false, 3, false);

            campaignGameStarter.AddGameMenuOption("town_arena", "town_arena_nearby_tournaments", "{=aiDNBFQ4U}Nearby Tournaments", args => { _tournamentListOffset = 0; args.optionLeaveType = GameMenuOption.LeaveType.Submenu; return true; }, x => GameMenu.SwitchToMenu("nearby_tournaments_list"), false, 4, false);

            campaignGameStarter.AddGameMenu("nearby_tournaments_list", "{=!}{MENU_TEXT}", new OnInitDelegate(game_menu_nearby_tournaments_list_on_init), GameOverlays.MenuOverlayType.SettlementWithBoth, GameMenu.MenuFlags.None, null);

            campaignGameStarter.AddGameMenuOption("nearby_tournaments_list", "nearby_tournaments_list_nextpage", "{=uBC62Jdh1}Next page...", args => { args.optionLeaveType = GameMenuOption.LeaveType.Continue; return _tournamentListOffset * _tournamentListEntriesPerPage + _tournamentListEntriesPerPage < _tournamentListTotalCount; }, x => { ++_tournamentListOffset; GameMenu.SwitchToMenu("nearby_tournaments_list"); }, false, 30, false);
            campaignGameStarter.AddGameMenuOption("nearby_tournaments_list", "nearby_tournaments_list_previouspage", "{=De0boqLm0}Previous page...", args => { args.optionLeaveType = GameMenuOption.LeaveType.LeaveTroopsAndFlee; return _tournamentListOffset > 0; }, x => { --_tournamentListOffset; GameMenu.SwitchToMenu("nearby_tournaments_list"); }, false, 20, false);
            campaignGameStarter.AddGameMenuOption("nearby_tournaments_list", "nearby_tournaments_list_leave", "{=fakGolQMf}Back to arena", args => { args.optionLeaveType = GameMenuOption.LeaveType.Leave; return true; }, x => GameMenu.SwitchToMenu("town_arena"), true, 10, false);

            campaignGameStarter.AddGameMenuOption("menu_town_tournament_join", "participate_as_team", "{=xRkr497KP}Join as a team", new GameMenuOption.OnConditionDelegate(team_game_select_roster_condition), new GameMenuOption.OnConsequenceDelegate(team_game_select_roster_consequence), false, 1, false);

#if DEBUG // only needed when debugging for testing
            campaignGameStarter.AddGameMenuOption("town_arena", "test_add_tournament_game", "Add Tournament", new GameMenuOption.OnConditionDelegate(AddTournamentCondition), new GameMenuOption.OnConsequenceDelegate(AddTournamentConsequence), false, 5, true);
            campaignGameStarter.AddGameMenuOption("town_arena", "test_resolve_tournament_game", "Resolve Tournament", new GameMenuOption.OnConditionDelegate(ResolveTournamentCondition), new GameMenuOption.OnConsequenceDelegate(ResolveTournamentConsequence), false, 6, true);
#endif
        }

        protected void AddDialogs(CampaignGameStarter campaignGameStarter)
        {
            //Expansive practice introduction and rewards
            campaignGameStarter.AddDialogLine("arena_training_expansive_practice_fight_intro_1a_line", "arena_expansive_practice_fight_rules", "arena_intro_4", "{=ForHVzTkJ}Also, from time to time, so-called expansive practice fights are held. Basically, it's the same thing, but longer and much more intense. I would advise you to bring some of your men with you, it might be good practice for them. Just do not forget - no teams in there, otherwise you will be disqualified.[ib:closed][if:convo_bared_teeth]", null, null, 100, null);
            campaignGameStarter.AddPlayerLine("arena_training_expansive_practice_fight_intro_3_line", "arena_prizes_amounts", "arena_expansive_practice_fight_reward", "{=nZVbLJnVn}And what about these expansive practice fights, are they rewarded too? How much are the prizes?", null, null, 95, null, null);
            campaignGameStarter.AddDialogLine("arena_training_expansive_practice_fight_intro_reward_line", "arena_expansive_practice_fight_reward", "arena_joining_ask", "{=!}{ARENA_REWARD}", new ConversationSentence.OnConditionDelegate(conversation_arena_expansive_practice_fight_explain_reward_on_condition), null, 100, null);

            //Companion choice
            campaignGameStarter.AddPlayerLine("arena_master_practice_choose_companion_line", "arena_master_enter_practice_fight_confirm", "arena_master_practice_choose_companion_request", "{=fpPMPCNp5}My men need training more than I do. I think I'll stay here and send someone instead.", new ConversationSentence.OnConditionDelegate(conversation_arena_companion_choice_allowed_on_condition), new ConversationSentence.OnConsequenceDelegate(conversation_arena_companion_choice_request_on_consequence), 200, null, null);
            campaignGameStarter.AddDialogLine("arena_master_practice_choose_companion_master_confirm_line", "arena_master_practice_choose_companion_request", "arena_master_practice_companions_list", "{=7ONiAo9q0}Alright, who will be the lucky one?[ib:closed][if:convo_bared_teeth]", null, null, 100, null);
            campaignGameStarter.AddRepeatablePlayerLine("arena_master_practice_choose_companion_choice_line", "arena_master_practice_companions_list", "arena_master_enter_custom_practice_fight", "{=!}{HERO.NAME}", "{=UNFE1BeG}I am thinking of a different person", "arena_master_practice_choose_companion_request", new ConversationSentence.OnConditionDelegate(conversation_arena_master_practice_choose_companion_choice_repeatable_on_condition), new ConversationSentence.OnConsequenceDelegate(conversation_arena_master_practice_choose_companion_choice_repeatable_on_consequence), 100, null);
            campaignGameStarter.AddPlayerLine("arena_master_practice_choose_companion_return_line", "arena_master_practice_companions_list", "arena_master_enter_practice_fight", "{=lIBwkFipY}Actually, nevermind.", () => !_AOArenaBehaviorManager.IsAfterPracticeTalk, new ConversationSentence.OnConsequenceDelegate(_AOArenaBehaviorManager.ResetCompanionChoice), 10, null, null);
            campaignGameStarter.AddPlayerLine("arena_master_practice_choose_companion_return_line2", "arena_master_practice_companions_list", "arena_master_custom_post_practice_fight_talk", "{=lIBwkFipY}Actually, nevermind.", () => _AOArenaBehaviorManager.IsAfterPracticeTalk, new ConversationSentence.OnConsequenceDelegate(_AOArenaBehaviorManager.ResetCompanionChoice), 10, null, null);

            //Loadout choice
            campaignGameStarter.AddPlayerLine("arena_master_practice_choose_weapon_line", "arena_master_enter_practice_fight_confirm", "arena_master_practice_choose_weapon_request", "{=MqTDJG0uG}I'd like to choose my gear.", new ConversationSentence.OnConditionDelegate(conversation_arena_weapon_choice_allowed_on_condition), null, 200, null, null);
            campaignGameStarter.AddDialogLine("arena_master_practice_choose_weapon_master_confirm_line", "arena_master_practice_choose_weapon_request", "arena_master_practice_weapons_list", "{=iDGR9F0Gn}Alright{?WEAPON_CHOICE_HAS_PRICE}, but it will cost you {WEAPON_CHOICE_PRICE}{GOLD_ICON}{?}{\\?}! Which weapon set are you taking?[ib:closed][if:convo_bared_teeth]", new ConversationSentence.OnConditionDelegate(conversation_town_arena_weapon_choice_request_confirm_on_condition), null, 100, null);
            campaignGameStarter.AddPlayerLine("arena_master_practice_choose_better_weapon_line", "arena_master_practice_weapons_list", "arena_master_practice_choose_weapon_request", "{=DGsGeb4LA}I'd like to get better gear.", new ConversationSentence.OnConditionDelegate(conversation_town_arena_weapons_list_on_condition), new ConversationSentence.OnConsequenceDelegate(conversation_town_arena_weapons_list_get_better_on_consequence), 20, null, null);
            campaignGameStarter.AddPlayerLine("arena_master_practice_choose_weapon_return_line1", "arena_master_practice_weapons_list", "arena_master_enter_practice_fight", "{=lIBwkFipY}Actually, nevermind.", () => !_AOArenaBehaviorManager.IsAfterPracticeTalk && _AOArenaBehaviorManager.ChosenCharacter is null, new ConversationSentence.OnConsequenceDelegate(_AOArenaBehaviorManager.ResetWeaponChoice), 10, null, null);
            campaignGameStarter.AddPlayerLine("arena_master_practice_choose_weapon_return_line2", "arena_master_practice_weapons_list", "arena_master_enter_custom_practice_fight", "{=lIBwkFipY}Actually, nevermind.", () => !_AOArenaBehaviorManager.IsAfterPracticeTalk && _AOArenaBehaviorManager.ChosenCharacter != null, new ConversationSentence.OnConsequenceDelegate(_AOArenaBehaviorManager.ResetWeaponChoice), 10, null, null);
            campaignGameStarter.AddPlayerLine("arena_master_practice_choose_weapon_return_line3", "arena_master_practice_weapons_list", "arena_master_custom_post_practice_fight_talk", "{=lIBwkFipY}Actually, nevermind.", () => _AOArenaBehaviorManager.IsAfterPracticeTalk, new ConversationSentence.OnConsequenceDelegate(_AOArenaBehaviorManager.ResetWeaponChoice), 10, null, null);

            //Custom pre-fight
            campaignGameStarter.AddDialogLine("arena_master_practice_custom_master_confirm_line", "arena_master_enter_custom_practice_fight", "arena_master_enter_custom_practice_fight_confirm", "{=cB4QnrtWM}Anything else?[ib:normal]", null, null, 100, null);
            campaignGameStarter.AddPlayerLine("arena_master_practice_choose_weapon_line2", "arena_master_enter_custom_practice_fight_confirm", "arena_master_practice_choose_weapon_request", "{=y9zNkuGg0}I'd like to choose the gear for my companion.", new ConversationSentence.OnConditionDelegate(conversation_arena_weapon_choice_allowed_on_condition), null, 200, null, null);
            campaignGameStarter.AddPlayerLine("arena_master_enter_custom_practice_fight_confirm_line", "arena_master_enter_custom_practice_fight_confirm", "close_window", "{=0uFoXskRW}No, let's get the show started.", null, new ConversationSentence.OnConsequenceDelegate(conversation_arena_join_fight_with_default_weapons_on_consequence), 95, new ConversationSentence.OnClickableConditionDelegate(conversation_arena_join_fight_with_default_weapons_on_condition), null);
            campaignGameStarter.AddPlayerLine("arena_master_practice_reset_companion_choice_line", "arena_master_enter_custom_practice_fight_confirm", "arena_master_enter_practice_fight", "{=BbpYVQSc6}I've changed my mind and will fight myself.", null, new ConversationSentence.OnConsequenceDelegate(_AOArenaBehaviorManager.ResetCompanionChoice), 20, null, null);
            campaignGameStarter.AddPlayerLine("arena_master_enter_custom_practice_fight_decline_line", "arena_master_enter_custom_practice_fight_confirm", "arena_master_pre_talk", "{=VVjb1EG4d}Hold off, I have second thoughts about all that.", null, null, 10, null, null);

            //Expansive practice request
            campaignGameStarter.AddPlayerLine("arena_master_ask_for_expansive_practice_fight_fight_line", "arena_master_talk", "arena_master_enter_expansive_practice_fight", "{=7f21TSn5W}I'd like to participate in an expansive practice fight...", null, new ConversationSentence.OnConsequenceDelegate(_AOArenaBehaviorManager.SetExpansivePracticeMode), 100, new ConversationSentence.OnClickableConditionDelegate(conversation_town_arena_fight_join_check_on_condition), null);
            campaignGameStarter.AddDialogLine("arena_master_enter_expansive_practice_fight_master_confirm_line", "arena_master_enter_expansive_practice_fight", "arena_master_enter_practice_fight_confirm", "{=MnhLtl9Nn}Well, gather your men and go to it! Don't forget to grab a practice weapon on your way down.[if:convo_approving]", new ConversationSentence.OnConditionDelegate(conversation_arena_can_join_practice_fight_on_condition), null, 100, null);
            campaignGameStarter.AddDialogLine("arena_master_enter_expansive_practice_fight_master_decline_line", "arena_master_enter_expansive_practice_fight", "close_window", "{=FguHzavX}You can't practice in the arena because there is a tournament going on right now.", null, null, 100, null);

            //Special practice request
            campaignGameStarter.AddPlayerLine("arena_master_ask_for_speacial_practice_fight_line", "arena_master_talk", "arena_master_request_speacial_practice_fight", "{=jBqErWEjU}Can you arrange a special match for me?", new ConversationSentence.OnConditionDelegate(conversation_arena_ask_for_speacial_practice_fight_on_condition), null, 100, new ConversationSentence.OnClickableConditionDelegate(conversation_town_arena_fight_join_check_on_condition), null);
            campaignGameStarter.AddDialogLine("arena_master_request_speacial_practice_fight_master_confirm_line", "arena_master_request_speacial_practice_fight", "arena_master_speacial_practice_fight_list", "{=EYXQnjEvK}It depends. What exactly are you thinking about?[ib:hip][if:convo_thinking]", new ConversationSentence.OnConditionDelegate(conversation_arena_can_join_practice_fight_on_condition), null, 100, null);
            campaignGameStarter.AddDialogLine("arena_master_request_speacial_practice_fight_master_decline_line", "arena_master_request_speacial_practice_fight", "close_window", "{=FguHzavX}You can't practice in the arena because there is a tournament going on right now.", null, null, 100, null);
            campaignGameStarter.AddPlayerLine("arena_master_request_speacial_practice_fight_return_line", "arena_master_speacial_practice_fight_list", "arena_master_pre_talk", "{=lIBwkFipY}Actually, nevermind.", null, new ConversationSentence.OnConsequenceDelegate(_AOArenaBehaviorManager.ResetWeaponChoice), 10, null, null);

            //Parry practice request
            campaignGameStarter.AddPlayerLine("arena_master_ask_for_parry_practice_fight_fight_line", "arena_master_speacial_practice_fight_list", "arena_master_enter_parry_practice_fight", "{=h2Gv6NA8J}I'd like to practice parrying.", new ConversationSentence.OnConditionDelegate(conversation_arena_parry_practice_enabled_on_condition), new ConversationSentence.OnConsequenceDelegate(_AOArenaBehaviorManager.SetParryPracticeMode), 100, new ConversationSentence.OnClickableConditionDelegate(conversation_town_arena_parry_fight_join_check_on_condition), null);
            campaignGameStarter.AddDialogLine("arena_master_enter_parry_practice_fight_master_confirm_line", "arena_master_enter_parry_practice_fight", "arena_master_enter_practice_fight_confirm", "{=9yIQ6Qd1E}This is a really important skill and practicing it will benefit others too... I think I can make this match happen{?PRACTICE_CHOICE_HAS_PRICE}, but it will cost you {PRACTICE_CHOICE_PRICE}{GOLD_ICON}{?}{\\?}. Oh, and don't expect any winnings, since this type of exercise is unlikely to interest the crowd. If you're still up to it, don't forget to grab a practice weapon on your way down.[if:convo_calm_friendly][ib:confident]", new ConversationSentence.OnConditionDelegate(conversation_arena_join_special_practice_fight_confirm_on_condition), null, 100, null);
            campaignGameStarter.AddDialogLine("arena_master_enter_parry_practice_fight_master_decline_line", "arena_master_enter_parry_practice_fight", "close_window", "{=FguHzavX}You can't practice in the arena because there is a tournament going on right now.", null, null, 100, null);

            //Team practice request
            campaignGameStarter.AddPlayerLine("arena_master_ask_for_team_practice_fight_fight_line", "arena_master_speacial_practice_fight_list", "arena_master_enter_team_practice_fight", "{=iXwumAizJ}I want to fight alongside my comrades as a team. Any chane you will host a team practice session?", new ConversationSentence.OnConditionDelegate(conversation_arena_team_practice_enabled_on_condition), new ConversationSentence.OnConsequenceDelegate(_AOArenaBehaviorManager.SetTeamPracticeMode), 100, new ConversationSentence.OnClickableConditionDelegate(conversation_town_arena_fight_join_check_on_condition), null);
            campaignGameStarter.AddDialogLine("arena_master_enter_team_practice_fight_master_confirm_line", "arena_master_enter_team_practice_fight", "arena_master_enter_practice_fight_confirm", "{=LTfWl2OPC}This will definitely shake things up! {?PRACTICE_CHOICE_HAS_PRICE}I like the idea, however, in order to properly organize such a competition, I will have to make a fair amount of fuss, so it will cost you {PRACTICE_CHOICE_PRICE}{GOLD_ICON}. On the bright side, {?}{\\?}I think it will be great entertainment for the crowd, which will have a positive impact on the prizes. If you're still up for it, don't forget to grab a practice weapon on your way down.[ib:nervous2][if:convo_excited]", new ConversationSentence.OnConditionDelegate(conversation_arena_join_special_practice_fight_confirm_on_condition), null, 100, null);
            campaignGameStarter.AddDialogLine("arena_master_enter_team_practice_fight_master_decline_line", "arena_master_enter_team_practice_fight", "close_window", "{=FguHzavX}You can't practice in the arena because there is a tournament going on right now.", null, null, 100, null);

            //Loadout choice post match
            campaignGameStarter.AddPlayerLine("arena_master_practice_choose_companion_line2", "arena_master_post_practice_fight_talk", "arena_master_practice_choose_companion_request", "{=6kW561DVB}I need to rest a little and my men need training. I think I'll stay here this time and send someone instead.", () => conversation_arena_companion_choice_post_fight_allowed_on_condition(false), new ConversationSentence.OnConsequenceDelegate(conversation_arena_companion_choice_request_on_consequence), 95, null, null);
            campaignGameStarter.AddPlayerLine("arena_master_practice_choose_companion_line3", "arena_master_post_practice_fight_talk", "arena_master_practice_choose_companion_request", "{=RKNcniRPk}I want to send someone else this time.", () => conversation_arena_companion_choice_post_fight_allowed_on_condition(true), new ConversationSentence.OnConsequenceDelegate(conversation_arena_companion_choice_request_on_consequence), 95, null, null);
            campaignGameStarter.AddPlayerLine("arena_master_practice_reset_companion_choice_line2", "arena_master_post_practice_fight_talk", "arena_master_custom_post_practice_fight_talk", "{=7NfBn2JvL}I'd like to warm up, this time I will take part in the fight myself.", () => _AOArenaBehaviorManager.ChosenCharacter != null, new ConversationSentence.OnConsequenceDelegate(_AOArenaBehaviorManager.ResetCompanionChoice), 90, null, null);
            campaignGameStarter.AddPlayerLine("arena_master_post_practice_fight_take_default_loadout_line", "arena_master_post_practice_fight_talk", "close_window", "{=!}{DEFAULT_WEAPONS_CHOICE_LINE}", new ConversationSentence.OnConditionDelegate(conversation_arena_return_to_default_choice_allowed_on_condition), new ConversationSentence.OnConsequenceDelegate(conversation_arena_join_fight_with_default_weapons_on_consequence), 85, new ConversationSentence.OnClickableConditionDelegate(conversation_arena_join_fight_with_default_weapons_on_condition), null);
            campaignGameStarter.AddPlayerLine("arena_master_post_practice_fight_take_new_loadout_line", "arena_master_post_practice_fight_talk", "arena_master_practice_choose_weapon_request", "{=!}{NEW_LOADOUT_CHOICE_LINE}", new ConversationSentence.OnConditionDelegate(conversation_arena_make_new_weapon_choice_on_condition), new ConversationSentence.OnConsequenceDelegate(_AOArenaBehaviorManager.ResetWeaponChoice), 80, conversation_arena_join_fight_with_new_weapons_on_condition, null);

            campaignGameStarter.AddDialogLine("arena_master_practice_custom_post_fight_line", "arena_master_custom_post_practice_fight_talk", "arena_master_post_practice_fight_talk", "{=wuuxg3Lkg}So... Do you want to give it another go?[ib:normal]", null, null, 100, null);
        }

        public void AfterMissionStarted(IMission obj)
        {
            if (!_AOArenaBehaviorManager._enteredPracticeFightFromMenu)
            {
                return;
            }

            AOArenaBehaviorManager.Instance!.PayForPracticeMatch();
            Mission.Current.SetMissionMode(_AOArenaBehaviorManager.GetArenaPracticeMissionMode(), true);
            Mission.Current.GetMissionBehavior<ArenaPracticeFightMissionController>().StartPlayerPractice();
            _AOArenaBehaviorManager._enteredPracticeFightFromMenu = false;
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
            _AOArenaBehaviorManager.ResetCompanionChoice();
            _AOArenaBehaviorManager.ResetWeaponChoice();
            AddLoadoutDialogues(_campaignGame!, settlement);
        }

        public void OnHeroComesOfAge(Hero hero)
        {
            if (hero.Clan != null && hero.Clan == Clan.PlayerClan)
            {
                UpdateCompanionSettings();
            }
        }

        public void OnHeroKilledEvent(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
        {
            if (victim.Clan != null && victim.Clan == Clan.PlayerClan)
            {
                UpdateCompanionSettings();
            }
        }

        public void OnNewCompanionAdded(Hero hero)
        {
            UpdateCompanionSettings();
        }

        public void OnCompanionRemoved(Hero hero, RemoveCompanionAction.RemoveCompanionDetail detail)
        {
            UpdateCompanionSettings();
        }

        protected void UpdateCompanionSettings()
        {
            CompanionPracticeSettings.UnregisterCompanionPracticeSettings();
            AOArenaBehaviorManager._companionPracticeSettings ??= [];
            CompanionPracticeSettings.RegisterCompanionPracticeSettings();
        }

        protected void AddLoadoutDialogues(CampaignGameStarter campaignGameStarter, Settlement settlement)
        {
            if (settlement is null || !settlement.IsTown)
            {
                return;
            }

            var settlementCulture = settlement.MapFaction?.Culture ?? settlement.Culture;
            var defaultCulture = Game.Current.ObjectManager.GetObject<CultureObject>("empire") ?? Game.Current.ObjectManager.GetObject<CultureObject>(x => x.IsMainCulture);

            if (!_visitedCultures.Contains(settlementCulture) && settlementCulture != null)
            {
                int practiceLoadoutStages = Settings.Instance!.PracticeLoadoutStages;
                for (int practiceStage = 1; practiceStage <= practiceLoadoutStages; practiceStage++)
                {
                    CharacterObject? characterObject =
                        Game.Current.ObjectManager.GetObject<CharacterObject>("weapon_practice_stage_" + practiceStage.ToString() + "_" + settlementCulture.StringId.ToLower())
                        ?? Game.Current.ObjectManager.GetObject<CharacterObject>("weapon_practice_stage_" + practiceStage.ToString() + "_" + defaultCulture.StringId.ToLower());
                    if (characterObject is null)
                    {
                        continue;
                    }

                    var battleEquipments = characterObject.BattleEquipments.ToList();
                    AddLoadoutDialoguesInternal(campaignGameStarter, settlementCulture, practiceStage, battleEquipments, "arena_practice_", ArenaPracticeMode.ExceptParry);
                    AddLoadoutDialoguesInternal(campaignGameStarter, settlementCulture, practiceStage, AOArenaBehaviorManager.FilterAvailableWeapons(battleEquipments, ArenaPracticeMode.Parry), "arena_parry_practice_", ArenaPracticeMode.Parry);
                }
                _visitedCultures.Add(settlementCulture);
            }
        }

        private void AddLoadoutDialoguesInternal(CampaignGameStarter campaignGameStarter, CultureObject settlementCulture, int practiceStage, List<Equipment> battleEquipments, string dialogueIDPrefix, ArenaPracticeMode practiceMode)
        {
            List<(int EquipmentStage, string Loadout)> listOfExistingLoadouts = [];
            for (int i = 0; i < battleEquipments.Count; i++)
            {
                string[] dialogueIdArr = new string[4];
                string[] dialogueTextArr = new string[4];
                string dialogueID = dialogueIDPrefix + "stage_" + practiceStage.ToString() + "_loadout_";
                string dialogueText = "";
                int loadout = i;
                int equipmentStage = practiceStage;

                for (int x = 0; x < 4; x++)
                {
                    EquipmentElement equipmentFromSlot = battleEquipments[i].GetEquipmentFromSlot((EquipmentIndex) x);
                    if (equipmentFromSlot.Item != null)
                    {
                        dialogueIdArr[x] = equipmentFromSlot.Item.StringId;
                        dialogueTextArr[x] = equipmentFromSlot.Item.Name.ToString();
                    }
                }
                dialogueID = dialogueID + string.Join("_", dialogueIdArr.Where(s => !string.IsNullOrEmpty(s)));
                dialogueText = string.Join(", ", dialogueTextArr.Where(s => !string.IsNullOrEmpty(s)));

                var equipmentEntry = (equipmentStage, dialogueText);
                if (!listOfExistingLoadouts.Contains(equipmentEntry))
                {
                    listOfExistingLoadouts.Add(equipmentEntry);
                    campaignGameStarter.AddPlayerLine(dialogueID, "arena_master_practice_weapons_list", "close_window", dialogueText, new ConversationSentence.OnConditionDelegate(() => conversation_town_arena_culture_match_on_condition(settlementCulture, equipmentStage, practiceMode)), new ConversationSentence.OnConsequenceDelegate(() => conversation_arena_join_fight_with_selected_loadout_on_consequence(loadout)), 100, new ConversationSentence.OnClickableConditionDelegate((out TextObject? explanation) => conversation_town_arena_afford_loadout_choice_on_condition(out explanation, equipmentStage)), null);
                }
            }
        }

#pragma warning disable IDE1006 // Naming Styles

        private bool conversation_arena_ask_for_speacial_practice_fight_on_condition() => Settings.Instance!.EnableParryPractice || Settings.Instance!.EnableTeamPractice;

        private bool conversation_arena_parry_practice_enabled_on_condition() => AOArenaBehaviorManager.IsPracticeModeEnabled(ArenaPracticeMode.Parry);
        private bool conversation_arena_team_practice_enabled_on_condition() => AOArenaBehaviorManager.IsPracticeModeEnabled(ArenaPracticeMode.Team);

        public bool conversation_arena_companion_choice_allowed_on_condition() => _AOArenaBehaviorManager.IsAgentSwitchingAllowed();
        public bool conversation_arena_companion_choice_post_fight_allowed_on_condition(bool isRepickLine) => _AOArenaBehaviorManager.IsAgentSwitchingAllowed() && (isRepickLine ? _AOArenaBehaviorManager.ChosenCharacter != null : _AOArenaBehaviorManager.ChosenCharacter is null);

        public bool conversation_arena_weapon_choice_allowed_on_condition() => _AOArenaBehaviorManager.IsWeaponChoiceAllowed();

        public bool conversation_arena_weapon_choice_forbidden_on_condition() => !_AOArenaBehaviorManager.IsWeaponChoiceAllowed();

        private bool conversation_town_arena_culture_match_on_condition(CultureObject culture, int stage, ArenaPracticeMode practiceMode) =>
            culture.Equals(Settlement.CurrentSettlement.MapFaction?.Culture ?? Settlement.CurrentSettlement.Culture) && stage == _AOArenaBehaviorManager.ChosenLoadoutStage && practiceMode.Contains(_AOArenaBehaviorManager.PracticeMode);

        private bool conversation_arena_join_fight_with_default_weapons_on_condition(out TextObject? explanation) => _AOArenaBehaviorManager.CheckAffordabilityForNextPracticeRound(_AOArenaBehaviorManager.PracticeMode, 0, out explanation);

        private bool conversation_arena_join_fight_with_new_weapons_on_condition(out TextObject? explanation) =>
            _AOArenaBehaviorManager.CheckAffordabilityForNextPracticeRound(_AOArenaBehaviorManager.PracticeMode, 1, out explanation);

        private bool conversation_arena_master_practice_choose_companion_choice_repeatable_on_condition()
        {
            var selectedRepeatLine = ConversationSentence.SelectedRepeatLine;
            if (ConversationSentence.CurrentProcessedRepeatObject is not CharacterObject processedRepeatObject)
            {
                return false;
            }

            StringHelpers.SetRepeatableCharacterProperties("HERO", processedRepeatObject);
            return true;
        }

        private bool conversation_town_arena_weapon_choice_request_confirm_on_condition()
        {
            int price = _AOArenaBehaviorManager.GetWeaponLoadoutChoiceCost();
            MBTextManager.SetTextVariable("WEAPON_CHOICE_HAS_PRICE", (price > 0 ? 1 : 0).ToString(), false);
            MBTextManager.SetTextVariable("WEAPON_CHOICE_PRICE", (_AOArenaBehaviorManager.ChosenLoadoutStage * price).ToString(), false);
            return true;
        }

        private bool conversation_town_arena_weapons_list_on_condition() => _AOArenaBehaviorManager.ChosenLoadoutStage < Settings.Instance!.PracticeLoadoutStages;

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

        private bool conversation_town_arena_parry_fight_join_check_on_condition(out TextObject? explanation)
        {
            if (!conversation_town_arena_fight_join_check_on_condition(out explanation) || !_AOArenaBehaviorManager.CheckAvailabilityOfWeaponLoadouts(ArenaPracticeMode.Parry, out explanation))
            {
                return false;
            }

            explanation = null;
            return true;
        }

        private bool conversation_arena_return_to_default_choice_allowed_on_condition()
        {
            TextObject textObject = _AOArenaBehaviorManager.ChosenCharacter is null
                ? new("{=WRO1rFtQm}I'll do that with standard gear.")
                : new("{=8k8br8lfq}{?COMPANION_GENDER}She{?}He{\\?}'ll do that with standard gear.", new() { ["COMPANION_GENDER"] = (_AOArenaBehaviorManager.ChosenCharacter.IsFemale ? 1 : 0).ToString() });
            MBTextManager.SetTextVariable("DEFAULT_WEAPONS_CHOICE_LINE", textObject, false);
            return conversation_arena_weapon_choice_allowed_on_condition() && _AOArenaBehaviorManager.ChosenLoadout >= 0;
        }

        private bool conversation_arena_make_new_weapon_choice_on_condition()
        {
            TextObject textObject = _AOArenaBehaviorManager.ChosenCharacter is null
                ? new("{=uLNYivCXl}Sure. Although I'd like to take a new loadout.")
                : new("{=rPVHszLLs}Sure. Although I'd like to pick a new loadout for {COMPANION_NAME} to use.", new() { ["COMPANION_NAME"] = _AOArenaBehaviorManager.ChosenCharacter.Name });
            MBTextManager.SetTextVariable("NEW_LOADOUT_CHOICE_LINE", textObject, false);
            return conversation_arena_weapon_choice_allowed_on_condition();
        }

        private bool conversation_town_arena_afford_loadout_choice_on_condition(out TextObject? explanation, int stage)
        {
            return _AOArenaBehaviorManager.CheckAffordabilityForNextPracticeRound(_AOArenaBehaviorManager.PracticeMode, stage, out explanation);
        }

        internal bool conversation_town_arena_afford_loadout_choice_on_condition(out TextObject? explanation)
        {
            return _AOArenaBehaviorManager.CheckAffordabilityForNextPracticeRound(out explanation);
        }

        private static bool conversation_arena_expansive_practice_fight_explain_reward_on_condition()
        {
            PracticePrizeManager.ExplainPracticeReward(ArenaPracticeMode.Expansive);
            return true;
        }

        private bool conversation_arena_can_join_practice_fight_on_condition() => !Settlement.CurrentSettlement.Town.HasTournament;

        private bool conversation_arena_join_special_practice_fight_confirm_on_condition()
        {
            int price = _AOArenaBehaviorManager.GetPracticeModeChoiceCost();
            MBTextManager.SetTextVariable("PRACTICE_CHOICE_HAS_PRICE", (price > 0 ? 1 : 0).ToString(), false);
            MBTextManager.SetTextVariable("PRACTICE_CHOICE_PRICE", price.ToString(), false);

            return !Settlement.CurrentSettlement.Town.HasTournament;
        }

        private void conversation_arena_companion_choice_request_on_consequence()
        {
            ConversationSentence.SetObjectsToRepeatOver((IReadOnlyList<object>) AOArenaBehaviorManager.GetAvailableHeroes());
        }

        private void conversation_arena_master_practice_choose_companion_choice_repeatable_on_consequence()
        {
            if (ConversationSentence.SelectedRepeatObject is not CharacterObject selectedRepeatObject)
            {
                return;
            }
            _AOArenaBehaviorManager.ChosenCharacter = selectedRepeatObject;
        }

        private void conversation_arena_join_fight_with_selected_loadout_on_consequence(int loadout)
        {
            _AOArenaBehaviorManager.ChosenLoadout = loadout;
            conversation_arena_join_fight_with_pre_selected_loadout_on_consequence();
        }

        public void conversation_arena_join_fight_with_pre_selected_loadout_on_consequence()
        {
            Campaign.Current.ConversationManager.ConversationEndOneShot += new Action(StartPlayerPracticeAfterConversationEnd);
        }

        private void conversation_town_arena_weapons_list_get_better_on_consequence() => _AOArenaBehaviorManager.ChosenLoadoutStage++;

        public void conversation_arena_join_fight_with_default_weapons_on_consequence()
        {
            _AOArenaBehaviorManager.ResetWeaponChoice();
            Campaign.Current.ConversationManager.ConversationEndOneShot += new Action(StartPlayerPracticeAfterConversationEnd);
        }

        private static void StartPlayerPracticeAfterConversationEnd()
        {
            AOArenaBehaviorManager.Instance!.PayForPracticeMatch();
            Mission.Current.SetMissionMode(AOArenaBehaviorManager.Instance!.GetArenaPracticeMissionMode(), false);
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
                    TournamentGame tournamentGame = Campaign.Current.TournamentManager.GetTournamentGame(town);

                    string distanceEstimate = GetDistanceEstimate(town, nearbyTownDistances, out bool isCloseBy);
                    int tournamentAge = (int) tournamentGame.CreationTime.ElapsedDaysUntilNow;
                    int textVariation = (isCloseBy ? 0 : 3) + (tournamentAge <= 4 ? 0 : tournamentAge > 12 ? 2 : 1);

                    ItemObject prizeItemObject = tournamentGame.Prize;
                    TextObject prizeTextObject = new("{=UOpsoG57t}tier {TIER} {TYPE}, {NAME}, worth {GOLD}{GOLD_ICON}");
                    LocalizationHelper.SetNumericVariable(prizeTextObject, "TIER", (int) prizeItemObject.Tier + 1);
                    prizeTextObject.SetTextVariable("TYPE", GetItemTypeName(prizeItemObject.Type));
                    prizeTextObject.SetTextVariable("NAME", TournamentRewardManager.GetPrizeItemName(tournamentGame));
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

        private void game_menu_enter_expansive_practice_fight_on_consequence(MenuCallbackArgs args) => StartPlayerPracticeFromMenu(ArenaPracticeMode.Expansive);

        private void game_menu_enter_parry_practice_fight_on_consequence(MenuCallbackArgs args) => StartPlayerPracticeFromMenu(ArenaPracticeMode.Parry);

        private void game_menu_enter_team_practice_fight_on_consequence(MenuCallbackArgs args) => StartPlayerPracticeFromMenu(ArenaPracticeMode.Team);

        private void StartPlayerPracticeFromMenu(ArenaPracticeMode practiceMode)
        {
            if (!FieldAccessHelper.ArenaMasterHasMetInSettlementsByRef(_arenaMasterBehavior!).Contains(Settlement.CurrentSettlement))
            {
                FieldAccessHelper.ArenaMasterHasMetInSettlementsByRef(_arenaMasterBehavior!).Add(Settlement.CurrentSettlement);
            }
            _AOArenaBehaviorManager.IsPlayerPrePractice = true;
            _AOArenaBehaviorManager.SetPracticeMode(practiceMode);
            PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(LocationComplex.Current.GetLocationWithId("arena"), null, null, null);
            _AOArenaBehaviorManager._enteredPracticeFightFromMenu = true;
        }

        private bool game_menu_enter_expansive_practice_fight_on_condition(MenuCallbackArgs args) => CheckCustomPracticeFightMenuAvailability(args, ArenaPracticeMode.Expansive);

        private bool game_menu_enter_parry_practice_fight_on_condition(MenuCallbackArgs args) => CheckCustomPracticeFightMenuAvailability(args, ArenaPracticeMode.Parry);

        private bool game_menu_enter_team_practice_fight_on_condition(MenuCallbackArgs args) => CheckCustomPracticeFightMenuAvailability(args, ArenaPracticeMode.Team);

        private bool CheckCustomPracticeFightMenuAvailability(MenuCallbackArgs args, ArenaPracticeMode practiceMode)
        {
            if (!AOArenaBehaviorManager.IsPracticeModeEnabled(practiceMode))
            {
                return false;
            }

            Settlement currentSettlement = Settlement.CurrentSettlement;
            args.optionLeaveType = GameMenuOption.LeaveType.PracticeFight;
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
            if (currentSettlement.Town.HasTournament)
            {
                args.Tooltip = new TextObject("{=NESB0CVc}There is no practice fight because of the Tournament.", null);
                args.IsEnabled = false;
                return true;
            }
            if (!_AOArenaBehaviorManager.CheckAvailabilityOfWeaponLoadouts(practiceMode, out args.Tooltip))
            {
                args.IsEnabled = false;
                return true;
            }
            if (!_AOArenaBehaviorManager.CheckAffordabilityForNextPracticeRound(practiceMode, 0, out args.Tooltip, true))
            {
                args.IsEnabled = false;
                return true;
            }

            return true;
        }

        public bool team_game_select_roster_condition(MenuCallbackArgs args)
        {
            if (!Settings.Instance!.EnableTeamTournaments)
            {
                return false;
            }

            bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(
                Settlement.CurrentSettlement,
                SettlementAccessModel.SettlementAction.JoinTournament,
                out bool shouldBeDisabled,
                out TextObject disabledText
            );

            args.optionLeaveType = GameMenuOption.LeaveType.TroopSelection;

            // if this town has a tournament, activate
            shouldBeDisabled &= TeamTournamentHelpers.IsTournamentActive;
            canPlayerDo &= TeamTournamentHelpers.IsTournamentActive;

            if (shouldBeDisabled || string.IsNullOrEmpty(disabledText.ToString()))
                disabledText = new TextObject("{=Ams5ccKzh}Roster can only be selected for team tournaments.");

            return TWHelpers.MenuHelper.SetOptionProperties(args, canPlayerDo, shouldBeDisabled, disabledText);
        }

        public void team_game_select_roster_consequence(MenuCallbackArgs args)
        {
            if (args.MenuContext.Handler is MenuViewContext menuViewContext)
            {
                if (TeamTournamentInfo.Current != null && !TeamTournamentInfo.Current.IsFinished)
                    TeamTournamentInfo.Current.OpenSelectionMenu(menuViewContext);
                else
                    new TeamTournamentInfo().OpenSelectionMenu(menuViewContext);
            }
        }

#if DEBUG // only needed when debugging for testing
        private bool AddTournamentCondition(MenuCallbackArgs args) => !Settlement.CurrentSettlement.Town.HasTournament;
        private void AddTournamentConsequence(MenuCallbackArgs args)
        {
            Campaign.Current.TournamentManager.AddTournament(new FightTournamentGame(Settlement.CurrentSettlement.Town));
            GameMenu.SwitchToMenu("town_arena");
        }

        private bool ResolveTournamentCondition(MenuCallbackArgs args) => Settlement.CurrentSettlement.Town.HasTournament;
        private void ResolveTournamentConsequence(MenuCallbackArgs args)
        {
            var town = Settlement.CurrentSettlement.Town;
            if (town.HasTournament)
            {
                var tournament = Campaign.Current.TournamentManager.GetTournamentGame(town);
                Campaign.Current.TournamentManager.ResolveTournament(tournament, town);
            }
            GameMenu.SwitchToMenu("town_arena");
        }
#endif

#pragma warning restore IDE1006 // Naming Styles

        public override void SyncData(IDataStore dataStore)
        {
#pragma warning disable CS8601 // Possible null reference assignment.
            dataStore.SyncDataAsJson("_AOArenaBehaviorManager", ref _AOArenaBehaviorManager);
#pragma warning restore CS8601 // Possible null reference assignment.
            if (dataStore.IsLoading)
            {
                _AOArenaBehaviorManager ??= new AOArenaBehaviorManager();
                _AOArenaBehaviorManager.Sync();
            }
            AOArenaBehaviorManager.Instance = _AOArenaBehaviorManager;
        }
    }
}