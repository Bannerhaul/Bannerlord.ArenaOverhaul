using ArenaOverhaul.CampaignBehaviors.BehaviorManagers;
using ArenaOverhaul.Tournament;

using System.Collections.Generic;

using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;

namespace ArenaOverhaul
{
    internal class ArenaOverhaulSaveableTypeDefiner : SaveableTypeDefiner
    {
        public ArenaOverhaulSaveableTypeDefiner() : base(2003477000) { }

        protected override void DefineClassTypes()
        {
            //BasicSavableClasses (1 through 99)
            base.AddClassDefinition(typeof(PrizeItemInfo), 1);

            //BehaviorManagers (100 through 150)
            base.AddClassDefinition(typeof(AOArenaBehaviorManager), 100);
        }

        protected override void DefineContainerDefinitions()
        {
            base.ConstructContainerDefinition(typeof(Dictionary<Town, PrizeItemInfo?>));
        }
    }
}