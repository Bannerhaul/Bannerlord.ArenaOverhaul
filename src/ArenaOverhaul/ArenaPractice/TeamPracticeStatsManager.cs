using ArenaOverhaul.CampaignBehaviors.BehaviorManagers;

namespace ArenaOverhaul.ArenaPractice
{
    public static class TeamPracticeStatsManager
    {
        public static int AliveAlliesCount { get; internal set; } = 0;
        public static int SpawnedAliedAgentCount { get; internal set; } = 0;
        public static int RemainingAlliesCount => (AOArenaBehaviorManager._lastPlayerRelatedCharacterList?.Count ?? 0) - SpawnedAliedAgentCount + AliveAlliesCount;

        public static (int AliveAlliesCount, int SpawnedAliedAgentCount, int RemainingAlliesCount) LastPracticeStats { get; internal set; } = (0, 0, 0);

        public static void Reset()
        {
            LastPracticeStats = (AliveAlliesCount, SpawnedAliedAgentCount, RemainingAlliesCount);

            AliveAlliesCount = 0;
            SpawnedAliedAgentCount = 0;
        }
    }
}
