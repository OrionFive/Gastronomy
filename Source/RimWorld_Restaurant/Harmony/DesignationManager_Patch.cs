using HarmonyLib;
using Verse;

namespace Restaurant.Harmony
{
    /// <summary>
    /// So TableTops are notified when a table is removed
    /// </summary>
    internal static class DesignationManager_Patch
    {
        [HarmonyPatch(typeof(DesignationManager), "Notify_BuildingDespawned")]
        public class Notify_BuildingDespawned
        {
            [HarmonyPostfix]
            internal static void Postfix(Thing b)
            {
                b?.Position.GetFirstThing<TableTop>(b.Map)?.Notify_BuildingDespawned(b);
            }
        }
    }
}
