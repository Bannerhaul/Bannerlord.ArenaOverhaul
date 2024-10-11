using System.Collections.Generic;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace ArenaOverhaul.TeamTournament
{
    public static class TeamTournamentHelpers
    {
        /// <summary>
        /// Returns all found characters which are heroes inside a settlement.
        /// </summary>
        /// <param name="settlement">Settlement to find the heroes in</param>
        /// <returns>List of all found hero characters inside the settlement</returns>
        public static IEnumerable<CharacterObject> GetHeroesInSettlement(this Settlement settlement)
        {
            return settlement.LocationComplex
              .GetListOfCharacters()
              .Where(x =>
                  x != null
                  && x.Character.IsHero
                  && !x.IsHidden)
              .Select(sel => sel.Character);
        }

        public static IEnumerable<CharacterObject> GetCombatantHeroesInSettlement(this Settlement settlement)
        {
            return settlement.GetHeroesInSettlement().Where(x => x.CanBeAParticipant(true, false));
        }

        public static IEnumerable<Hero> AllLivingRelatedHeroes(this Hero inHero)
        {
            if (inHero.Father != null && !inHero.Father.IsDead)
                yield return inHero.Father;

            if (inHero.Mother != null && !inHero.Mother.IsDead)
                yield return inHero.Mother;

            if (inHero.Spouse != null && !inHero.Spouse.IsDead)
                yield return inHero.Spouse;

            foreach (Hero hero in inHero.Children.Where(x => !x.IsDead))
                yield return hero;

            foreach (Hero hero2 in inHero.Siblings.Where(x => !x.IsDead))
                yield return hero2;

            foreach (Hero hero3 in inHero.ExSpouses.Where(x => !x.IsDead))
                yield return hero3;
        }

        public static bool CanBeAParticipant(this CharacterObject character, bool considerSkills, bool allowNotables = false)
        {
            if (!character.IsHero)
                return !considerSkills || character.Tier >= 3;

            Hero heroObject = character.HeroObject;
            return
                !heroObject.IsChild && !heroObject.IsNoncombatant && !heroObject.IsWounded && !heroObject.IsPrisoner
                && (allowNotables || !heroObject.IsNotable)
                && (!considerSkills || heroObject.GetSkillValue(DefaultSkills.OneHanded) >= 100 || heroObject.GetSkillValue(DefaultSkills.TwoHanded) >= 100 || heroObject.GetSkillValue(DefaultSkills.Polearm) >= 100);
        }

        public static Town GetCurrentTown() => Settlement.CurrentSettlement.Town;

        public static bool IsTournamentActive => GetCurrentTown().HasTournament;
    }
}