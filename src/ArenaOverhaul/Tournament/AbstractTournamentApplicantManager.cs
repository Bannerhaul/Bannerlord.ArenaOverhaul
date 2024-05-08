using System.Collections.Generic;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace ArenaOverhaul.Tournament
{
    public abstract class AbstractTournamentApplicantManager
    {
        protected const int DefaultPlayerImportance = 200000;
        protected const int DefaultPartyLeaderImportance = 100000;
        protected const int DefaultHeroImportance = 10000;
        protected const int HeroImportanceStep = 2000;
        protected const int TroopImportanceStep = 5000;

        private const float DefaultHeroParticipantsCount = 10f;

        protected static int GetImportance(CharacterObject character, bool isPartyLeader)
        {
            bool isHighStatus = isPartyLeader || (character.IsHero && character.HeroObject.IsLord);
            int baseImportance = character.IsHero ? DefaultHeroImportance + HeroImportanceStep * character.Level : character.Tier * TroopImportanceStep;
            return (character.IsPlayerCharacter ? DefaultPlayerImportance : 0) + (isHighStatus ? DefaultPartyLeaderImportance : 0) + GetRandomizedImportance(baseImportance);
        }

        private static int GetRandomizedImportance(int baseImportance)
        {
            return MBRandom.RandomInt(2 * baseImportance / 3, 4 * baseImportance / 3);
        }

        public static bool IsRightTypeOfHero(Hero hero, bool allowNotables = false) =>
            hero != null && !hero.IsWounded && !hero.IsNoncombatant && !hero.IsChild && (hero.IsLord || hero.IsWanderer || (allowNotables && hero.IsNotable));

        protected static int GetNonHeroParticipantsCount(int applicantHeroCount, int maximumParticipantCount)
        {
            if (applicantHeroCount >= maximumParticipantCount)
            {
                return MBRandom.RandomInt(0, (int) MathF.Max(maximumParticipantCount - (DefaultHeroParticipantsCount + applicantHeroCount / applicantHeroCount), 1f));
            }
            return maximumParticipantCount - applicantHeroCount;
        }
    }

    public abstract class AbstractTournamentApplicantManager<Ta, Tg> : AbstractTournamentApplicantManager
        where Ta : AbstractTournamentApplicant<Tg>
        where Tg : MBObjectBase
    {
        protected List<Ta> GetAllApplicants(Settlement settlement, int requiredCount, bool includePlayer = true)
        {
            List<Ta> applicantCharacters = new();

            AddPlayerParyToApplicants(settlement, applicantCharacters, includePlayer);
            AddOtherPartiesToApplicants(settlement, applicantCharacters);
            AddPartylessHeroesToApplicants(settlement, applicantCharacters);
            FillUpWithRandomTroops(settlement, applicantCharacters, requiredCount);

            return applicantCharacters.OrderByDescending(x => x.Importance).ToList();
        }

        public virtual void AddPlayerParyToApplicants(Settlement settlement, List<Ta> applicantCharacters, bool includePlayer = true)
        {
            if (!includePlayer)
            {
                return;
            }
            applicantCharacters.Add(GetPlayerApplicant());

            MobileParty? mainParty = Hero.MainHero.PartyBelongedTo;
            if (mainParty is null || mainParty.Party?.MemberRoster is null || !settlement.Parties.Contains(mainParty))
            {
                return;
            }

            foreach (TroopRosterElement troopRosterElement in mainParty.Party.MemberRoster.GetTroopRoster())
            {
                if (CanBeAParticipant(troopRosterElement.Character, applicantCharacters, true))
                {
                    applicantCharacters.Add(GetApplicant(troopRosterElement.Character, mainParty, false));
                }
            }
        }

        public virtual void AddOtherPartiesToApplicants(Settlement settlement, List<Ta> applicantCharacters)
        {
            foreach (var party in settlement.Parties)
            {
                var leaderHero = party.LeaderHero;
                if (leaderHero != null)
                {
                    var leaderCharacter = leaderHero.CharacterObject;
                    if (CanBeAParticipant(leaderCharacter, applicantCharacters, true))
                    {
                        applicantCharacters.Add(GetApplicant(leaderCharacter, party, true));
                    }
                }

                if (party.Party?.MemberRoster is null)
                {
                    return;
                }

                foreach (TroopRosterElement troopRosterElement in party.Party.MemberRoster.GetTroopRoster())
                {
                    if (CanBeAParticipant(troopRosterElement.Character, applicantCharacters, true))
                    {
                        applicantCharacters.Add(GetApplicant(troopRosterElement.Character, party, false));
                    }
                }
            }
        }

        public virtual void AddPartylessHeroesToApplicants(Settlement settlement, List<Ta> applicantCharacters)
        {
            for (int index = 0; index < settlement.HeroesWithoutParty.Count; ++index)
            {
                var hero = settlement.HeroesWithoutParty[index];
                var characterObject = hero.CharacterObject;
                if (CanBeAParticipant(characterObject, applicantCharacters, true))
                {
                    if (hero.CurrentSettlement != settlement)
                    {
                        Debug.Print(hero.StringId + " is in settlement.HeroesWithoutParty list but current settlement is not, tournament settlement: " + settlement.StringId);
                    }
                    applicantCharacters.Add(GetApplicant(characterObject, null, false));
                }
            }
        }

        public virtual void FillUpWithRandomTroops(Settlement settlement, List<Ta> applicantCharacters, int requiredCount)
        {
            while (applicantCharacters.Count <= requiredCount)
            {
                CultureObject cultureObject = settlement != null ? settlement.Culture : Game.Current.ObjectManager.GetObject<CultureObject>("empire");
                CharacterObject characterObject = (double) MBRandom.RandomFloat > 0.5 ? cultureObject.BasicTroop : cultureObject.EliteBasicTroop;
                applicantCharacters.Add(GetApplicant(characterObject, null, false));
            }
        }

        public virtual Ta GetApplicant(CharacterObject character, MobileParty? mobileParty, bool isPartyLeader = false)
        {
            var importance = GetImportance(character, isPartyLeader);
            return GetApplicantInternal(character, mobileParty, importance);
        }

        public virtual Ta GetPlayerApplicant()
        {
            var character = CharacterObject.PlayerCharacter;
            var mobileParty = character.HeroObject.PartyBelongedTo;
            var isPartyLeader = mobileParty.LeaderHero == character.HeroObject;

            var importance = GetImportance(character, isPartyLeader);
            return GetApplicantInternal(character, mobileParty, importance);
        }

        protected abstract Ta GetApplicantInternal(CharacterObject characterObject, MobileParty? originParty, int importance);

        public bool CanBeAParticipant(CharacterObject character, List<Ta> applicantCharacters, bool considerSkills, bool allowNotables = false)
        {
            if (!character.IsHero)
                return (!considerSkills || character.Tier >= 3) /*&& !applicantCharacters.Any(participant => participant.CharacterObject == character)*/;

            Hero hero = character.HeroObject;
            return
                IsRightTypeOfHero(hero, allowNotables)
                && hero != Hero.MainHero
                && (!considerSkills || hero.GetSkillValue(DefaultSkills.OneHanded) >= 100 || hero.GetSkillValue(DefaultSkills.TwoHanded) >= 100 || hero.GetSkillValue(DefaultSkills.Polearm) >= 100)
                && !applicantCharacters.Any(applicant => applicant.CharacterObject == hero.CharacterObject);
        }
    }
}