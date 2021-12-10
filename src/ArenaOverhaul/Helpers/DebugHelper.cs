using System;
using System.Reflection;

using TaleWorlds.Localization;

namespace ArenaOverhaul.Helpers
{
    internal static class DebugHelper
    {
        public static void HandleException(Exception ex, MethodInfo? methodInfo, string sectionName)
        {
            MessageHelper.ErrorMessage(string.Format("Allegiance Overhaul - error occurred in [{1}]{0} - {2} See details in the mod log.", methodInfo != null ? " in " + methodInfo.Name : "", sectionName, ex.Message));
            LoggingHelper.Log(string.Format("Error occurred{0} - {1}", methodInfo != null ? $" in {methodInfo}" : "", ex.ToString()), sectionName);
        }

        public static void HandleException(Exception ex, string sectionName, string logMessage, string chatMessage)
        {
            if (chatMessage.Length > 0)
            {
                TextObject textObject = new TextObject(chatMessage);
                textObject.SetTextVariable("SECTION", sectionName);
                textObject.SetTextVariable("EXCEPTION_MESSAGE", ex.Message);
                MessageHelper.ErrorMessage(textObject);
            }
            LoggingHelper.Log(string.Format(logMessage, ex.ToString()), sectionName);
        }
        public static ArgumentOutOfRangeException GetOutOfRangeException<T>(T value, string functionName, string argumentName)
        {
            return new ArgumentOutOfRangeException(argumentName, value, string.Format("{0} is supplied with not supported {1} value.", functionName, typeof(T).Name));
        }
    }
}
