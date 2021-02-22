using System;
using System.Linq;
using Gastronomy.Dining;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Gastronomy.Restaurant
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

        public static bool IsGuest(this Pawn pawn)
        {
            var faction = pawn.GetLord()?.faction;
            if (pawn.IsPrisoner) return false;
            //Log.Message($"{pawn.NameShortColored}: Faction = {faction?.GetCallLabel()} Is player = {faction?.IsPlayer} Hostile = {faction?.HostileTo(Faction.OfPlayer)}");
            return faction != null && !faction.IsPlayer && !faction.HostileTo(Faction.OfPlayer);
            //var isGuest = AccessTools.Method("Hospitality.GuestUtility:IsGuest");
            //Log.Message($"isGuest == null? {isGuest == null}");
            //if(isGuest != null)
            //{
            //    return (bool) isGuest.Invoke(null, new object[] {pawn, false});
            //}
            //return false;
        }

        public static int GetSilver(this Pawn pawn)
        {
            if (pawn?.inventory?.innerContainer == null) return 0;
            return pawn.inventory.innerContainer.Where(s => s.def == ThingDefOf.Silver).Sum(s => s.stackCount);
        }

        public static float GetPrice(this ThingDef mealDef, RestaurantController restaurant)
        {
            if (mealDef == null) return 0;
            return mealDef.BaseMarketValue * restaurant.guestPricePercentage;
        }

        public static T FailOnRestaurantClosed<T>(this T f) where T : IJobEndable
        {
            JobCondition OnRestaurantClosed() => f.GetActor().GetRestaurant().IsOpenedRightNow ? JobCondition.Ongoing : JobCondition.Incompletable;

            f.AddEndCondition(OnRestaurantClosed);
            return f;
        }

        public static T FailOnNotDining<T>(this T f, TargetIndex patronInd) where T : IJobEndable
        {
            JobCondition PatronIsNotDining()
            {
                var patron = f.GetActor().jobs.curJob.GetTarget(patronInd).Thing as Pawn;
                if (patron?.jobs.curDriver is JobDriver_Dine) return JobCondition.Ongoing;
                Log.Message($"Checked {patron?.NameShortColored}. Not dining >> failing {f.GetActor().NameShortColored}'s job {f.GetActor().CurJobDef?.label}.");
                return JobCondition.Incompletable;
            }

            f.AddEndCondition(PatronIsNotDining);
            return f;
        }

        public static T FailOnNotDiningQueued<T>(this T f, TargetIndex patronInd) where T : IJobEndable
        {
            JobCondition PatronHasNoDiningInQueue()
            {
                var patron = f.GetActor().jobs.curJob.GetTarget(patronInd).Thing as Pawn;
                if (patron.HasDiningQueued()) return JobCondition.Ongoing;
                Log.Message($"Checked {patron?.NameShortColored}. Not planning to dine >> failing {f.GetActor().NameShortColored}'s job {f.GetActor().CurJobDef?.label}.");
                return JobCondition.Incompletable;
            }

            f.AddEndCondition(PatronHasNoDiningInQueue);
            return f;
        }
    }
}
