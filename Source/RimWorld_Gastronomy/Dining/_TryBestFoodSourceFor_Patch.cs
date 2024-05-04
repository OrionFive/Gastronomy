using System.Linq;
using System.Reflection;
using Gastronomy.Restaurant;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Gastronomy.Dining
{
    /// <summary>
    /// For allowing pawns to find DiningSpots when hungry. This should later be replaced with BestFoodSourceOnMap, so alternatives are considered
    /// </summary>
    internal static class _TryBestFoodSourceFor_Patch
    {
        /// <summary>
        /// This is for GetFood_Patch, so we can see what restaurant was found for the dining spot. Very hacky, but should work.
        /// </summary>
        internal static RestaurantController LastRestaurantResult { get; private set; }

        /// <summary>
        /// Patching _NewTemp if it exists, or original version if it doesn't, so players with older versions don't run into issues.
        ///     Also: Goddammit, Ludeon :(
        /// </summary>
        [HarmonyPatch]
        public class TryFindBestFoodSourceFor
        {
            [HarmonyTargetMethod]
            private static MethodBase TargetMethod()
            {
                return AccessTools.Method(typeof(FoodUtility), nameof(FoodUtility.TryFindBestFoodSourceFor));
            }

            [HarmonyPrefix]
            internal static bool Prefix(Pawn getter, Pawn eater, ref bool __result, ref Thing foodSource, ref ThingDef foodDef, ref bool desperate)
            {
                if (desperate && __result) return true; // Run original code, but only if we actually found something

                if (getter != eater) return true; // Run original code

                if (eater.NonHumanlikeOrWildMan()) return true;

                // Only if pawn doesn't have to work himself
                if (eater.GetAllRestaurantsEmployed().Any()) return true;

                if (!getter.IsAbleToDine()) return true;

                // Caravans can't eat, since indoor locations are forbidden to them
                var diningSpots = DiningUtility.FindDiningSpotsFor(eater, false).ToArray();
                //Log.Message($"{getter.NameShortColored} is looking for food (check stack trace for reason). Found {diningSpots.Length} dining spots.");

                var bestType = RestaurantStock.GetBestMealFor(diningSpots.SelectMany(d => d.GetRestaurantsServing()).Distinct(), eater, out var restaurant, false);
                if (bestType == null) return true; // Run original code

                foodDef = bestType.def;
                foodSource = diningSpots.FirstOrDefault(s => restaurant.diningSpots.Contains(s)); // TODO: Could check for closest, but eh, expensive
                LastRestaurantResult = restaurant;
                //Log.Message($"{getter.NameShortColored} found diningSpot at {foodSource?.Position} with {foodDef?.label}, (nutrition = {FoodUtility.GetNutrition(eater, foodSource, foodDef)}, parent = {foodSource?.ParentHolder}).");
                __result = true;
                return false; // Don't run original code
            }
        }
    }
}