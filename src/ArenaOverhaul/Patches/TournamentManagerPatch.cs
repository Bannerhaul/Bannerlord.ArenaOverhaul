using ArenaOverhaul.Tournament;

using HarmonyLib;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;

namespace ArenaOverhaul.Patches
{
    [HarmonyPatch(typeof(TournamentManager))]
    public static class TournamentManagerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("GivePrizeToWinner")]
        public static bool ExecuteShowPrizeItemTooltipPrefix(TournamentManager __instance, TournamentGame tournament, Hero winner, bool isPlayerParticipated) //Bool prefixes compete with each other and skip others, as well as original, if return false
        {
            if (!isPlayerParticipated)
            {
                tournament.UpdateTournamentPrize(isPlayerParticipated);
            }

            EquipmentElement prizeEquipmentElement = TournamentRewardManager.TryGetPrizeItemModifier(tournament, out var prizeItemInfo)
                ? new(prizeItemInfo!.ItemObject, prizeItemInfo.ItemModifier)
                : new(tournament.Prize);

            if (winner.PartyBelongedTo == MobileParty.MainParty)
            {
                winner.PartyBelongedTo.ItemRoster.AddToCounts(prizeEquipmentElement, 1);
            }
            else
            {
                if (winner.Clan == null)
                {
                    return false;
                }
                GiveGoldAction.ApplyBetweenCharacters(null, winner.Clan.Leader, tournament.Town.MarketData.GetPrice(prizeEquipmentElement));
            }

            return false;
        }
    }
}