using HarmonyLib;

namespace ArenaOverhaul.Extensions.Harmony
{
    public static class PatchExtension
    {
        public static string GetDebugString(this Patch patch)
        {
            return $"\t\tPatching method: {patch.PatchMethod}\n\t\tOwner: {patch.PatchMethod.DeclaringType.Assembly.GetName().Name} (HarmonyID: \"{patch.owner}\")\n\t\tPriority: {patch.priority}\n\t\tBefore: {patch.before}\n\t\tAfter: {patch.after}";
        }
    }
}