namespace ArenaOverhaul.ArenaPractice
{
    public static class ParryStatsManager
    {
        public static int PreparedBlocks { get; internal set; } = 0;
        public static int PerfectBlocks { get; internal set; } = 0;
        public static int ChamberBlocks { get; internal set; } = 0;
        public static int HitsTaken { get; internal set; } = 0;

        public static void Reset()
        {
            PreparedBlocks = 0;
            PerfectBlocks = 0;
            ChamberBlocks = 0;
            HitsTaken = 0;
        }
    }
}