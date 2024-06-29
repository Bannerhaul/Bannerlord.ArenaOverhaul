namespace ArenaOverhaul.ArenaPractice
{
    public static class ParryPracticeStatsManager
    {
        public static int PreparedBlocks { get; internal set; } = 0;
        public static int PerfectBlocks { get; internal set; } = 0;
        public static int ChamberBlocks { get; internal set; } = 0;

        public static int HitsTaken { get; internal set; } = 0;
        public static int HitsMade { get; internal set; } = 0;

        public static (int PreparedBlocks, int PerfectBlocks, int ChamberBlocks, int HitsTaken, int HitsMade) LastPracticeStats { get; internal set; } = (0, 0, 0, 0, 0);

        public static void Reset()
        {
            LastPracticeStats = (PreparedBlocks, PerfectBlocks, ChamberBlocks, HitsTaken, HitsMade);

            PreparedBlocks = 0;
            PerfectBlocks = 0;
            ChamberBlocks = 0;
            HitsTaken = 0;
            HitsMade = 0;
        }
    }
}