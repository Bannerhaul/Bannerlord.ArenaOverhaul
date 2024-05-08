using System;
using System.Collections.Generic;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Library;

namespace ArenaOverhaul.Tournament
{
    public class FightTournamentApplicantManager : AbstractTournamentApplicantManager<FightTournamentApplicant, MobileParty>
    {
#if v100 || v101 || v102 || v103
        public List<CharacterObject> GetParticipantCharacters(FightTournamentGame instance, Settlement settlement, bool includePlayer = true)
        {
#else
        public MBList<CharacterObject> GetParticipantCharacters(FightTournamentGame instance, Settlement settlement, bool includePlayer = true)
        {
#endif
            List<CharacterObject> participantCharacters = new();

            int maximumParticipantCount = instance.MaximumParticipantCount;
            var applicantCharacters = GetAllApplicants(settlement, maximumParticipantCount, includePlayer);

            var totalImportance = applicantCharacters.Sum(x => x.Importance);
            var applicantGroups = applicantCharacters.GroupBy(x => x.GrouppingObject).Select(group => (group.Key, Importance: group.Sum(item => item.Importance))).OrderByDescending(x => x.Importance).Take(maximumParticipantCount).ToList();

            //First we fill using Importance proportions
            foreach (var troopGroup in applicantGroups)
            {
                var participants = applicantCharacters.Where(x => x.GrouppingObject == troopGroup.Key).Take(maximumParticipantCount * troopGroup.Importance / totalImportance).Select(x => x.CharacterObject).ToList();
                participantCharacters.AddRange(participants);
            }

            //Then we fill the rest one by one starting by the least represented group
            applicantGroups = applicantGroups.OrderBy(x => x.Importance).ToList();
            int index = 0;
            while (participantCharacters.Count < maximumParticipantCount)
            {
                foreach (var troopGroup in applicantGroups)
                {
                    var participant = applicantCharacters.Where(x => x.GrouppingObject == troopGroup.Key).Skip(index + maximumParticipantCount * troopGroup.Importance / totalImportance).Take(1).FirstOrDefault();
                    if (participant != null)
                    {
                        participantCharacters.Add(participant.CharacterObject);
                    }
                    if (participantCharacters.Count == maximumParticipantCount)
                    {
                        break;
                    }
                }
                index++;
            }

#if v100 || v101 || v102 || v103
            return participantCharacters;
#else
            return new MBList<CharacterObject>(participantCharacters);
#endif
        }

        protected override FightTournamentApplicant GetApplicantInternal(CharacterObject characterObject, MobileParty? originParty, int importance)
        {
            return new FightTournamentApplicant(characterObject, originParty, importance);
        }
    }
}