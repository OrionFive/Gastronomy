using Gastronomy.Dining;
using Gastronomy.Restaurant;
using JetBrains.Annotations;
using Verse;
using Verse.AI;

namespace Gastronomy.TableTops
{
    public static class RegisterUtility
    {
        public static readonly ThingDef cashRegisterDef = ThingDef.Named("Gastronomy_CashRegister");
        public static readonly JobDef emptyRegisterDef = DefDatabase<JobDef>.GetNamed("Gastronomy_EmptyRegister");

        public static Building_CashRegister GetClosestRegister([NotNull]this Pawn pawn)
        {
            return (Building_CashRegister)GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(cashRegisterDef), PathEndMode.Touch, TraverseParms.For(pawn), 90f, x => x.Faction == pawn.Faction, null, 0, 30);
        }

        public static void OnDiningSpotCreated([NotNull]DiningSpot diningSpot)
        {
            diningSpot.GetRestaurant().diningSpots.Add(diningSpot);
        }

        public static void OnDiningSpotRemoved([NotNull]DiningSpot diningSpot)
        {
            diningSpot.GetRestaurant().diningSpots.Remove(diningSpot);
        }

        public static void OnBuildingDespawned(Building building, Map map)
        {
            if (building == null) return;
            if (building.def.surfaceType == SurfaceType.Eat || building is Building_TableTop)
            {
                foreach (var pos in building.OccupiedRect())
                {
                    NotifyDespawnedAtPosition(building, map, pos);
                }
            }
        }

        private static void NotifyDespawnedAtPosition(Building building, Map map, IntVec3 pos)
        {
            foreach (var thing in pos.GetThingList(map).ToArray())
            {
                // Notify potential dining spots
                if (DiningUtility.CanPossiblyDineAt(thing.def)) thing.TryGetComp<CompCanDineAt>()?.Notify_BuildingDespawned(building, map);
                // Notify table top
                if (thing is Building_TableTop t) t.Notify_BuildingDespawned(building);
                // Remove blueprints
                else if (thing.def.IsBlueprint && thing.def.entityDefToBuild is ThingDef td && typeof(Building_TableTop).IsAssignableFrom(td.thingClass))
                {
                    thing.Destroy(DestroyMode.Cancel);
                }
            }
        }

        public static void OnBuildingSpawned(Building building, Map map)
        {
            if (building == null) return;

            if (!(building is Building_TableTop)) return;
            foreach (var thing in building.Position.GetThingList(map).ToArray())
            {
                if (thing is DiningSpot)
                {
                    thing.Destroy(DestroyMode.Cancel);
                }
            }
        }
    }
}
