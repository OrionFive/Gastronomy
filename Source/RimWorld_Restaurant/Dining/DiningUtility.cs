using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace Restaurant.Dining
{
    public static class DiningUtility
    {
        public static ThingDef diningSpotDef = ThingDef.Named("Restaurant_DiningSpot");

        public static DiningSpot FindDiningSpotFor(Pawn pawn, out Thing foodDef)
        {
            const int maxRegionsToScan = 100;

            bool Validator(Thing thing)
            {
                var spot = (DiningSpot) thing;
                if (spot.register == null || !spot.register.IsOpenedRightNow || !spot.register.HasAnyFoodFor(pawn) || spot.IsForbidden(pawn) || !spot.IsSociallyProper(pawn) || !pawn.CanReserve(spot)) return false;
                return true;
            }

            var diningSpot = (DiningSpot)GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(diningSpotDef), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, Validator, null, 0, maxRegionsToScan, false, RegionType.Set_Passable, true);
            if (diningSpot == null)
            {
                foodDef = null;
                return null;
            }

            foodDef = diningSpot.register.GetBestFoodFor(pawn);
            return foodDef == null ? null : diningSpot;
        }
    }
}
