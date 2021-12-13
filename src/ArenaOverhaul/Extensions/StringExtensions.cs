using System;
using System.Collections.ObjectModel;
using System.Linq;

using TaleWorlds.Localization;

namespace ArenaOverhaul.Extensions
{
    public static class StringExtensions
    {
        public static string ToLocalizedString(this string String)
        {
            return new TextObject(String).ToString();
        }
        public static ReadOnlyCollection<string> ToReadOnlyCollection(this string String, char Separator = ';')
        {
            return String.Split(Separator).Select(p => p.Trim()).ToList().AsReadOnly();
        }
    }
}