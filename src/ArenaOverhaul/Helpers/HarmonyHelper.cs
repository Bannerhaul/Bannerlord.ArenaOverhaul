using HarmonyLib;

using System;

namespace ArenaOverhaul.Helpers
{
    internal static class HarmonyHelper
    {
        public static bool PatchAll(ref Harmony? harmonyInstance, string sectionName, string logMessage, string chatMessage = "")
        {
            try
            {
                if (harmonyInstance is null)
                    harmonyInstance = new Harmony("Bannerlord.ArenaOverhaul");
                harmonyInstance.PatchAll();
                return true;
            }
            catch (Exception ex)
            {
                DebugHelper.HandleException(ex, sectionName, logMessage, chatMessage);
                return false;
            }
        }
    }
}
