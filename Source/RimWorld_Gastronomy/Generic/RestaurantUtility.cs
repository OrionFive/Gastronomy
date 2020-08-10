using System;
using System.Linq;
using Gastronomy.Dining;
using JetBrains.Annotations;
using Verse;

namespace Gastronomy
{
    internal static class RestaurantUtility
    {
        public static bool HasDiningQueued(this Pawn patron)
        {
            if (patron?.CurJobDef == DiningUtility.dineDef) return true;
            return patron?.jobs.jobQueue?.Any(j => j.job.def == DiningUtility.dineDef) == true;
        }

        public static RestaurantController GetRestaurant([NotNull]this Thing thing)
        {
            return thing.Map.GetComponent<RestaurantController>();
        }

        public static void GetRequestGroup(Thing thing)
        {
            foreach (ThingRequestGroup group in Enum.GetValues(typeof(ThingRequestGroup)))
            {
                if (@group == ThingRequestGroup.Undefined) continue;
                if (thing.Map.listerThings.ThingsInGroup(@group).Contains(thing))
                    Log.Message($"DiningSpot group: {@group}");
            }
        }

        public static bool IsRegionDangerous(Pawn pawn, Region region = null)
        {
            if (region == null) region = pawn.GetRegion();
            return region.DangerFor(pawn) > Danger.None;
        }
    }
}
