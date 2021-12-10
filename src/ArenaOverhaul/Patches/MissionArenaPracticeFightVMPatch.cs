using ArenaOverhaul.Helpers;

using HarmonyLib;

using SandBox.ViewModelCollection;

using TaleWorlds.Core;

namespace ArenaOverhaul.Patches
{
    [HarmonyPatch(typeof(MissionArenaPracticeFightVM))]
    public static class MissionArenaPracticeFightVMPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("UpdatePrizeText")]
        public static bool UpdatePrizeTextPrefix(MissionArenaPracticeFightVM __instance) //Bool prefixes compete with each other and skip others, as well as original, if return false
        {
            int remainingOpponentCount = FieldAccessHelper.MAPFVMPracticeMissionControllerByRef(__instance).RemainingOpponentCount;
            int countBeatenByPlayer = FieldAccessHelper.MAPFVMPracticeMissionControllerByRef(__instance).OpponentCountBeatenByPlayer;

            int prizeAmount = PracticePrizeManager.GetPrizeAmount(remainingOpponentCount, countBeatenByPlayer);
            GameTexts.SetVariable("DENAR_AMOUNT", prizeAmount);
            GameTexts.SetVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"8\">");
            __instance.PrizeText = GameTexts.FindText("str_earned_denar", null).ToString();
            return false;
        }
    }
}
