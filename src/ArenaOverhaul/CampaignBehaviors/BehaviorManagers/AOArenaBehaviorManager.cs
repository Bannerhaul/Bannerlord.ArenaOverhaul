using ArenaOverhaul.Tournament;

using System.Collections.Generic;

using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;

namespace ArenaOverhaul.CampaignBehaviors.BehaviorManagers
{
    public class AOArenaBehaviorManager
    {
        [SaveableProperty(1)]
        public Dictionary<Town, PrizeItemInfo?> TournamentPrizes { get; private set; } = [];
    }
}