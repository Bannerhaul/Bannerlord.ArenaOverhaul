using ArenaOverhaul.Helpers;
using ArenaOverhaul.Tournament;

using SandBox;
using SandBox.Tournaments;
using SandBox.Tournaments.MissionLogics;

using System;
using System.Collections.Generic;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace ArenaOverhaul.TeamTournament
{
    public class TeamTournamentMissionController : MissionLogic, ITournamentGameBehavior
    {
        private const float XpShareForDamage = 0.5f;

        public override void AfterStart()
        {
            TournamentBehavior.DeleteTournamentSetsExcept(Mission.Scene.FindEntityWithTag("tournament_fight"));
            _spawnPoints = new List<GameEntity>();

            for (int i = 0; i < 4; i++)
            {
                var gameEntity = Mission.Scene.FindEntityWithTag("sp_arena_" + (i + 1));
                if (gameEntity != null)
                    _spawnPoints.Add(gameEntity);
            }

            if (_spawnPoints.Count < 4)
                _spawnPoints = Mission.Scene.FindEntitiesWithTag("sp_arena").ToList<GameEntity>();
        }

        private void PrepareForMatch() // also called from skip SkipMatch
        {
            var numMembers = MBMath.ClampInt(_match!.Teams.Max(x => x.Members.Count()), 1, 4);
            var teamWeaponEquipmentList = GetTeamWeaponEquipmentList(numMembers);
            numMembers = Math.Min(numMembers, teamWeaponEquipmentList.Count);

            foreach (var team in _match.Teams)
            {
                int num = 0;
                foreach (var tournamentMember in team.Members)
                {
                    tournamentMember.MatchEquipment = teamWeaponEquipmentList[num].Clone(false);
                    AddRandomClothes(tournamentMember);
                    num = ++num % numMembers;
                }
            }
        }

        public void StartMatch(TeamTournamentMatch match, bool isLastRound)
        {
            _match = match;
            _isLastRound = isLastRound;
            PrepareForMatch();
            Mission.SetMissionMode(MissionMode.Battle, true);
            var tmpList = new List<Team>();
            int count = _spawnPoints!.Count;
            foreach (var tournamentTeam in _match.Teams)
            {
                var side = tournamentTeam.IsPlayerTeam ? BattleSideEnum.Defender : BattleSideEnum.Attacker;
                var team = Mission.Teams.Add(side, tournamentTeam.TeamColor, uint.MaxValue, tournamentTeam.TeamBanner, true, false, true);
                var spawnPoint = _spawnPoints[tmpList.Count % count];

                foreach (var tournamentMember in tournamentTeam.Members)
                    SpawnTournamentMember(spawnPoint, tournamentMember, team);

                tmpList.ForEach(x => x.SetIsEnemyOf(team, true));
                tmpList.Add(team);
            }
            _aliveMembers = new List<TeamTournamentMember>(_match.MatchMembers);
            _aliveTeams = new List<TeamTournamentTeam>(_match.Teams);
        }

        private void SpawnTournamentMember(GameEntity spawnPoint, TeamTournamentMember member, Team team)
        {
            MatrixFrame globalFrame = spawnPoint.GetGlobalFrame();
            globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
            SpawnAgentWithRandomItems(member, team, globalFrame);
        }

        private List<Equipment> GetTeamWeaponEquipmentList(int teamSize)
        {
            var retList = new List<Equipment>();
            var culture = PlayerEncounter.EncounterSettlement.Culture;
            var equiptmentSet = teamSize == 4 ? culture.TournamentTeamTemplatesForFourParticipant : (teamSize == 2 ? culture.TournamentTeamTemplatesForTwoParticipant : culture.TournamentTeamTemplatesForOneParticipant);
            CharacterObject characterObject;

            if (equiptmentSet.Count > 0)
                characterObject = equiptmentSet[MBRandom.RandomInt(equiptmentSet.Count)];
            else
                characterObject = teamSize == 4 ? _defaultWeaponTemplatesIdTeamSizeFour : (teamSize == 2 ? _defaultWeaponTemplatesIdTeamSizeTwo : _defaultWeaponTemplatesIdTeamSizeOne);

            foreach (var sourceEquipment in characterObject.BattleEquipments)
            {
                var equipment = new Equipment();
                equipment.FillFrom(sourceEquipment, true);
                retList.Add(equipment);
            }
            return retList;
        }

        public void SkipMatch(TeamTournamentMatch match)
        {
            _match = match;
            PrepareForMatch();
            Simulate();
        }

        private void SkipMatch()
        {
            Mission.Current.GetMissionBehavior<TeamTournamentBehavior>().SkipMatch(false);
        }

        public bool IsMatchEnded()
        {
            if (_isSimulated || _match == null)
                return true;

            if ((_endTimer != null && _endTimer.ElapsedTime > 6f) || _forceEndMatch)
            {
                _forceEndMatch = false;
                _endTimer = null;
                return true;
            }

            if (_cheerTimer != null && _cheerTimer.ElapsedTime > 1f)
            {
                OnMatchResultsReady();
                _cheerTimer = null;

                foreach (Agent agent in Mission.Agents)
                {
                    if (agent.IsAIControlled)
                        Mission.GetMissionBehavior<AgentVictoryLogic>().SetTimersOfVictoryReactionsOnTournamentVictoryForAgent(agent, 1f, 3f);
                }

                return false;
            }

            if (_endTimer == null && !CheckIfIsThereAnyEnemies())
            {
                _endTimer = new BasicMissionTimer();
                _cheerTimer = new BasicMissionTimer();
            }

            return false;
        }

        public void OnMatchResultsReady()
        {
            if (!_match!.IsPlayerParticipating)
                MessageHelper.QuickInformationMessage(new TextObject("{=UBd0dEPp}Match is over", null), 0, null, "");
            else if (_match.IsPlayerTeamQualified)
            {
                if (_isLastRound)
                    MessageHelper.QuickInformationMessage(new TextObject("{=wOqOQuJl}Round is over, your team survived the final round of the tournament.", null), 0, null, "");
                else
                    MessageHelper.QuickInformationMessage(new TextObject("{=fkOYvnVG}Round is over, your team is qualified for the next stage of the tournament.", null), 0, null, "");
            }
            else
                MessageHelper.QuickInformationMessage(new TextObject("{=MLyBN51z}Round is over, your team is disqualified from the tournament.", null), 0, null, "");
        }

        public void OnMatchEnded()
        {
            SandBoxHelpers.MissionHelper.FadeOutAgents(Mission.Agents.Where(a => a.IsActive() && (a.Team is null || a.Team.TeamIndex >= 0)), true, false);
            Mission.ClearCorpses(false);
            Mission.Teams.Clear();
            Mission.RemoveSpawnedItemsAndMissiles();
            _match = null;
            _endTimer = null;
            _cheerTimer = null;
            _isSimulated = false;
        }

        private void SpawnAgentWithRandomItems(TeamTournamentMember member, Team team, MatrixFrame frame)
        {
            frame.Strafe((float) MBRandom.RandomInt(-2, 2) * 1f);
            frame.Advance((float) MBRandom.RandomInt(0, 2) * 1f);
            var character = member.Character;
            var agentBuildData = new AgentBuildData(new SimpleAgentOrigin(character, -1, null, member.Descriptor)).Team(team).InitialPosition(frame.origin);
            agentBuildData = agentBuildData.InitialDirection(frame.rotation.f.AsVec2.Normalized()).Equipment(member.MatchEquipment).ClothingColor1(team.Color).Banner(team.Banner).Controller(character.IsPlayerCharacter ? Agent.ControllerType.Player : Agent.ControllerType.AI);
#if v100 || v101 || v102 || v103
            var agent = Mission.SpawnAgent(agentBuildData, false, 0);
#else
            var agent = Mission.SpawnAgent(agentBuildData, false);
#endif

            if (character.IsPlayerCharacter)
            {
                agent.Health = character.HeroObject.HitPoints;
                Mission.PlayerTeam = team;
            }
            else
            {
                agent.SetWatchState(Agent.WatchState.Alarmed);
            }
            agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp);
        }

        private void AddRandomClothes(TeamTournamentMember member)
        {
            var participantArmor = Campaign.Current.Models.TournamentModel.GetParticipantArmor(member.Character);
            for (int i = 5; i < 10; i++)
            {
                var equipmentFromSlot = participantArmor.GetEquipmentFromSlot((EquipmentIndex) i);
                if (equipmentFromSlot.Item != null)
                {
                    member.MatchEquipment!.AddEquipmentToSlotWithoutAgent((EquipmentIndex) i, equipmentFromSlot);
                }
            }
        }

        private void AddScoreToRemainingTeams() => _aliveTeams!.ForEach(x => x.AddScore(1));

        private void AddScoreToKillerTeam(int killerUniqueSeed)
        {
            _aliveTeams!.FirstOrDefault(x => x.Members.Any(m => m.IsCharWithDescriptor(killerUniqueSeed)))?.AddScore(1);
        }

        private void AddLastTeamScore()
        {
            _aliveTeams!.First().AddScore(_match!.Teams.Count());
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
        {
            if (!IsMatchEnded() && affectorAgent != null && affectedAgent != affectorAgent && affectedAgent.IsHuman && affectorAgent.IsHuman)
            {
                var member = _match!.MatchMembers.FirstOrDefault(x => x.IsCharWithDescriptor(affectedAgent.Origin.UniqueSeed));
                if (member is null)
                {
                    return;
                }

                _aliveMembers!.Remove(member);
                member.Team!.IsAlive = _aliveMembers.Any(x => x.Team == member.Team);

                // apply score only if not on same team
                if (affectedAgent.Team != affectorAgent.Team)
                    AddScoreToKillerTeam(affectorAgent.Origin.UniqueSeed);

                if (!member.Team.IsAlive)
                {
                    _aliveTeams!.Remove(member.Team);

                    // give last team a bonus
                    if (_aliveTeams.Count == 1)
                        AddLastTeamScore();
                }
            }
        }

        public override void OnScoreHit(
            Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon,
            bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData,
            float damagedHp, float hitDistance, float shotDifficulty)
        {
            if (affectorAgent != null)
            {
                if (affectorAgent.Character != null && affectedAgent.Character != null)
                {
                    float damage = blow.InflictedDamage;
                    float movementSpeedDamageModifier = blow.MovementSpeedDamageModifier;
                    AgentAttackType attackType = blow.AttackType;
                    if (damage > affectedAgent.HealthLimit)
                        damage = affectedAgent.HealthLimit;

                    EnemyHitReward(affectedAgent, affectorAgent, movementSpeedDamageModifier, shotDifficulty, attackerWeapon, attackType, XpShareForDamage * damage / affectedAgent.HealthLimit, damage);
                }
            }
        }

        private void EnemyHitReward(
          Agent affectedAgent,
          Agent affectorAgent,
          float lastSpeedBonus,
          float lastShotDifficulty,
          WeaponComponentData lastAttackerWeapon,
          AgentAttackType attackType,
          float hitpointRatio,
          float damageAmount)
        {
            CharacterObject affectedCharacter = (CharacterObject) affectedAgent.Character;
            CharacterObject affectorCharacter = (CharacterObject) affectorAgent.Character;
            if (affectedAgent.Origin != null && affectorAgent != null && affectorAgent.Origin != null)
            {
                bool isHorseCharge = affectorAgent.MountAgent != null && attackType == AgentAttackType.Collision;
                SkillLevelingManager.OnCombatHit(
                    affectorCharacter, affectedCharacter,
                    null, null,
                    lastSpeedBonus, lastShotDifficulty, lastAttackerWeapon,
                    hitpointRatio, CombatXpModel.MissionTypeEnum.Tournament,
                    affectorAgent.MountAgent != null, affectorAgent.Team == affectedAgent.Team,
                    false, damageAmount, affectedAgent.Health < 1f, false, isHorseCharge);
            }
            //NoticableTakedowns for renown reward
            if (affectedAgent.Origin == null || affectorAgent == null || affectorAgent.Origin == null)
            {
                return;
            }
            TournamentRewardManager.UpdateNoticableTakedowns(affectorAgent, affectedAgent);
        }

        public bool CheckIfIsThereAnyEnemies()
        {
            Team? team = null;
            foreach (var agent in Mission.Agents.Where(x => x.IsHuman && x.Team != null && x.Team.TeamIndex >= 0))
            {
                if (team == null)
                    team = agent.Team;
                else if (team != agent.Team)
                    return true;
            }
            return false;
        }

        private void Simulate()
        {
            _isSimulated = false;

            if (base.Mission.Agents.Count == 0 || _aliveMembers is null || _aliveTeams is null)
            {
                _aliveMembers = new List<TeamTournamentMember>(_match!.MatchMembers);
                _aliveTeams = new List<TeamTournamentTeam>(_match.Teams);
            }

            var player = _aliveMembers.FirstOrDefault(x => x.IsPlayer);

            // if player is still alive => player quit, remove and take teams score too
            if (player != null)
            {
                foreach (var member in player.Team!.Members)
                {
                    member.ResetScore();
                    _aliveMembers.Remove(member);
                }
                _aliveTeams.Remove(player.Team);
                player.Team?.ResetScore();
                AddScoreToRemainingTeams();
            }

            var simAttacks = new Dictionary<TeamTournamentMember, Tuple<float, float>>();
            foreach (var member in _aliveMembers)
            {
                member.Character.GetSimulationAttackPower(out float item, out float item2, member.MatchEquipment);
                simAttacks.Add(member, new Tuple<float, float>(item, item2));
            }

            int runningIndex = 0;
            while (_aliveMembers.Count > 1 && _aliveTeams!.Count > 1)
            {
                runningIndex = ++runningIndex % _aliveMembers.Count;
                var currentFighter = _aliveMembers[runningIndex];
                int nextIndex;

                TeamTournamentMember nextFighter;
                do
                {
                    nextIndex = MBRandom.RandomInt(_aliveMembers.Count);
                    nextFighter = _aliveMembers[nextIndex];
                }
                while (currentFighter == nextFighter || currentFighter.Team == nextFighter.Team);

                if (simAttacks[nextFighter].Item2 - simAttacks[currentFighter].Item1 > 0f)
                {
                    simAttacks[nextFighter] = new Tuple<float, float>(simAttacks[nextFighter].Item1, simAttacks[nextFighter].Item2 - simAttacks[currentFighter].Item1);
                }
                else
                {
                    simAttacks.Remove(nextFighter);
                    _aliveMembers.Remove(nextFighter);
                    nextFighter.Team!.IsAlive = _aliveMembers.Any(x => x.Team == nextFighter.Team);

                    TournamentRewardManager.UpdateNoticableTakedowns(currentFighter, nextFighter);

                    if (!nextFighter.Team.IsAlive)
                    {
                        _aliveTeams?.Remove(nextFighter.Team);
                        AddScoreToRemainingTeams();
                    }

                    if (nextIndex < runningIndex)
                        runningIndex--;
                }
            }
            _isSimulated = true;
        }

        private bool IsThereAnyPlayerAgent() => Mission.MainAgent != null && base.Mission.MainAgent.IsActive() || Mission.Agents.Any(agent => agent.IsPlayerControlled);

        public override InquiryData? OnEndMissionRequest(out bool canPlayerLeave)
        {
            InquiryData? result = null;
            canPlayerLeave = true;
            //var missionBehaviour = Mission.Current.GetMissionBehavior<TeamTournamentBehavior>();
            if (_match != null) // && missionBehaviour != null)
            {
                if (_match.IsPlayerParticipating)
                {
                    MBTextManager.SetTextVariable("SETTLEMENT_NAME", Hero.MainHero.CurrentSettlement.EncyclopediaLinkWithName, false);
                    if (IsThereAnyPlayerAgent())
                    {
                        if (base.Mission.IsPlayerCloseToAnEnemy(5f))
                        {
                            canPlayerLeave = false;
                            MessageHelper.QuickInformationMessage(GameTexts.FindText("str_can_not_retreat", null), 0, null, "");
                        }
                        else if (CheckIfIsThereAnyEnemies())
                        {
                            result = new InquiryData(GameTexts.FindText("str_tournament", null).ToString(), GameTexts.FindText("str_tournament_forfeit_game", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), new Action(this.SkipMatch), null, "");
                        }
                        else
                        {
                            _forceEndMatch = true;
                            canPlayerLeave = false;
                        }
                    }
                    else if (CheckIfIsThereAnyEnemies())
                    {
                        result = new InquiryData(GameTexts.FindText("str_tournament", null).ToString(), GameTexts.FindText("str_tournament_skip", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), new Action(this.SkipMatch), null, "");
                    }
                    else
                    {
                        _forceEndMatch = true;
                        canPlayerLeave = false;
                    }
                }
                else if (CheckIfIsThereAnyEnemies())
                {
                    result = new InquiryData(GameTexts.FindText("str_tournament", null).ToString(), GameTexts.FindText("str_tournament_skip", null).ToString(), true, true, GameTexts.FindText("str_yes", null).ToString(), GameTexts.FindText("str_no", null).ToString(), new Action(this.SkipMatch), null, "");
                }
                else
                {
                    _forceEndMatch = true;
                    canPlayerLeave = false;
                }
            }
            return result!;
        }

        /// <summary>
        /// just so we keep the interface intact, should never be called
        /// </summary>
        /// <param name="match"></param>
        /// <param name="isLastRound"></param>
        public void StartMatch(TournamentMatch match, bool isLastRound)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// just so we keep the interface intact, should never be called
        /// </summary>
        /// <param name="match"></param>
        public void SkipMatch(TournamentMatch match)
        {
            throw new NotImplementedException();
        }

        private readonly CharacterObject _defaultWeaponTemplatesIdTeamSizeOne = MBObjectManager.Instance.GetObject<CharacterObject>("tournament_template_empire_one_participant_set_v1");
        private readonly CharacterObject _defaultWeaponTemplatesIdTeamSizeTwo = MBObjectManager.Instance.GetObject<CharacterObject>("tournament_template_empire_two_participant_set_v1");
        private readonly CharacterObject _defaultWeaponTemplatesIdTeamSizeFour = MBObjectManager.Instance.GetObject<CharacterObject>("tournament_template_empire_four_participant_set_v1");
        private TeamTournamentMatch? _match;
        private bool _isLastRound;
        private BasicMissionTimer? _endTimer;
        private BasicMissionTimer? _cheerTimer;
        private List<GameEntity>? _spawnPoints;
        private bool _isSimulated;
        private bool _forceEndMatch;
        private List<TeamTournamentMember>? _aliveMembers;
        private List<TeamTournamentTeam>? _aliveTeams;
    }
}