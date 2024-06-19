using ArenaOverhaul.CampaignBehaviors.BehaviorManagers;
using ArenaOverhaul.ModSettings;

using System;

using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace ArenaOverhaul.ArenaPractice
{
    public static class PracticePrizeManager
    {
        public static int GetPrizeAmount(int remainingOpponentCount, int countBeatenByPlayer)
        {
            return remainingOpponentCount == 0 ? GetLastManStandingPrizeAmount(countBeatenByPlayer) : GetValorPrizeAmount(countBeatenByPlayer);
        }

        public static (int PrizeAmount, TextObject Explanation, int ValorPrizeAmount) GetPrizeAmountExplained(int remainingOpponentCount, int countBeatenByPlayer)
        {
            return (GetPrizeAmount(remainingOpponentCount, countBeatenByPlayer), GetTextExplanation(remainingOpponentCount, countBeatenByPlayer), GetValorPrizeAmount(countBeatenByPlayer));
        }

        public static void ExplainPracticeReward(bool isAboutExpansivePractice = false)
        {
            MBTextManager.SetTextVariable("OPPONENT_COUNT_1", "3", false);
            MBTextManager.SetTextVariable("PRIZE_1", (isAboutExpansivePractice ? Settings.Instance!.ExpansivePracticeValorReward1 : Settings.Instance!.PracticeValorReward1).ToString(), false);
            MBTextManager.SetTextVariable("OPPONENT_COUNT_2", "6", false);
            MBTextManager.SetTextVariable("PRIZE_2", (isAboutExpansivePractice ? Settings.Instance!.ExpansivePracticeValorReward2 : Settings.Instance!.PracticeValorReward2).ToString(), false);
            MBTextManager.SetTextVariable("OPPONENT_COUNT_3", "10", false);
            MBTextManager.SetTextVariable("PRIZE_3", (isAboutExpansivePractice ? Settings.Instance!.ExpansivePracticeValorReward3 : Settings.Instance!.PracticeValorReward3).ToString(), false);
            MBTextManager.SetTextVariable("OPPONENT_COUNT_4", "20", false);
            MBTextManager.SetTextVariable("PRIZE_4", (isAboutExpansivePractice ? Settings.Instance!.ExpansivePracticeValorReward4 : Settings.Instance!.PracticeValorReward4).ToString(), false);
            MBTextManager.SetTextVariable("OPPONENT_COUNT_5", "35", false);
            MBTextManager.SetTextVariable("PRIZE_5", (isAboutExpansivePractice ? Settings.Instance!.ExpansivePracticeValorReward5 : Settings.Instance!.PracticeValorReward5).ToString(), false);
            MBTextManager.SetTextVariable("PRIZE_CHAMP", (isAboutExpansivePractice ? Settings.Instance!.ExpansivePracticeChampionReward : Settings.Instance!.PracticeChampionReward).ToString(), false);

            int totalParticipants = isAboutExpansivePractice ? Settings.Instance!.ExpansivePracticeTotalParticipants : Settings.Instance!.PracticeTotalParticipants;
            int valorVariation = totalParticipants switch
            {
                >= 35 => 4,
                >= 20 => 3,
                >= 10 => 2,
                >= 6 => 1,
                _ => 0,
            };
            MBTextManager.SetTextVariable("VALOR_PRIZES_DESCRIPTION", GameTexts.FindText("str_arena_reward_valor_overhauled", valorVariation.ToString()), false);
            MBTextManager.SetTextVariable("CHAMPION_PRIZE_DESCRIPTION", GameTexts.FindText("str_arena_reward_champ_overhauled", GetLMSPrizeCalculationTypeIndex().ToString()), false);

            MBTextManager.SetTextVariable("ARENA_REWARD", GameTexts.FindText("str_arena_reward_overhauled", null), false);
        }

        private static TextObject GetTextExplanation(int remainingOpponentCount, int countBeatenByPlayer)
        {
            int valorCat = GetValorCategory(countBeatenByPlayer);
            int baseCat = countBeatenByPlayer > 0 ? 1 : 0;
            int textVariation = remainingOpponentCount == 0
                                ? GetLMSPrizeCalculationTypeIndex() switch
                                {
                                    1 => 7 + (valorCat > 3 ? 2 : Math.Min(valorCat, 1)),
                                    2 => valorCat > 1 ? 10 + valorCat - 2 : 7,
                                    _ => 7,
                                }
                                : baseCat + valorCat;
            return GameTexts.FindText("str_arena_take_down_overhauled", textVariation.ToString());
        }

        private static int GetLastManStandingPrizeAmount(int countBeatenByPlayer)
        {
            int calculationTypeIndex = GetLMSPrizeCalculationTypeIndex();
            return calculationTypeIndex switch
            {
                2 => Math.Max(GetValorCategory(countBeatenByPlayer) - 1, 1) * GetLastManStandingBasePrize(),
                1 => GetLastManStandingBasePrize() + GetValorPrizeAmount(countBeatenByPlayer),
                _ => GetLastManStandingBasePrize()
            };
        }

        private static int GetLMSPrizeCalculationTypeIndex()
        {
            return AOArenaBehaviorManager.Instance!.PracticeMode switch
            {
                ArenaPracticeMode.Standard => Settings.Instance!.PracticeChampionPrizeCalculation.SelectedIndex,
                ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeChampionPrizeCalculation.SelectedIndex,
                _ => 0,
            };
        }

        private static int GetLastManStandingBasePrize()
        {
            return AOArenaBehaviorManager.Instance!.PracticeMode switch
            {
                ArenaPracticeMode.Standard => Settings.Instance!.PracticeChampionReward,
                ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeChampionReward,
                _ => 0,
            };
        }

        private static int GetValorCategory(int countBeatenByPlayer) =>
            countBeatenByPlayer switch
            {
                >= 35 => 5,
                >= 20 => 4,
                >= 10 => 3,
                >= 6 => 2,
                >= 3 => 1,
                _ => 0
            };

        private static int GetValorPrizeAmount(int countBeatenByPlayer) =>
            GetValorCategory(countBeatenByPlayer) switch
            {
                1 => AOArenaBehaviorManager.Instance!.PracticeMode switch
                {
                    ArenaPracticeMode.Standard => Settings.Instance!.PracticeValorReward1,
                    ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeValorReward1,
                    _ => 0,
                },
                2 => AOArenaBehaviorManager.Instance!.PracticeMode switch
                {
                    ArenaPracticeMode.Standard => Settings.Instance!.PracticeValorReward2,
                    ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeValorReward2,
                    _ => 0,
                },
                3 => AOArenaBehaviorManager.Instance!.PracticeMode switch
                {
                    ArenaPracticeMode.Standard => Settings.Instance!.PracticeValorReward3,
                    ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeValorReward3,
                    _ => 0,
                },
                4 => AOArenaBehaviorManager.Instance!.PracticeMode switch
                {
                    ArenaPracticeMode.Standard => Settings.Instance!.PracticeValorReward4,
                    ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeValorReward4,
                    _ => 0,
                },
                5 => AOArenaBehaviorManager.Instance!.PracticeMode switch
                {
                    ArenaPracticeMode.Standard => Settings.Instance!.PracticeValorReward5,
                    ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeValorReward5,
                    _ => 0,
                },
                _ => 0
            };
    }
}