using System.Linq;
using Gastronomy.Restaurant;
using HarmonyLib;
using RimWorld;
using System.Reflection;
using Verse;

namespace Gastronomy.Dining
{
    /// <summary>
    /// For allowing pawns to find DiningSpots when hungry. This should later be replaced with BestFoodSourceOnMap, so alternatives are considered
    /// </summary>
    internal static class _TryBestFoodSourceFor_Patch
    {
        /// <summary>
        /// Patching _NewTemp if it exists, or original version if it doesn't, so players with older versions don't run into issues.
        /// Also: Goddammit, Ludeon :(
        /// </summary>
        [HarmonyPatch]
        public class TryFindBestFoodSourceFor
        {
            [HarmonyTargetMethod]
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(FoodUtility), nameof(FoodUtility.TryFindBestFoodSourceFor_NewTemp)) ??
                       AccessTools.Method(typeof(FoodUtility), nameof(FoodUtility.TryFindBestFoodSourceFor)); // Not obsolete until NewTemp goes away. Don't believe their lies!
            }

            [HarmonyPrefix]
            internal static bool Prefix(Pawn getter, Pawn eater, ref bool __result, ref Thing foodSource, ref ThingDef foodDef, ref bool desperate)
            {
                if (desperate && __result) return true; // Run original code, but only if we actually found something

                if (getter != eater) return true; // Run original code

                // Only if pawn doesn't have to work
                if (eater.GetAllRestaurants().Any(r => r.HasToWork(eater))) return true;

                if (!getter.IsAbleToDine()) return true;

                var diningSpots = DiningUtility.FindDiningSpotsFor(eater, false).ToArray();

                var bestType = RestaurantStock.GetBestMealFor(diningSpots.SelectMany(d => d.GetRestaurants()).Distinct(), eater, out var restaurant, false);
                if (bestType == null) return true; // Run original code

                foodDef = bestType.def;
                foodSource = diningSpots.FirstOrDefault(s => restaurant.diningSpots.Contains(s)); // TODO: Could check for closest, but eh, expensive
                __result = true;
                return false; // Don't run original code
            }
        }
    }
}
