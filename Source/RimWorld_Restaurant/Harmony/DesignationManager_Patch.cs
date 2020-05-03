using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace Restaurant.Harmony
{
    /// <summary>
    /// So TableTops are notified when a table is removed
    /// </summary>
    internal static class DesignationManager_Patch
    {
        private static List<Thing> destroyList = new List<Thing>();

        [HarmonyPatch(typeof(DesignationManager), "Notify_BuildingDespawned")]
        public class Notify_BuildingDespawned
        {
            [HarmonyPostfix]
            internal static void Postfix(Thing b, DesignationManager __instance)
            {
                if (b == null) return;

                foreach (var pos in b.OccupiedRect())
                {
                    pos.GetFirstThing<TableTop>(__instance.map)?.Notify_BuildingDespawned(b);
                    foreach (var thing in pos.GetThingList(__instance.map))
                    {
                        if(thing is TableTop t) t.Notify_BuildingDespawned(b);
                        else if (thing.def.IsBlueprint && thing.def.entityDefToBuild is ThingDef td && typeof(TableTop).IsAssignableFrom(td.thingClass))
                        {
                            destroyList.Add(thing);
                        }
                    }
                }

                // Destroy late to avoid changing collection
                foreach (var thing in destroyList)
                {
                    thing.Destroy(DestroyMode.Cancel);
                }
                destroyList.Clear();
            }
        }
    }
}
