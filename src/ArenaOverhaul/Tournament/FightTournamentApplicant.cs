using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace ArenaOverhaul.Tournament
{
    public class FightTournamentApplicant(CharacterObject characterObject, MobileParty? originParty, int importance) : AbstractTournamentApplicant<MobileParty>(characterObject, originParty, importance, originParty) { }
}