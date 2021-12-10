using ArenaOverhaul.CampaignBehaviors;
using ArenaOverhaul.Helpers;

using HarmonyLib;

using SandBox;
using SandBox.Source.Towns;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace ArenaOverhaul.Patches
{
    [HarmonyPatch(typeof(ArenaMaster))]
    public static class ArenaMasterPatch
    {
        private static readonly MethodInfo miSetStandardPracticeMode = AccessTools.Method(typeof(ArenaMasterPatch), "SetStandardPracticeMode");

        [HarmonyPrefix]
        [HarmonyPatch("conversation_arena_master_post_fight_on_condition")]
        public static bool PostFightPrefix(ref bool __result) //Bool prefixes compete with each other and skip others, as well as original, if return false
        {
            ArenaPracticeFightMissionController? missionBehavior = Mission.Current?.GetMissionBehavior<ArenaPracticeFightMissionController>();
            if (CharacterObject.OneToOneConversationCharacter.Occupation != Occupation.ArenaMaster || !Settlement.CurrentSettlement.IsTown || (missionBehavior == null || !missionBehavior.AfterPractice))
            {
                __result = false;
                return false;
            }

            missionBehavior.AfterPractice = false;
            int countBeatenByPlayer = missionBehavior.OpponentCountBeatenByPlayer;
            int remainingOpponentCount = missionBehavior.RemainingOpponentCountFromLastPractice;

            var (PrizeAmount, Explanation, ValorPrizeAmount) = PracticePrizeManager.GetPrizeAmountExplained(remainingOpponentCount, countBeatenByPlayer);
            MBTextManager.SetTextVariable("PRIZE", PrizeAmount);
            MBTextManager.SetTextVariable("VALOR_PRIZE", ValorPrizeAmount);
            MBTextManager.SetTextVariable("OPPONENT_COUNT", countBeatenByPlayer);
            MBTextManager.SetTextVariable("FIGHT_DEBRIEF", Explanation, false);
            if (PrizeAmount > 0)
            {
                GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, PrizeAmount, true);
                MBTextManager.SetTextVariable("GOLD_AMOUNT", PrizeAmount);
                InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_quest_gold_reward_msg", null).ToString(), "event:/ui/notification/coins_positive"));
            }
            Mission.Current!.SetMissionMode(MissionMode.Conversation, false);

            __result = true;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("conversation_arena_practice_fight_explain_reward_on_condition")]
        public static bool ExplainPracticeRewardPrefix(ref bool __result) //Bool prefixes compete with each other and skip others, as well as original, if return false
        {
            PracticePrizeManager.ExplainPracticeReward();
            __result = true;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("conversation_arena_join_fight_on_consequence")]
        public static void ConversationJoinFightPostfix()
        {
            AOArenaBehavior currentAOArenaBehavior = Campaign.Current.CampaignBehaviorManager.GetBehavior<AOArenaBehavior>()!;
            currentAOArenaBehavior.PayForWeaponLoadout();
        }

        [HarmonyPostfix]
        [HarmonyPatch("game_menu_enter_practice_fight_on_consequence")]
        public static void GameMenuJoinFightPostfix()
        {
            SetStandardPracticeMode();
        }

        [HarmonyTranspiler]
        [HarmonyPatch("AddDialogs")]
        public static IEnumerable<CodeInstruction> AddDialogsTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new(instructions);
            int numberOfEdits = 0;
            int indexToSetStandardPracticeMode = 0;
            int indexOfNewobjOperand = 0;
            for (int i = 0; i < codes.Count; ++i)
            {
                if (numberOfEdits == 0 && i > 2 && codes[i - 2].opcode == OpCodes.Ldstr && codes[i - 2].operand.ToString() == "arena_training_practice_fight_intro_1a" && codes[i].opcode == OpCodes.Ldstr && codes[i].operand.ToString() == "arena_intro_4")
                {
                    codes[i].operand = "arena_expansive_practice_fight_rules";
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 1 && codes[i].LoadsConstant(100) && i > 6 && codes[i - 6].opcode == OpCodes.Ldstr && codes[i - 6].operand.ToString() == "arena_training_practice_fight_intro_4")
                {
                    codes[i].operand = 90;
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 2 && codes[i].LoadsConstant(100) && codes[i - 8].opcode == OpCodes.Ldstr && codes[i - 8].operand.ToString() == "arena_master_sign_up_tournament")
                {
                    codes[i].operand = 110;
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 3 && codes[i].LoadsConstant(100) && codes[i - 6].opcode == OpCodes.Ldstr && codes[i - 6].operand.ToString() == "arena_master_ask_for_practice_fight_fight")
                {
                    codes[i].operand = 109;
                    indexToSetStandardPracticeMode = i;
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 4 && codes[i].opcode == OpCodes.Newobj && codes[i - 6].opcode == OpCodes.Ldstr && codes[i - 6].operand.ToString() == "arena_master_enter_practice_fight_confirm" && codes[i - 7].opcode == OpCodes.Ldstr && codes[i - 7].operand.ToString() == "arena_master_enter_practice_fight_confirm")
                {
                    indexOfNewobjOperand = i;
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 5 && codes[i].LoadsConstant(100) && codes[i - 8].opcode == OpCodes.Ldstr && codes[i - 8].operand.ToString() == "arena_join_fight")
                {
                    codes[i].operand = 50;
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 6 && codes[i].LoadsConstant(100) && codes[i - 4].opcode == OpCodes.Ldstr && codes[i - 4].operand.ToString() == "arena_master_practice_fight_reject" && codes[i - 6].opcode == OpCodes.Ldstr && codes[i - 6].operand.ToString() == "2593")
                {
                    codes[i].operand = 10;
                    ++numberOfEdits;
                    break;
                }
            }
            //Logging
            if (indexToSetStandardPracticeMode <= 0 || indexOfNewobjOperand <= 0 || numberOfEdits < 7)
            {
                LogNoHooksIssue(indexToSetStandardPracticeMode, indexOfNewobjOperand, numberOfEdits, codes);
                if (numberOfEdits < 7)
                {
                    MessageHelper.ErrorMessage("Harmony transpiler for ArenaMaster. AddDialogs was not able to make all required changes!");
                }
            }
            if (indexToSetStandardPracticeMode > 0 && indexOfNewobjOperand > 0 )
            {
                codes.InsertRange(indexToSetStandardPracticeMode, new CodeInstruction[] { new CodeInstruction(opcode: OpCodes.Ldftn, operand: miSetStandardPracticeMode), new CodeInstruction(opcode: OpCodes.Newobj, operand: codes[indexOfNewobjOperand].operand) });
            }
            else
            {
                MessageHelper.ErrorMessage("Harmony transpiler for ArenaMaster. AddDialogs could not find code hooks for setting standard practice mode!");
            }
            return codes.AsEnumerable();

            //local methods
            static void LogNoHooksIssue(int indexToSetStandardPracticeMode, int indexOfNewobjOperand, int numberOfEdits, List<CodeInstruction> codes)
            {
                LoggingHelper.Log("Indexes:", "Transpiler for ArenaMaster.AddDialogs");
                StringBuilder issueInfo = new("");
                issueInfo.Append($"\tindexToSetStandardPracticeMode={indexToSetStandardPracticeMode}.\n\tindexOfNewobjOperand={indexOfNewobjOperand}.");
                issueInfo.Append($"\nNumberOfEdits: {numberOfEdits}");
                issueInfo.Append($"\nMethodInfos:");
                issueInfo.Append($"\n\tmiSetStandardPracticeMode={(miSetStandardPracticeMode != null ? miSetStandardPracticeMode.ToString() : "not found")}");
                LoggingHelper.LogILAndPatches(codes, issueInfo, MethodBase.GetCurrentMethod());
                LoggingHelper.Log(issueInfo.ToString());
            }
        }

        /* service methods */
        private static void SetStandardPracticeMode()
        {
            AOArenaBehavior currentAOArenaBehavior = Campaign.Current.CampaignBehaviorManager.GetBehavior<AOArenaBehavior>()!;
            currentAOArenaBehavior.SetStandardPracticeMode();
        }
    }
}
