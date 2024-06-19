using ArenaOverhaul.CampaignBehaviors;
using ArenaOverhaul.CampaignBehaviors.BehaviorManagers;
using ArenaOverhaul.Extensions;
using ArenaOverhaul.Helpers;
using ArenaOverhaul.Models;
using ArenaOverhaul.ModSettings;

using Bannerlord.BUTR.Shared.Helpers;
using Bannerlord.UIExtenderEx;

using HarmonyLib;

using MCM.Abstractions.Base.PerSave;

using System;
using System.Collections.Generic;
using System.Runtime;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace ArenaOverhaul
{
    public class SubModule : MBSubModuleBase
    {
        private const string SLoaded = "{=miUO4w1hn}Loaded Arena Overhaul!";
        private const string SErrorLoading = "{=5KIeqRJJX}Arena Overhaul failed to load! See details in the mod log.";
        private const string SErrorInitialising = "{=LRhyA9mqB}Error initialising Arena Overhaul! See details in the mod log. Error text: \"{EXCEPTION_MESSAGE}\"";

        private static readonly UIExtender Extender = UIExtender.Create("ArenaOverhaulUI");

        internal static FluentPerSaveSettings? PerSaveSettings;

        public bool Patched { get; private set; }
        public bool OnBeforeInitialModuleScreenSetAsRootWasCalled { get; private set; } = false;

        private Harmony? _arenaOverhaulHarmonyInstance;
        public Harmony? ArenaOverhaulHarmonyInstance { get => _arenaOverhaulHarmonyInstance; private set => _arenaOverhaulHarmonyInstance = value; }

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            Patched = false;

            Extender.Register(typeof(SubModule).Assembly);
            Extender.Enable();
        }

        protected override void OnSubModuleUnloaded()
        {
            base.OnSubModuleUnloaded();
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            try
            {
                if (OnBeforeInitialModuleScreenSetAsRootWasCalled)
                {
                    return;
                }
                OnBeforeInitialModuleScreenSetAsRootWasCalled = true;

                Patched = HarmonyHelper.PatchAll(ref _arenaOverhaulHarmonyInstance, "OnSubModuleLoad", "Initialization error - {0}");
                if (Patched)
                {
                    InformationManager.DisplayMessage(new InformationMessage(SLoaded.ToLocalizedString(), Color.FromUint(4282569842U)));
                }
                else
                {
                    MessageHelper.ErrorMessage(SErrorLoading.ToLocalizedString());
                }
            }
            catch (Exception ex)
            {
                DebugHelper.HandleException(ex, "OnBeforeInitialModuleScreenSetAsRoot", "Initialization error - {0}", SErrorInitialising);
            }
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            if (game.GameType is Campaign)
            {
                //CampaignGameStarter
                CampaignGameStarter gameStarter = (CampaignGameStarter) gameStarterObject;
                //Behaviors
                AOArenaBehaviorManager._companionPracticeSettings ??= [];
                gameStarter.AddBehavior(new AOArenaBehavior());
                //Models
                AddGameModels(gameStarterObject, gameStarter);
            }
        }

        private void AddGameModels(IGameStarter gameStarterObject, CampaignGameStarter gameStarter)
        {
            DecoratorModelHelper.AddDecoratorModel<TournamentModel, ArenaOverhaulTournamentModel, DefaultTournamentModel>(gameStarterObject, gameStarter, (previouslyAssignedModel) => new ArenaOverhaulTournamentModel(previouslyAssignedModel));
        }

        public override void OnAfterGameInitializationFinished(Game game, object starterObject)
        {
            if (game.GameType is not Campaign campaign)
            {
                return;
            }

            var builder = CompanionPracticeSettings.AddCompanionPracticeSettings(AOArenaBehaviorManager._companionPracticeSettings!);
            PerSaveSettings = builder.BuildAsPerSave();
            PerSaveSettings?.Register();
        }

        public override void OnGameEnd(Game game)
        {
            var oldSettings = PerSaveSettings;
            oldSettings?.Unregister();
            PerSaveSettings = null;

            AOArenaBehaviorManager._companionPracticeSettings = null;
        }
    }
}