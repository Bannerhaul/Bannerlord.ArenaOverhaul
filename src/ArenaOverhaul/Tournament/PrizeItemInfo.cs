using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace ArenaOverhaul.Tournament
{
    public class PrizeItemInfo(ItemObject itemObject, ItemModifier? itemModifier)
    {
        [SaveableProperty(1)]
        public ItemObject ItemObject { get; private set; } = itemObject;

        [SaveableProperty(2)]
        public ItemModifier? ItemModifier { get; private set; } = itemModifier;
    }
}