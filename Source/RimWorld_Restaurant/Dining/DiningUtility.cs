using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using Verse.AI;

namespace Restaurant.Dining
{
    public static class DiningUtility
    {
        public static readonly ThingDef diningSpotDef = ThingDef.Named("Restaurant_DiningSpot");
        public static readonly JobDef dineDef = DefDatabase<JobDef>.GetNamed("Restaurant_Dine");
        public static readonly HashSet<ThingDef> thingsWithCompCanDineAt = new HashSet<ThingDef>();

        public static IEnumerable<DiningSpot> GetAllDiningSpots([NotNull] Map map)
        {
            return map.listerThings.ThingsOfDef(diningSpotDef).OfType<DiningSpot>();
        }

        public static DiningSpot FindDiningSpotFor([NotNull] Pawn pawn, out ThingDef foodDef, bool allowDrug)
        {
            const int maxRegionsToScan = 100;
            foodDef = null;

            var settings = pawn.GetRestaurant();
            if (settings == null) return null;

            if (!settings.HasAnyFoodFor(pawn, allowDrug)) return null;

            bool Validator(Thing thing)
            {
                var spot = (DiningSpot) thing;
                return !spot.IsForbidden(pawn) && spot.IsSociallyProper(pawn) && pawn.CanReserve(spot, spot.GetMaxReservations(), 0) && spot.IsOpenedRightNow;
            }

            var diningSpot = (DiningSpot) GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(diningSpotDef), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, Validator, null, 0, maxRegionsToScan, false,
                RegionType.Set_Passable, true);
            if (diningSpot == null) return null;

            foodDef = settings.GetBestFoodTypeFor(pawn, allowDrug);
            return foodDef == null ? null : diningSpot;
        }

        public static void RegisterDiningSpotHolder(ThingWithComps thing)
        {
            thingsWithCompCanDineAt.Add(thing.def);
        }

        public static bool CanPossiblyDineAt(ThingDef def)
        {
            return thingsWithCompCanDineAt.Contains(def);
        }
    }
}
