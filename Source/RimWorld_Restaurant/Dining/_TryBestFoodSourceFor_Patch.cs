using HarmonyLib;
using RimWorld;
using Verse;

namespace Restaurant.Dining
{
    /// <summary>
    /// For allowing pawns to find DiningSpots when hungry. This should later be replaced with BestFoodSourceOnMap, so alternatives are considered
    /// </summary>
    internal static class _TryBestFoodSourceFor_Patch
    {
        [HarmonyPatch(typeof(FoodUtility), "TryFindBestFoodSourceFor")]
        public class TryFindBestFoodSourceFor
        {
            [HarmonyPrefix]
            internal static bool Prefix(Pawn getter, Pawn eater, ref bool __result, ref Thing foodSource, ref ThingDef foodDef, ref bool desperate)
            {
                if (desperate) return true; // Run original code

                if (getter != eater) return true; // Run original code

                // Only if time assignment allows
                if (!eater.GetTimeAssignment().allowJoy) return true;

                if (!getter.IsAbleToDine()) return true;

                var diningSpot = DiningUtility.FindDiningSpotFor(getter, out foodDef, false);

                if (diningSpot == null) return true; // Run original code
                
                // Actually dine
                foodSource = diningSpot;
                //Log.Message($"{getter.NameShortColored} found diningSpot at {diningSpot.Position} with {foodDef?.label}.");
                __result = true;
                return false; // Don't run original code
            }
        }
    }
}
