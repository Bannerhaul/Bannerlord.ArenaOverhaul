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

        protected const int DefaultTroopImportance = 15000;
        protected const int TroopImportanceStep = 2000;

        private const float DefaultHeroParticipantsCount = 10f;

        protected static int GetImportance(CharacterObject character, bool isPartyLeader)
        {
            bool isHighStatus = isPartyLeader || (character.IsHero && character.HeroObject.IsLord);
            int baseImportance = character.IsHero ? DefaultHeroImportance + HeroImportanceStep * character.Level : DefaultTroopImportance + TroopImportanceStep * character.Tier;
            return (character.IsPlayerCharacter ? DefaultPlayerImportance : 0) + (isHighStatus ? DefaultPartyLeaderImportance : 0) + GetRandomizedImportance(baseImportance);
        }

        private static int GetRandomizedImportance(int baseImportance)
        {
            return MBRandom.RandomInt(3 * baseImportance / 4, 5 * baseImportance / 4);
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

        protected static void GetUpgradeTargets(CharacterObject troop, List<CharacterObject> list)
        {
            if (!list.Contains(troop))
            {
                list.Add(troop);
            }
            foreach (CharacterObject upgradeTarget in troop.UpgradeTargets)
            {
                GetUpgradeTargets(upgradeTarget, list);
            }
        }
    }

    public abstract class AbstractTournamentApplicantManager<Ta, Tg> : AbstractTournamentApplicantManager
        where Ta : AbstractTournamentApplicant<Tg>
        where Tg : MBObjectBase
    {
        protected List<Ta> GetAllApplicants(Settlement settlement, bool includePlayer = true)
        {
            List<Ta> applicantCharacters = new();

            AddPlayerParyToApplicants(settlement, applicantCharacters, includePlayer);
            AddOtherPartiesToApplicants(settlement, applicantCharacters);
            AddPartylessHeroesToApplicants(settlement, applicantCharacters);

            return applicantCharacters.OrderByDescending(x => x.Importance).ToList();
        }

        protected List<Ta> FillUpApplicants(List<Ta> applicantCharacters, Settlement settlement, int requiredCount)
        {
            var extraApplicantCharacters = AddExtraTroops(applicantCharacters);
            FillUpWithRandomTroops(settlement, extraApplicantCharacters, requiredCount);

            return extraApplicantCharacters.OrderByDescending(x => x.Importance).ToList();
        }

        public virtual void AddPlayerParyToApplicants(Settlement settlement, List<Ta> applicantCharacters, bool includePlayer = true)
        {
            if (!includePlayer || Hero.MainHero.IsPrisoner)
            {
                return;
            }
            applicantCharacters.Add(GetPlayerApplicant());

            MobileParty? mainParty = Hero.MainHero.PartyBelongedTo;
            if (mainParty is null || mainParty.Party?.MemberRoster is null)
            {
                return;
            }

            AddApplicantsFromRoster(settlement, applicantCharacters, mainParty);
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

                AddApplicantsFromRoster(settlement, applicantCharacters, party);
            }
        }

        public virtual void AddPartylessHeroesToApplicants(Settlement settlement, List<Ta> applicantCharacters)
        {
            bool allowNotablesParticipation = Settings.Instance!.AllowNotablesParticipation;
            for (int index = 0; index < settlement.HeroesWithoutParty.Count; ++index)
            {
                var hero = settlement.HeroesWithoutParty[index];
                var characterObject = hero.CharacterObject;
                if (CanBeAParticipant(characterObject, applicantCharacters, true, allowNotablesParticipation))
                {
                    if (hero.CurrentSettlement != settlement)
                    {
                        Debug.Print(hero.StringId + " is in settlement.HeroesWithoutParty list but current settlement is not, tournament settlement: " + settlement.StringId);
                    }
                    applicantCharacters.Add(GetApplicant(characterObject, null, false));
                }
            }
        }

        public virtual List<Ta> AddExtraTroops(List<Ta> applicantCharacters)
        {
            List<Ta> extraApplicantCharacters = new();
            var troopsList = applicantCharacters.Where(x => x.AvailableCount > 1).ToList();
            troopsList.ForEach(x =>
            {
                for (int index = x.AvailableCount - 1; index > 0; --index)
                {
                    extraApplicantCharacters.Add(GetApplicant(x.CharacterObject, x.OriginParty, false));
                }
                x.AvailableCount = 1;
            });

            return extraApplicantCharacters;
        }

        public virtual void FillUpWithRandomTroops(Settlement settlement, List<Ta> extraApplicantCharacters, int requiredCount)
        {
            while (extraApplicantCharacters.Count <= requiredCount)
            {
                CultureObject cultureObject = settlement != null ? settlement.Culture : Game.Current.ObjectManager.GetObject<CultureObject>("empire");
                CharacterObject? characterObject = (double) MBRandom.RandomFloat > 0.5 ? cultureObject.BasicTroop : cultureObject.EliteBasicTroop;

                var list = new List<CharacterObject>();
                GetUpgradeTargets(characterObject, list);

                list = list.Where(x => CanBeAParticipant(x, extraApplicantCharacters, true)).ToList();
                list.Shuffle();
                characterObject = list.FirstOrDefault();

                if (characterObject != null && CanBeAParticipant(characterObject, extraApplicantCharacters, true))
                {
                    extraApplicantCharacters.Add(GetApplicant(characterObject, null, false));
                }
            }
        }

        public virtual Ta GetApplicant(CharacterObject character, MobileParty? mobileParty, bool isPartyLeader = false, int availableCount = 1)
        {
            var importance = GetImportance(character, isPartyLeader);
            return GetApplicantInternal(character, mobileParty, importance, availableCount);
        }

        public virtual Ta GetPlayerApplicant()
        {
            var character = CharacterObject.PlayerCharacter;
            var mobileParty = character.HeroObject.PartyBelongedTo;
            var isPartyLeader = mobileParty != null && mobileParty.LeaderHero == character.HeroObject;

            var importance = GetImportance(character, isPartyLeader);
            return GetApplicantInternal(character, mobileParty, importance);
        }

        protected abstract Ta GetApplicantInternal(CharacterObject characterObject, MobileParty? originParty, int importance, int availableCount = 1);

        public bool CanBeAParticipant(CharacterObject character, List<Ta> applicantCharacters, bool considerSkills, bool allowNotables = false)
        {
            if (!character.IsHero)
                return (!considerSkills || character.Tier >= 3);

            Hero hero = character.HeroObject;
            return
                IsRightTypeOfHero(hero, allowNotables)
                && hero != Hero.MainHero
                && (!considerSkills || hero.GetSkillValue(DefaultSkills.OneHanded) >= 100 || hero.GetSkillValue(DefaultSkills.TwoHanded) >= 100 || hero.GetSkillValue(DefaultSkills.Polearm) >= 100)
                && !applicantCharacters.Any(applicant => applicant.CharacterObject == hero.CharacterObject);
        }

        private void AddApplicantsFromRoster(Settlement settlement, List<Ta> applicantCharacters, MobileParty party)
        {
            List<CharacterObject> upgradeTargets = new();
            foreach (TroopRosterElement troopRosterElement in party.Party.MemberRoster.GetTroopRoster())
            {
                ProcessRosterElement(applicantCharacters, upgradeTargets, party, troopRosterElement);
            }
            upgradeTargets = upgradeTargets.Where(x => CanBeAParticipant(x, applicantCharacters, true) && !applicantCharacters.Any(a => a.CharacterObject == x)).ToList();
            upgradeTargets.ForEach(upgradedTroopCaharacter =>
            {
                if ((upgradedTroopCaharacter.Culture != null && upgradedTroopCaharacter.Culture == settlement.Culture ? 1.0f : 0.33f) > MBRandom.RandomFloat)
                {
                    applicantCharacters.Add(GetApplicant(upgradedTroopCaharacter, null, false));
                }
            });
        }

        private void ProcessRosterElement(List<Ta> applicantCharacters, List<CharacterObject> upgradeTargets, MobileParty? party, TroopRosterElement troopRosterElement)
        {
            var character = troopRosterElement.Character;
            if (character.IsHero)
            {
                if (CanBeAParticipant(character, applicantCharacters, true))
                {
                    applicantCharacters.Add(GetApplicant(character, party, false));
                }
            }
            else
            {
                var availableCount = troopRosterElement.Number - troopRosterElement.WoundedNumber;
                if (availableCount > 0 && CanBeAParticipant(character, applicantCharacters, true))
                {
                    applicantCharacters.Add(GetApplicant(character, party, false, availableCount));
                }
                GetUpgradeTargets(character, upgradeTargets);
            }
        }
    }
}