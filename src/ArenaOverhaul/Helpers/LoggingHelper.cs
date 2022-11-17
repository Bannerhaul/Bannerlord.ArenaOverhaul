using ArenaOverhaul.Extensions.Harmony;

using HarmonyLib;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace ArenaOverhaul.Helpers
{
    internal static class LoggingHelper
    {
        private static readonly PlatformDirectoryPath ModLogsPath = EngineFilePaths.ConfigsPath + "/ModLogs";
        private static readonly PlatformFilePath ModLogsFilePath = new PlatformFilePath(ModLogsPath, "ArenaOverhaul.log");
        public static readonly string AOLogFile = ModLogsFilePath.FileFullPath;

        public static void Log(string message)
        {
            lock (AOLogFile)
            {
                using (StreamWriter streamWriter = File.AppendText(AOLogFile))
                {
                    streamWriter.WriteLine(message);
                }
            }
        }
        public static void Log(string message, string sectionName)
        {
            lock (AOLogFile)
            {
                using (StreamWriter streamWriter = File.AppendText(AOLogFile))
                {
                    streamWriter.WriteLine($"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] - {sectionName}.\n{message}");
                }
            }
        }
        public static void LogILAndPatches(List<CodeInstruction> codes, StringBuilder issueInfo, MethodBase currentMethod)
        {
            issueInfo.Append($"\nIL:");
            for (int i = 0; i < codes.Count; ++i)
            {
                issueInfo.Append($"\n\t{i:D4}:\t{codes[i]}");
            }
            // get info about other transpilers on OriginalMethod        
            HarmonyLib.Patches patches;
            patches = Harmony.GetPatchInfo(currentMethod);
            if (patches != null)
            {
                issueInfo.Append($"\nOther transpilers:");
                foreach (Patch patch in patches.Transpilers)
                {
                    issueInfo.Append(patch.GetDebugString());
                }
            }
        }
    }
}