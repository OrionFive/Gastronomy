using System;
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

        public static DiningSpot FindDiningSpotFor([NotNull] Pawn pawn, bool allowDrug, Predicate<Thing> extraSpotValidator = null)
        {
            const int maxRegionsToScan = 100;
            const int maxDistanceToScan = 100; // TODO: Make mod option?

            var restaurant = pawn.GetRestaurant();
            if (restaurant == null) return null;

            if (!restaurant.Stock.HasAnyFoodFor(pawn, allowDrug)) return null;

            bool Validator(Thing thing)
            {
                var spot = (DiningSpot) thing;
                return !spot.IsForbidden(pawn) && spot.IsSociallyProper(pawn) && spot.IsPoliticallyProper(pawn) && pawn.CanReserve(spot, spot.GetMaxReservations(), 0) 
                       && spot.IsOpenedRightNow && !RestaurantUtility.IsRegionDangerous(pawn, spot.GetRegion()) && (extraSpotValidator == null || extraSpotValidator.Invoke(spot));
            }

            var diningSpot = (DiningSpot) GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(diningSpotDef), 
                PathEndMode.ClosestTouch, TraverseParms.For(pawn), maxDistanceToScan, Validator, null, 0, 
                maxRegionsToScan, false, RegionType.Set_Passable, true);

            return diningSpot;
        }

        public static void RegisterDiningSpotHolder(ThingWithComps thing)
        {
            thingsWithCompCanDineAt.Add(thing.def);
        }

        public static bool CanPossiblyDineAt(ThingDef def)
        {
            return thingsWithCompCanDineAt.Contains(def);
        }

        public static bool IsAbleToDine(this Pawn getter)
        {
            var canManipulate = getter.RaceProps.ToolUser && getter.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation);
            if (!canManipulate) return false;

            var canTalk = getter.health.capacities.CapableOf(PawnCapacityDefOf.Talking);
            if (!canTalk) return false;

            var canMove = getter.health.capacities.CapableOf(PawnCapacityDefOf.Moving);
            if (!canMove) return false;

            return true;
        }

        public static DrugPolicyEntry GetPolicyFor(this Pawn pawn, ThingDef def)
        {
            var policy = pawn.drugs.CurrentPolicy;
            for (int i = 0; i < policy.Count; i++)
            {
                var entry = policy[i];
                if (entry.drug == def) return entry;
            }

            return null;
        }
    }
}
