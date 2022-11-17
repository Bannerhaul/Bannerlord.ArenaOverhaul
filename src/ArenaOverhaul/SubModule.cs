using ArenaOverhaul.CampaignBehaviors;
using ArenaOverhaul.Extensions;
using ArenaOverhaul.Helpers;

using HarmonyLib;

using System;

using TaleWorlds.CampaignSystem;
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

        public bool Patched { get; private set; }
        public bool OnBeforeInitialModuleScreenSetAsRootWasCalled { get; private set; }

        private Harmony? _arenaOverhaulHarmonyInstance;
        public Harmony? ArenaOverhaulHarmonyInstance { get => _arenaOverhaulHarmonyInstance; private set => _arenaOverhaulHarmonyInstance = value; }

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            Patched = false;
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
                gameStarter.AddBehavior(new AOArenaBehavior());
            }
        }
    }
}