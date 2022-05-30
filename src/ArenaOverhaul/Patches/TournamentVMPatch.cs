using HarmonyLib;

using SandBox.ViewModelCollection;
using SandBox.ViewModelCollection.Tournament;

using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace ArenaOverhaul.Patches
{
    [HarmonyPatch(typeof(TournamentVM))]
    public static class TournamentVMPatch
    {
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
    }
}