using System;
using System.Linq;
using Gastronomy.Restaurant;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Gastronomy.Dining
{
    internal static class _JoyGiver_Ingest_Patch
    {
        /// <summary>
        /// Give dine job as joy, if restaurant is open
        /// </summary>
        [HarmonyPatch(typeof(JoyGiver_Ingest), "TryGiveJobInternal")]
        public class TryGiveJobInternal
        {
            [HarmonyPrefix]
            internal static bool Prefix(Pawn pawn, Predicate<Thing> extraValidator, ref Job __result)
            {
                //Log.Message($"{pawn.NameShortColored} is looking for restaurant (as joy job).");
                if (pawn.GetAllRestaurantsEmployed().Any()) return true;

                bool allowDrug = !pawn.IsTeetotaler();
                var diningSpots = DiningUtility.FindDiningSpotsFor(pawn, allowDrug, extraValidator).ToArray();

                // There is something edible, but is it good enough or like... a corpse?
                var bestConsumable = RestaurantStock.GetBestMealFor(diningSpots.SelectMany(d => d.GetRestaurantsServing()).Distinct(), pawn, out var restaurant, allowDrug, false);
                if (bestConsumable == null) return true; // Run regular code

                //Log.Message($"{pawn.NameShortColored} wants to eat at restaurant ({diningSpot.Position}).")
                pawn.GetRestaurantsManager().RegisterDiningAt(pawn, restaurant);
                Job job = JobMaker.MakeJob(DiningDefOf.Gastronomy_Dine, diningSpots.FirstOrDefault(s => restaurant.diningSpots.Contains(s))); // TODO: Could check for closest, but eh, expensive
                __result = job;
                return false;
            }
        }
    }
}
