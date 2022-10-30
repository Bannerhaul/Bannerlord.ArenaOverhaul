using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace ArenaOverhaul.Helpers
{
    internal static class MessageHelper
    {
        public static void QuickInformationMessage(TextObject message, int priorty = 0, BasicCharacterObject? announcerCharacter = null, string soundEventPath = "")
        {
            MBInformationManager.AddQuickInformation(message, priorty, announcerCharacter, soundEventPath);
        }

        public static void SimpleMessage(string message)
        {
            InformationManager.DisplayMessage(new InformationMessage(message, Colors.Yellow));
        }
        public static void SimpleMessage(TextObject textObject)
        {
            InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), Colors.Yellow));
        }

        public static void TechnicalMessage(string message)
        {
            InformationManager.DisplayMessage(new InformationMessage(message, Colors.Magenta));
        }
        public static void TechnicalMessage(TextObject textObject)
        {
            InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), Colors.Magenta));
        }

        public static void ErrorMessage(string message)
        {
            InformationManager.DisplayMessage(new InformationMessage(message, Colors.Red));
        }
        public static void ErrorMessage(TextObject textObject)
        {
            InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), Colors.Red));
        }
    }
}