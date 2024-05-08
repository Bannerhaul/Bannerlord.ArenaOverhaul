using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.ObjectSystem;

namespace ArenaOverhaul.Tournament
{
    public abstract class AbstractTournamentApplicant<T>(CharacterObject characterObject, MobileParty? originParty, int importance, T? grouppingObject) where T : MBObjectBase
    {
        public CharacterObject CharacterObject { get; init; } = characterObject;
        public MobileParty? OriginParty { get; init; } = originParty;
        public int Importance { get; init; } = importance;

        public T? GrouppingObject { get; init; } = grouppingObject;
    }
}