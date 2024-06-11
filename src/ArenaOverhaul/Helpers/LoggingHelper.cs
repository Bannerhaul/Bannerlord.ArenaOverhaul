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

        public static void LogNoHooksIssue(List<CodeInstruction> codes, int numberOfEdits, int requiredNumberOfEdits, MethodBase originalMethod, (string IndexName, int IndexValue)[] indexArgs, (string MethodInfoName, MemberInfo? MemberInfo)[] memberInfoArgs)

        {
            StringBuilder issueInfo = new("Indexes:");
            foreach (var indexInfo in indexArgs)
            {
                issueInfo.Append($"\n\t{indexInfo.IndexName}={indexInfo.IndexValue}");
            }
            issueInfo.Append($"\nNumberOfEdits: {numberOfEdits} out of {requiredNumberOfEdits}");
            if (memberInfoArgs.Length > 0)
            {
                issueInfo.Append($"\nMemberInfos:");
                foreach (var memberInfo in memberInfoArgs)
                {
                    issueInfo.Append($"\n\t{memberInfo.MethodInfoName}={(memberInfo.MemberInfo != null ? memberInfo.MemberInfo.ToString() : "not found")}");
                }
            }
            if (Settings.Instance!.LogTechnicalTranspilerInfo)
            {
                LogILAndPatches(codes, issueInfo, originalMethod);
            }
            Log(issueInfo.ToString(), $"Transpiler for {originalMethod.DeclaringType?.Name}.{originalMethod.Name}");

            if (numberOfEdits < requiredNumberOfEdits)
            {
                MessageHelper.ErrorMessage($"Harmony transpiler for  {originalMethod.DeclaringType?.Name}. {originalMethod.Name} was not able to make all required changes!");
            }
        }


        private static void LogILAndPatches(List<CodeInstruction> codes, StringBuilder issueInfo, MethodBase originalMethod)
        {
            issueInfo.Append($"\nIL:");
            for (int i = 0; i < codes.Count; ++i)
            {
                issueInfo.Append($"\n\t{i:D4}:\t{codes[i]}");
            }
            // get info about other transpilers on OriginalMethod        
            HarmonyLib.Patches patches;
            patches = Harmony.GetPatchInfo(originalMethod);
            if (patches != null && patches.Transpilers.Count > 0)
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