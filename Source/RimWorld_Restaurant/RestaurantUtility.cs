using JetBrains.Annotations;
using Restaurant.TableTops;
using Verse;
using Verse.AI;

namespace Restaurant
{
    internal static class RestaurantUtility
    {
        public static T FailOnRestaurantClosed<T>(this T f) where T : IJobEndable
        {
            f.AddEndCondition(() => f.GetActor().Map.GetSettings().IsOpenedRightNow ? JobCondition.Ongoing : JobCondition.Incompletable);
            return f;
        }

        public static RestaurantSettings GetSettings([NotNull]this Map map)
        {
            return map.GetComponent<RestaurantSettings>();
        }
    }
}
