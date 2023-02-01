using System;

namespace ArenaOverhaul.Helpers
{
    public static class MathHelper
    {
        public static int GetSoftCappedValue(float value, int softCap = 10000)
        {
            int baseLog = (int) Math.Max(Math.Log10(softCap) - 1, 0);
            return (int) (value <= softCap ? value : (softCap * (Math.Log10(value) - baseLog)));
        }
    }
}
