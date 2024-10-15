using ArenaOverhaul.CampaignBehaviors.BehaviorManagers;
using ArenaOverhaul.ModSettings;

using Bannerlord.ButterLib.Common.Helpers;

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

        public static void ExplainPracticeReward(ArenaPracticeMode practiceMode)
        {
            MBTextManager.SetTextVariable("OPPONENT_COUNT_1", GetValorCategoryThreshold(1, practiceMode).ToString(), false);
            MBTextManager.SetTextVariable("PRIZE_1", GetValorCategoryPrizeAmount(1, practiceMode).ToString(), false);
            MBTextManager.SetTextVariable("OPPONENT_COUNT_2", GetValorCategoryThreshold(2, practiceMode).ToString(), false);
            MBTextManager.SetTextVariable("PRIZE_2", GetValorCategoryPrizeAmount(2, practiceMode).ToString(), false);
            MBTextManager.SetTextVariable("OPPONENT_COUNT_3", GetValorCategoryThreshold(3, practiceMode).ToString(), false);
            MBTextManager.SetTextVariable("PRIZE_3", GetValorCategoryPrizeAmount(3, practiceMode).ToString(), false);
            MBTextManager.SetTextVariable("OPPONENT_COUNT_4", GetValorCategoryThreshold(4, practiceMode).ToString(), false);
            MBTextManager.SetTextVariable("PRIZE_4", GetValorCategoryPrizeAmount(4, practiceMode).ToString(), false);
            MBTextManager.SetTextVariable("OPPONENT_COUNT_5", GetValorCategoryThreshold(5, practiceMode).ToString(), false);
            MBTextManager.SetTextVariable("PRIZE_5", GetValorCategoryPrizeAmount(5, practiceMode).ToString(), false);
            MBTextManager.SetTextVariable("PRIZE_CHAMP", GetLastManStandingBasePrize(practiceMode).ToString(), false);

            int totalParticipants = AOArenaBehaviorManager.GetTotalParticipantsCount(practiceMode);
            int valorVariation = Math.Max(GetValorCategory(totalParticipants, practiceMode) - 1, 0);

            MBTextManager.SetTextVariable("VALOR_PRIZES_DESCRIPTION", GameTexts.FindText("str_arena_reward_valor_overhauled", valorVariation.ToString()), false);
            MBTextManager.SetTextVariable("CHAMPION_PRIZE_DESCRIPTION", GameTexts.FindText("str_arena_reward_champ_overhauled", GetLMSPrizeCalculationTypeIndex().ToString()), false);

            MBTextManager.SetTextVariable("ARENA_REWARD", GameTexts.FindText("str_arena_reward_overhauled", null), false);
        }

        private static TextObject GetTextExplanation(int remainingOpponentCount, int countBeatenByPlayer)
        {
            var briefingAddressee = AOArenaBehaviorManager.Instance?.ChosenCharacter;
            if (briefingAddressee != null)
            {
                LocalizationHelper.SetEntityProperties(null, "COMPANION", briefingAddressee!.HeroObject);
            }

            var wasTeamPractice = (AOArenaBehaviorManager.Instance?.PracticeMode ?? ArenaPracticeMode.Standard) == ArenaPracticeMode.Team;
            var textVariationAddressee = wasTeamPractice
                ? 't'
                : briefingAddressee != null ? 'c' : 'p';

            if ((AOArenaBehaviorManager.Instance?.PracticeMode ?? ArenaPracticeMode.Standard) == ArenaPracticeMode.Parry)
            {
                int participantCount = AOArenaBehaviorManager.GetTotalParticipantsCount(ArenaPracticeMode.Parry);

                var beatEveryone = remainingOpponentCount == 0;
                var beatEveryoneScore = beatEveryone ? 1 : 0;
                var chamberScore = (ParryPracticeStatsManager.LastPracticeStats.ChamberBlocks < ParryPracticeStatsManager.LastPracticeStats.PerfectBlocks / 3) ? 0 : 2;
                var poorPerformance =
                    ParryPracticeStatsManager.LastPracticeStats.ChamberBlocks < participantCount / 3
                    || (ParryPracticeStatsManager.LastPracticeStats.PerfectBlocks + ParryPracticeStatsManager.LastPracticeStats.ChamberBlocks) < (ParryPracticeStatsManager.LastPracticeStats.HitsMade - participantCount)
                    || ParryPracticeStatsManager.LastPracticeStats.PerfectBlocks < ParryPracticeStatsManager.LastPracticeStats.PreparedBlocks * 4
                    || ParryPracticeStatsManager.LastPracticeStats.HitsTaken > (ParryPracticeStatsManager.LastPracticeStats.PerfectBlocks + ParryPracticeStatsManager.LastPracticeStats.ChamberBlocks) / (beatEveryone ? 4 : 2);

                int parryTextVariation = poorPerformance ? (1 - beatEveryoneScore) : 2 + beatEveryoneScore + chamberScore;//beating everyone makes poor performance even worse and a good performance better
                return GameTexts.FindText("str_arena_parry_practice_debrief", $"{parryTextVariation}{textVariationAddressee}");
            }

            int valorCat = GetValorCategory(countBeatenByPlayer);
            int baseCat = countBeatenByPlayer > 0 ? 1 : 0;
            int textVariationIndex = remainingOpponentCount == 0
                                ? GetLMSPrizeCalculationTypeIndex() switch
                                {
                                    1 => 7 + (valorCat > 3 ? 2 : Math.Min(valorCat, 1)),
                                    2 => valorCat > 1 ? 10 + valorCat - 2 : 7,
                                    _ => 7,
                                }
                                : baseCat + valorCat;
            return GameTexts.FindText("str_arena_take_down_overhauled", $"{textVariationIndex}{textVariationAddressee}");
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

        private static int GetLMSPrizeCalculationTypeIndex() => AOArenaBehaviorManager.Instance!.PracticeMode switch
        {
            ArenaPracticeMode.Standard => Settings.Instance!.PracticeChampionPrizeCalculation.SelectedIndex,
            ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeChampionPrizeCalculation.SelectedIndex,
            ArenaPracticeMode.Team => Settings.Instance!.TeamPracticeChampionPrizeCalculation.SelectedIndex,
            _ => 0,
        };

        private static int GetLastManStandingBasePrize() => GetLastManStandingBasePrize(AOArenaBehaviorManager.Instance!.PracticeMode);

        private static int GetLastManStandingBasePrize(ArenaPracticeMode practiceMode) => practiceMode switch
        {
            ArenaPracticeMode.Standard => Settings.Instance!.PracticeChampionReward,
            ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeChampionReward,
            ArenaPracticeMode.Team => Settings.Instance!.TeamPracticeChampionReward,
            _ => 0,
        };

        private static int GetValorCategoryThreshold(int valorCategory, ArenaPracticeMode practiceMode)
        {
            var totalParticipantsCount = AOArenaBehaviorManager.GetTotalParticipantsCount(practiceMode);
            return practiceMode switch
            {
                ArenaPracticeMode.Team => valorCategory switch
                {
                    5 => Math.Max(135, RoundWithModulus((int) (totalParticipantsCount / 2.25), 5)),
                    4 => Math.Max(100, RoundWithModulus(totalParticipantsCount / 3, 5)),
                    3 => Math.Max(75, RoundWithModulus(totalParticipantsCount / 4, 5)),
                    2 => Math.Max(50, RoundWithModulus(totalParticipantsCount / 6, 5)),
                    1 => Math.Max(30, RoundWithModulus(totalParticipantsCount / 10, 5)),
                    _ => throw new ArgumentOutOfRangeException(nameof(valorCategory)),
                },
                _ => valorCategory switch
                {
                    5 => Math.Max(35, RoundWithModulus((int) (totalParticipantsCount / 2.5), 5)),
                    4 => Math.Max(20, RoundWithModulus((int) (totalParticipantsCount / 4.5), 5)),
                    3 => Math.Max(10, RoundWithModulus(totalParticipantsCount / 9, 5)),
                    2 => Math.Max(6, RoundWithModulus(totalParticipantsCount / 15, 5)),
                    1 => Math.Max(3, RoundWithModulus(totalParticipantsCount / 30, 3)),
                    _ => throw new ArgumentOutOfRangeException(nameof(valorCategory)),
                }
            };

            static int RoundWithModulus(int source, int modulus) => (source / modulus) * modulus;
        }

        private static int GetValorCategory(int countBeatenByPlayer) => GetValorCategory(countBeatenByPlayer, AOArenaBehaviorManager.Instance!.PracticeMode);

        private static int GetValorCategory(int countBeatenByPlayer, ArenaPracticeMode practiceMode)
        {
            for (int i = 5; i > 0; i--)
            {
                if (countBeatenByPlayer >= GetValorCategoryThreshold(i, practiceMode))
                {
                    return i;
                }
            }
            return 0;
        }

        private static int GetValorPrizeAmount(int countBeatenByPlayer) => GetValorCategoryPrizeAmount(GetValorCategory(countBeatenByPlayer));

        private static int GetValorCategoryPrizeAmount(int valorCategory) => GetValorCategoryPrizeAmount(valorCategory, AOArenaBehaviorManager.Instance!.PracticeMode);

        private static int GetValorCategoryPrizeAmount(int valorCategory, ArenaPracticeMode practiceMode) => valorCategory switch
        {
            1 => practiceMode switch
            {
                ArenaPracticeMode.Standard => Settings.Instance!.PracticeValorReward1,
                ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeValorReward1,
                ArenaPracticeMode.Team => Settings.Instance!.TeamPracticeValorReward1,
                _ => 0,
            },
            2 => practiceMode switch
            {
                ArenaPracticeMode.Standard => Settings.Instance!.PracticeValorReward2,
                ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeValorReward2,
                ArenaPracticeMode.Team => Settings.Instance!.TeamPracticeValorReward2,
                _ => 0,
            },
            3 => practiceMode switch
            {
                ArenaPracticeMode.Standard => Settings.Instance!.PracticeValorReward3,
                ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeValorReward3,
                ArenaPracticeMode.Team => Settings.Instance!.TeamPracticeValorReward3,
                _ => 0,
            },
            4 => practiceMode switch
            {
                ArenaPracticeMode.Standard => Settings.Instance!.PracticeValorReward4,
                ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeValorReward4,
                ArenaPracticeMode.Team => Settings.Instance!.TeamPracticeValorReward4,
                _ => 0,
            },
            5 => practiceMode switch
            {
                ArenaPracticeMode.Standard => Settings.Instance!.PracticeValorReward5,
                ArenaPracticeMode.Expansive => Settings.Instance!.ExpansivePracticeValorReward5,
                ArenaPracticeMode.Team => Settings.Instance!.TeamPracticeValorReward5,
                _ => 0,
            },
            _ => 0
        };
    }
}