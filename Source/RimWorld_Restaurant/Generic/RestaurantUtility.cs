using System;
using System.Linq;
using JetBrains.Annotations;
using Restaurant.Dining;
using Verse;
using Verse.AI;

namespace Restaurant
{
    internal static class RestaurantUtility
    {
        public static T FailOnRestaurantClosed<T>(this T f) where T : IJobEndable
        {
            JobCondition OnRestaurantClosed() => f.GetActor().GetRestaurant().IsOpenedRightNow ? JobCondition.Ongoing : JobCondition.Incompletable;

            f.AddEndCondition(OnRestaurantClosed);
            return f;
        }

        public static T FailOnDangerous<T>(this T f) where T : IJobEndable
        {
            JobCondition OnRegionDangerous() => f.GetActor().GetRegion().DangerFor(f.GetActor()) <= Danger.None ? JobCondition.Ongoing : JobCondition.Incompletable;

            f.AddEndCondition(OnRegionDangerous);
            return f;
        }

        public static T FailOnDurationExpired<T>(this T f) where T : IJobEndable
        {
            JobCondition OnDurationExpired() => f.GetActor().jobs.curDriver.ticksLeftThisToil > 0 ? JobCondition.Ongoing : JobCondition.Incompletable;
            
            f.AddEndCondition(OnDurationExpired);
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

        public static bool HasDiningQueued(this Pawn patron)
        {
            if (patron?.CurJobDef == DiningUtility.dineDef) return true;
            return patron?.jobs.jobQueue?.Any(j => j.job.def == DiningUtility.dineDef) == true;
        }

        public static RestaurantSettings GetRestaurant([NotNull]this Thing thing)
        {
            return thing.Map.GetComponent<RestaurantSettings>();
        }

        public static T GetDriver<T>(this Pawn patron) where T : JobDriver
        {
            return patron?.jobs?.curDriver as T;
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
    }
}
