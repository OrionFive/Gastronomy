using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Restaurant.Dining
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
                var restaurant = pawn.GetRestaurant();
                if (restaurant?.IsOpenedRightNow == false) return true; // Run regular code
                Log.Message($"{pawn.NameShortColored} is looking for joy-ingestable.");

                bool allowDrug = !pawn.IsTeetotaler();

                var diningSpot = DiningUtility.FindDiningSpotFor(pawn, out var foodDef, allowDrug, extraValidator);
                if (foodDef == null || diningSpot == null) return true; // Run regular code
                
                Log.Message($"{pawn.NameShortColored} wants to eat at restaurant ({diningSpot.Position}).");

                Job job = JobMaker.MakeJob(DiningUtility.dineDef, diningSpot);
                __result = job;
                return false;
            }
        }
    }
}
