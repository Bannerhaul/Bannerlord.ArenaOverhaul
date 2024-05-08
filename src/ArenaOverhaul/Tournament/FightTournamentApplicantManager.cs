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
            var applicantCharacters = GetAllApplicants(settlement, includePlayer);

            var applicantGroups = applicantCharacters.GroupBy(x => x.GrouppingObject).Select(group => (group.Key, Importance: (int) group.Average(item => item.Importance))).OrderByDescending(x => x.Importance).Take(maximumParticipantCount).ToList();
            var totalImportance = applicantGroups.Sum(x => x.Importance);

            //First we fill using Importance proportions
            foreach (var troopGroup in applicantGroups)
            {
                var participants = applicantCharacters.Where(x => x.GrouppingObject == troopGroup.Key).Take(maximumParticipantCount * troopGroup.Importance / totalImportance).Select(x => x.CharacterObject).ToList();
                participantCharacters.AddRange(participants);
            }

            //Then we fill the rest one by one starting by the least represented group
            applicantGroups = applicantGroups.OrderBy(x => x.Importance).ToList();
            int index = 0;
            int previousIterationCount = -1;
            while (participantCharacters.Count < maximumParticipantCount && previousIterationCount < participantCharacters.Count)
            {
                previousIterationCount = participantCharacters.Count;
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

            if (participantCharacters.Count < maximumParticipantCount)
            {
                //We ran out of unique applicants, so we fill the rest with available troop copies, or randoom troops - if we run out of copies.
                var extraApplicantCharacters = FillUpApplicants(applicantCharacters, settlement, maximumParticipantCount);
                applicantGroups = applicantCharacters.GroupBy(x => x.GrouppingObject).Select(group => (group.Key, Importance: (int) group.Average(item => item.Importance))).OrderByDescending(x => x.Importance).Take(maximumParticipantCount).ToList();
                index = 0;
                while (participantCharacters.Count < maximumParticipantCount)
                {
                    foreach (var troopGroup in applicantGroups)
                    {
                        var participant = applicantCharacters.Where(x => x.GrouppingObject == troopGroup.Key).Skip(index).Take(1).FirstOrDefault();
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
            }

#if v100 || v101 || v102 || v103
            return participantCharacters;
#else
            return new MBList<CharacterObject>(participantCharacters);
#endif
        }

        protected override FightTournamentApplicant GetApplicantInternal(CharacterObject characterObject, MobileParty? originParty, int importance, int availableCount = 1)
        {
            return new FightTournamentApplicant(characterObject, originParty, importance, availableCount);
        }
    }
}