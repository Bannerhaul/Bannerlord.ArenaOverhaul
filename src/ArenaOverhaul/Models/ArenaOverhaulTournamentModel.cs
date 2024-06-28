using ArenaOverhaul.CampaignBehaviors.BehaviorManagers;
using ArenaOverhaul.ModSettings;

using System;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace ArenaOverhaul.Models
{
    public class ArenaOverhaulTournamentModel : TournamentModel
    {
        private readonly TournamentModel _previouslyAssignedModel;

        public ArenaOverhaulTournamentModel(TournamentModel previouslyAssignedModel)
        {
            _previouslyAssignedModel = previouslyAssignedModel;
        }

        public override Equipment GetParticipantArmor(CharacterObject participant)
        {
            var practiceEquipmentSetting = AOArenaBehaviorManager.Instance!.GetParticipantArmorType();
            var practiceEquipment = practiceEquipmentSetting switch
            {
                PracticeEquipmentType.PracticeClothes => GetRandomPracticeClothes(),
                PracticeEquipmentType.CivilianEquipment => participant.RandomCivilianEquipment,
                PracticeEquipmentType.BattleEquipment => participant.RandomBattleEquipment,
                _ => null,
            };

            var equipment = Mission.Current.Mode != MissionMode.Tournament ? practiceEquipment : null;
            return equipment ?? _previouslyAssignedModel.GetParticipantArmor(participant);
        }

        public override TournamentGame CreateTournament(Town town) => _previouslyAssignedModel.CreateTournament(town);

        public override int GetInfluenceReward(Hero winner, Town town) => _previouslyAssignedModel.GetInfluenceReward(winner, town);

        public override int GetNumLeaderboardVictoriesAtGameStart() => _previouslyAssignedModel.GetNumLeaderboardVictoriesAtGameStart();

        public override int GetRenownReward(Hero winner, Town town) => _previouslyAssignedModel.GetRenownReward(winner, town);

        public override (SkillObject skill, int xp) GetSkillXpGainFromTournament(Town town) => _previouslyAssignedModel.GetSkillXpGainFromTournament(town);

        public override float GetTournamentEndChance(TournamentGame tournament) => _previouslyAssignedModel.GetTournamentEndChance(tournament);

        public override float GetTournamentSimulationScore(CharacterObject character) => _previouslyAssignedModel.GetTournamentSimulationScore(character);

        public override float GetTournamentStartChance(Town town) => _previouslyAssignedModel.GetTournamentStartChance(town);

        /* service methods */

        private static Equipment? GetRandomPracticeClothes()
        {
            if (CampaignMission.Current is not { } misson || misson.Mode != MissionMode.Battle || Settlement.CurrentSettlement is not { } settlement || AOArenaBehaviorManager.Instance!.PracticeMode != ArenaPracticeMode.Team)
            {
                return null;
            }

            var settlementCultureId = settlement.MapFaction?.Culture?.StringId ?? "empire";
            var dummyCharacter = Game.Current.ObjectManager.GetObject<CharacterObject>("gear_team_practice_dummy_" + settlementCultureId);
            return dummyCharacter?.RandomBattleEquipment;
        }
    }
}
