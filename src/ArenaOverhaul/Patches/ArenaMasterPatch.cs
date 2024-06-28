using ArenaOverhaul.ArenaPractice;
using ArenaOverhaul.CampaignBehaviors.BehaviorManagers;
using ArenaOverhaul.Helpers;

using HarmonyLib;

using SandBox.CampaignBehaviors;
using SandBox.Missions.MissionLogics.Arena;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace ArenaOverhaul.Patches
{
    [HarmonyPatch(typeof(ArenaMasterCampaignBehavior))]
    public static class ArenaMasterPatch
    {
        private static readonly MethodInfo? miSetStandardPracticeMode = AccessTools.Method(typeof(ArenaMasterPatch), "SetStandardPracticeMode");
        private static readonly MethodInfo? miCheckRematchIsAffordable = AccessTools.Method(typeof(ArenaMasterPatch), "CheckRematchIsAffordable");
        private static readonly MethodInfo? miGetMissionMode = AccessTools.Method(typeof(ArenaMasterPatch), "GetMissionMode");
        private static readonly MethodInfo? miResetAfterPracticeTalkFlag = AccessTools.Method(typeof(ArenaMasterPatch), "ResetAfterPracticeTalkFlag");

        private static readonly MethodInfo? miSetMissionMode = AccessTools.Method(typeof(Mission), "SetMissionMode");

        [HarmonyTranspiler]
        [HarmonyPatch("AfterMissionStarted")]
        public static IEnumerable<CodeInstruction> AfterMissionStartedTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = new(instructions);
            int numberOfEdits = 0;
            if (miSetMissionMode != null && miGetMissionMode != null)
            {
                for (int i = 0; i < codes.Count; ++i)
                {
                    if (codes[i].LoadsConstant(2) && codes[i + 1].LoadsConstant(1) && codes[i + 2].Calls(miSetMissionMode))
                    {
                        codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetMissionMode);
                        ++numberOfEdits;
                        break;
                    }
                }
            }

            //Logging
            const int RequiredNumberOfEdits = 1;
            if (numberOfEdits < RequiredNumberOfEdits)
            {
                LoggingHelper.LogNoHooksIssue(
                    codes, numberOfEdits, RequiredNumberOfEdits, __originalMethod, [],
                    [
                        (nameof(miSetMissionMode), miSetMissionMode),
                        (nameof(miGetMissionMode), miGetMissionMode),
                    ]);
            }

            return codes.AsEnumerable();
        }

        [HarmonyTranspiler]
        [HarmonyPatch("StartPlayerPracticeAfterConversationEnd")]
        public static IEnumerable<CodeInstruction> StartPlayerPracticeAfterConversationEndTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = new(instructions);
            int numberOfEdits = 0;
            if (miGetMissionMode != null && miSetMissionMode != null)
            {
                for (int i = 0; i < codes.Count; ++i)
                {
                    if (codes[i].LoadsConstant(2) && codes[i + 1].LoadsConstant(0) && codes[i + 2].Calls(miSetMissionMode))
                    {
                        codes[i] = new CodeInstruction(opcode: OpCodes.Call, operand: miGetMissionMode);
                        ++numberOfEdits;
                        break;
                    }
                }
            }

            //Logging
            const int RequiredNumberOfEdits = 1;
            if (numberOfEdits < RequiredNumberOfEdits)
            {
                LoggingHelper.LogNoHooksIssue(
                    codes, numberOfEdits, RequiredNumberOfEdits, __originalMethod, [],
                    [
                        (nameof(miSetMissionMode), miSetMissionMode),
                        (nameof(miGetMissionMode), miGetMissionMode),
                    ]);
            }

            return codes.AsEnumerable();
        }

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
            if (AOArenaBehaviorManager.Instance != null)
            {
                AOArenaBehaviorManager.Instance.IsAfterPracticeTalk = true;
            }
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
            AOArenaBehaviorManager.Instance?.PayForPracticeMatch();
        }

        [HarmonyPostfix]
        [HarmonyPatch("game_menu_enter_practice_fight_on_consequence")]
        public static void GameMenuJoinFightPostfix()
        {
            SetStandardPracticeMode();
            AOArenaBehaviorManager.Instance?.PayForPracticeMatch();
        }

        [HarmonyPostfix]
        [HarmonyPatch("game_menu_enter_practice_fight_on_condition")]
        public static void GameMenuJoinFightConditionPostfix(MenuCallbackArgs args)
        {
            if (!args.IsEnabled || AOArenaBehaviorManager.Instance is not AOArenaBehaviorManager behaviorManager)
            {
                return;
            }
            if (!behaviorManager.CheckAffordabilityForNextPracticeRound(ArenaPracticeMode.Standard, 0, out args.Tooltip, true))
            {
                args.IsEnabled = false;
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch("AddDialogs")]
        public static IEnumerable<CodeInstruction> AddDialogsTranspiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = new(instructions);
            int numberOfEdits = 0;
            int indexToSetStandardPracticeMode = 0;
            int indexToCheckRematchIsAffordable = 0;
            int indexToCheckRematchIsAffordable2 = 0;
            int indexToResetAfterPracticeTalkFlag = 0;
            int indexOfOnConsequenceNewobjOperand = 0;
            int indexOfOnClickableConditionNewobjOperand = 0;
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
                    if (codes[i + 3].opcode == OpCodes.Newobj)
                    {
                        indexOfOnClickableConditionNewobjOperand = i + 3;
                        ++numberOfEdits;
                    }
                }
                else if (numberOfEdits == 5 && codes[i].opcode == OpCodes.Newobj && codes[i - 6].opcode == OpCodes.Ldstr && codes[i - 6].operand.ToString() == "arena_master_enter_practice_fight_confirm" && codes[i - 7].opcode == OpCodes.Ldstr && codes[i - 7].operand.ToString() == "arena_master_enter_practice_fight_confirm")
                {
                    indexOfOnConsequenceNewobjOperand = i;
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 6 && codes[i].LoadsConstant(100) && codes[i + 1].opcode == OpCodes.Ldnull && codes[i + 2].opcode == OpCodes.Ldnull)
                {
                    indexToCheckRematchIsAffordable = i + 2;
                    ++numberOfEdits;
                }
                else if (numberOfEdits == 7 && codes[i].LoadsConstant(100) && codes[i - 8].opcode == OpCodes.Ldstr && codes[i - 8].operand.ToString() == "arena_join_fight")
                {
                    codes[i].operand = 50;
                    ++numberOfEdits;
                    if (codes[i + 1].opcode == OpCodes.Ldnull)
                    {
                        indexToCheckRematchIsAffordable2 = i + 2; //we keep the Ldnull for static call
                        ++numberOfEdits;
                    }
                }
                else if (numberOfEdits == 9 && codes[i].LoadsConstant(100) && codes[i - 4].opcode == OpCodes.Ldstr && codes[i - 4].operand.ToString() == "arena_master_practice_fight_reject" && codes[i - 6].opcode == OpCodes.Ldstr && codes[i - 6].operand.ToString() == "2593")
                {
                    codes[i].operand = 10;
                    ++numberOfEdits;
                    if (codes[i - 1].opcode == OpCodes.Ldnull)
                    {
                        indexToResetAfterPracticeTalkFlag = i; //we keep the Ldnull for static call
                        ++numberOfEdits;
                    }
                    break;
                }
            }
            //Logging
            const int RequiredNumberOfEdits = 11;
            if (indexToSetStandardPracticeMode <= 0 || indexOfOnConsequenceNewobjOperand <= 0 || indexToCheckRematchIsAffordable <= 0 || indexToCheckRematchIsAffordable2 <= 0 || indexToResetAfterPracticeTalkFlag <= 0 || indexOfOnClickableConditionNewobjOperand <= 0
                || numberOfEdits < RequiredNumberOfEdits || miCheckRematchIsAffordable is null || miSetStandardPracticeMode is null || miResetAfterPracticeTalkFlag is null)
            {
                LoggingHelper.LogNoHooksIssue(
                    codes, numberOfEdits, RequiredNumberOfEdits, __originalMethod,
                    [
                        (nameof(indexToSetStandardPracticeMode), indexToSetStandardPracticeMode),
                        (nameof(indexToCheckRematchIsAffordable), indexToCheckRematchIsAffordable),
                        (nameof(indexToCheckRematchIsAffordable2), indexToCheckRematchIsAffordable2),
                        (nameof(indexToResetAfterPracticeTalkFlag), indexToResetAfterPracticeTalkFlag),
                        (nameof(indexOfOnConsequenceNewobjOperand), indexOfOnConsequenceNewobjOperand),
                        (nameof(indexOfOnClickableConditionNewobjOperand), indexOfOnClickableConditionNewobjOperand),

                    ],
                    [
                        (nameof(miCheckRematchIsAffordable), miCheckRematchIsAffordable),
                        (nameof(miSetStandardPracticeMode), miSetStandardPracticeMode),
                        (nameof(miResetAfterPracticeTalkFlag), miResetAfterPracticeTalkFlag),
                    ]);
            }

            if (indexToResetAfterPracticeTalkFlag > 0 && indexOfOnConsequenceNewobjOperand > 0)
            {
                codes.InsertRange(indexToResetAfterPracticeTalkFlag, [new CodeInstruction(opcode: OpCodes.Ldftn, operand: miResetAfterPracticeTalkFlag), new CodeInstruction(opcode: OpCodes.Newobj, operand: codes[indexOfOnConsequenceNewobjOperand].operand)]);
            }
            else
            {
                MessageHelper.ErrorMessage("Harmony transpiler for ArenaMasterCampaignBehavior. AddDialogs could not find code hooks for adding rematch affordability check!");
            }
            if (indexToCheckRematchIsAffordable2 > 0 && indexOfOnClickableConditionNewobjOperand > 0)
            {
                codes.InsertRange(indexToCheckRematchIsAffordable2, [new CodeInstruction(opcode: OpCodes.Ldftn, operand: miCheckRematchIsAffordable), new CodeInstruction(opcode: OpCodes.Newobj, operand: codes[indexOfOnClickableConditionNewobjOperand].operand)]);
            }
            else
            {
                MessageHelper.ErrorMessage("Harmony transpiler for ArenaMasterCampaignBehavior. AddDialogs could not find code hooks for adding rematch affordability check!");
            }
            if (indexToCheckRematchIsAffordable > 0 && indexOfOnClickableConditionNewobjOperand > 0)
            {
                codes.InsertRange(indexToCheckRematchIsAffordable, [new CodeInstruction(opcode: OpCodes.Ldftn, operand: miCheckRematchIsAffordable), new CodeInstruction(opcode: OpCodes.Newobj, operand: codes[indexOfOnClickableConditionNewobjOperand].operand)]);
            }
            else
            {
                MessageHelper.ErrorMessage("Harmony transpiler for ArenaMasterCampaignBehavior. AddDialogs could not find code hooks for adding match affordability check!");
            }
            if (indexToSetStandardPracticeMode > 0 && indexOfOnConsequenceNewobjOperand > 0)
            {
                codes.InsertRange(indexToSetStandardPracticeMode, [new CodeInstruction(opcode: OpCodes.Ldftn, operand: miSetStandardPracticeMode), new CodeInstruction(opcode: OpCodes.Newobj, operand: codes[indexOfOnConsequenceNewobjOperand].operand)]);
            }
            else
            {
                MessageHelper.ErrorMessage("Harmony transpiler for ArenaMasterCampaignBehavior. AddDialogs could not find code hooks for setting standard practice mode!");
            }

            return codes.AsEnumerable();
        }

        /* service methods */
        private static void SetStandardPracticeMode()
        {
            AOArenaBehaviorManager.Instance?.SetStandardPracticeMode();
        }

        internal static bool CheckRematchIsAffordable(out TextObject? explanation)
        {
            explanation = null;
            return AOArenaBehaviorManager.Instance?.CheckAffordabilityForNextPracticeRound(out explanation) ?? true;
        }

        internal static MissionMode GetMissionMode()
        {
            return AOArenaBehaviorManager.Instance?.GetArenaPracticeMissionMode() ?? MissionMode.Battle;
        }

        internal static void ResetAfterPracticeTalkFlag()
        {
            if (AOArenaBehaviorManager.Instance is null)
            {
                return;
            }
            AOArenaBehaviorManager.Instance!.IsAfterPracticeTalk = false;
        }
    }
}