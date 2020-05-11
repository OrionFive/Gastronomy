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
            f.AddEndCondition(() => f.GetActor().GetRestaurant().IsOpenedRightNow ? JobCondition.Ongoing : JobCondition.Incompletable);
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

        public static RestaurantSettings GetRestaurant([NotNull]this Thing thing)
        {
            return thing.Map.GetComponent<RestaurantSettings>();
        }
    }
}
