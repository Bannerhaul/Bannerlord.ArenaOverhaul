using ArenaOverhaul.Helpers;
using ArenaOverhaul.Tournament;

using HarmonyLib;
using HarmonyLib.BUTR.Extensions;

using SandBox.ViewModelCollection;
using SandBox.ViewModelCollection.Tournament;

using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.TournamentGames;

namespace ArenaOverhaul.Patches
{
    [HarmonyPatch(typeof(TournamentVM))]
    public static class TournamentVMPatch
    {
        private static readonly MethodInfo? miTournamentGetter = AccessTools2.PropertyGetter(typeof(TournamentVM), "Tournament");
        private static readonly MethodInfo? miTournamentPrizeGetter = AccessTools2.PropertyGetter(typeof(TournamentGame), "Prize");
        private static readonly MethodInfo? miTextObjectToString = AccessTools2.Method(typeof(object), "ToString");

        private static readonly MethodInfo? miGetFullPrizeName = AccessTools2.DeclaredMethod(typeof(TournamentVMPatch), "GetFullPrizeName");

        [HarmonyTranspiler]
        [HarmonyPatch("OnTournamentEnd")]
        public static IEnumerable<CodeInstruction> OnTournamentEndTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = new(instructions);
            int numberOfEdits = 0;
            int getPrizeIndex = 0;
            int toStringIndex = 0;
            if (miTournamentGetter != null && miTournamentPrizeGetter != null && miTextObjectToString != null)
            {
                for (int i = 2; i < codes.Count; ++i)
                {
                    if (numberOfEdits == 0 && codes[i].Calls(miTournamentGetter) && codes[i + 2].Calls(miTournamentPrizeGetter) && codes[i + 4].Calls(miTextObjectToString) && codes[i + 6].opcode == OpCodes.Ldstr && codes[i + 6].operand.ToString() == "REWARD")
                    {
                        getPrizeIndex = i + 2;
                        toStringIndex = i + 5;
                        ++numberOfEdits;
                        break;
                    }
                }
            }

            //Logging
            const int RequiredNumberOfEdits = 1;
            if (getPrizeIndex == 0 || toStringIndex == 0 || numberOfEdits < RequiredNumberOfEdits || miGetFullPrizeName is null)
            {
                LoggingHelper.LogNoHooksIssue(
                    codes, numberOfEdits, RequiredNumberOfEdits, __originalMethod,
                    [
                        (nameof(getPrizeIndex), getPrizeIndex), (nameof(toStringIndex), toStringIndex),
                    ],
                    [
                        (nameof(miTournamentGetter), miTournamentGetter),
                        (nameof(miTournamentPrizeGetter), miTournamentPrizeGetter),
                        (nameof(miTextObjectToString), miTextObjectToString),
                        (nameof(miGetFullPrizeName), miGetFullPrizeName)
                    ]);
            }

            if (getPrizeIndex > 0 && toStringIndex > 0)
            {
                codes.RemoveRange(getPrizeIndex, toStringIndex - getPrizeIndex);
                codes.InsertRange(getPrizeIndex, [new CodeInstruction(opcode: OpCodes.Call, operand: miGetFullPrizeName)]);
            }
            else
            {
                MessageHelper.ErrorMessage("Harmony transpiler for TournamentVM. OnTournamentEnd could not find code hooks for applying prize quality name!");
            }

            return codes.AsEnumerable();
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnTournamentEnd")]
        public static void OnTournamentEndPostfix(TournamentVM __instance)
        {
            bool winnerIsPlayer = __instance.Tournament.Winner.IsPlayer;
            Town tournamentTown = __instance.Tournament.TournamentGame.Town;

            int renownReward = TournamentRewardManager.GetTakedownRenownReward(Hero.MainHero, tournamentTown);
            if (!winnerIsPlayer && renownReward > 0) //if player won he will get takedown renown reward included in champion's award
            {
                GameTexts.SetVariable("RENOWN_TAKEDOWN_REWARD", renownReward.ToString());
                __instance.BattleRewards.Add(new TournamentRewardVM(GameTexts.FindText("str_tournament_renown_takedown_reward").ToString()));
            }

            int playerGoldPrize = winnerIsPlayer ? TournamentRewardManager.GetTournamentGoldPrize(tournamentTown) : 0;
            int playerRoundWinnings = TournamentRewardManager.RoundPrizeWinners[tournamentTown].FirstOrDefault(x => x.Participant.IsHumanPlayerCharacter).Winnings;
            if (playerGoldPrize > 0 || playerRoundWinnings > 0)
            {
                if (playerGoldPrize > 0)
                {
                    GameTexts.SetVariable("TOTAL_GOLD_REWARD", (playerGoldPrize + playerRoundWinnings).ToString());
                    __instance.BattleRewards.Add(new TournamentRewardVM(GameTexts.FindText("str_tournament_gold_reward", "3").ToString()));
                }
                else
                {
                    GameTexts.SetVariable("PER_ROUND_REWARD", playerRoundWinnings.ToString());
                    __instance.BattleRewards.Add(new TournamentRewardVM(GameTexts.FindText("str_tournament_gold_reward", winnerIsPlayer ? "2" : (renownReward > 0 ? "1" : "0")).ToString()));
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch("ExecuteShowPrizeItemTooltip")]
        public static bool ExecuteShowPrizeItemTooltipPrefix(TournamentVM __instance) //Bool prefixes compete with each other and skip others, as well as original, if return false
        {
            if (!__instance.HasPrizeItem)
            {
                return false;
            }
            TournamentRewardManager.ShowPrizeItemHint(__instance.Tournament.TournamentGame);
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("RefreshValues")]
        public static void RefreshValuesPostfix(TournamentVM __instance) 
        {
            if (!__instance.HasPrizeItem)
            {
                return;
            }

            if (TournamentRewardManager.TryGetPrizeItemModifier(__instance.Tournament.TournamentGame, out var prizeItemInfo))
            {
                var equipmentElement = new EquipmentElement(prizeItemInfo!.ItemObject, prizeItemInfo.ItemModifier);
                __instance.PrizeItemName = equipmentElement.GetModifiedItemName().ToString();
            }
        }

        internal static string GetFullPrizeName(TournamentGame tournamentGame)
        {
            if (!TournamentRewardManager.TournamentPrizeAwards.TryGetValue(tournamentGame.Town, out var prizeItemInfo) || prizeItemInfo is null || tournamentGame.Prize.StringId != prizeItemInfo.ItemObject.StringId || prizeItemInfo.ItemModifier is null)
            {
                return tournamentGame.Prize.Name.ToString();
            }
            return new EquipmentElement(prizeItemInfo.ItemObject, prizeItemInfo.ItemModifier).GetModifiedItemName().ToString();
        }
    }
}