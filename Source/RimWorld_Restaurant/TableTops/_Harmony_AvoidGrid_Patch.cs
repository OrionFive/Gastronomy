using HarmonyLib;
using Restaurant.Dining;
using Verse;
using Verse.AI;

namespace Restaurant.TableTops
{
    internal static class _Harmony_AvoidGrid_Patch
    {
        /// <summary>
        /// Is a tabletop spawned? Remove DiningSpots
        /// </summary>
        [HarmonyPatch(typeof(AvoidGrid), "Notify_BuildingSpawned")]
        public class Notify_BuildingSpawned
        {
            [HarmonyPostfix]
            internal static void Postfix(Building building, AvoidGrid __instance)
            {
                if (building == null) return;

                if (!(building is TableTop)) return;
                Log.Message($"BuildingSpawned at {building.Position}: {building.Label}.");
                foreach (var thing in building.Position.GetThingList(__instance.map).ToArray())
                {
                    if (thing is DiningSpot)
                    {
                        Log.Message($"Removing DiningSpot.");
                        thing.Destroy(DestroyMode.Cancel);
                    }
                }
            }
        }

        /// <summary>
        /// Is a building removed? Remove tabletops and blueprints of tabletops at its location
        /// </summary>
        [HarmonyPatch(typeof(AvoidGrid), "Notify_BuildingDespawned")]
        public class Notify_BuildingDespawned
        {
            [HarmonyPostfix]
            internal static void Postfix(Building building, AvoidGrid __instance)
            {
                if (building == null) return;
                if (building.def.surfaceType != SurfaceType.Eat) return; // Has to be table
                Log.Message($"BuildingDespawned at {building.Position}: {building.Label}.");

                foreach (var pos in building.OccupiedRect())
                {
                    foreach (var thing in pos.GetThingList(__instance.map).ToArray())
                    {
                        // Notify table top
                        if(thing is TableTop t) t.Notify_BuildingDespawned(building);
                        // Remove blueprints
                        else if (thing.def.IsBlueprint && thing.def.entityDefToBuild is ThingDef td && typeof(TableTop).IsAssignableFrom(td.thingClass))
                        {
                            Log.Message($"Removing blueprint.");
                            thing.Destroy(DestroyMode.Cancel);
                        }
                    }
                }
            }
        }
    }
}
