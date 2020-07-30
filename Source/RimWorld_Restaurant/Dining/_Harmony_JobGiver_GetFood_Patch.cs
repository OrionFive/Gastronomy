using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Restaurant.Dining
{
	internal static class _Harmony_JobGiver_GetFood_Patch
	{
		/// <summary>
		/// Replaces regular ingest job with dine job
		/// </summary>
		[HarmonyPatch(typeof(JobGiver_GetFood), "TryGiveJob")]
		public class TryGiveJob
		{
			[HarmonyPostfix]
			internal static void Postfix(Pawn pawn, ref Job __result)
			{
				if (__result == null) return;
				//Log.Message($"{pawn.NameShortColored} got job {__result.def.label} on {__result.targetA.Thing.Label}.");
				if (__result?.def == JobDefOf.Ingest && __result?.targetA.HasThing == true && __result?.targetA.Thing is DiningSpot)
				{
					bool allowDrug = !pawn.IsTeetotaler();
					var foodDef = pawn.GetRestaurant().Stock.GetBestFoodTypeFor(pawn, allowDrug);

					//Log.Message($"{pawn.NameShortColored} is now dining instead of ingesting.");
					__result.def = DiningUtility.dineDef;
					__result.plantDefToSow = foodDef; // Abusing this def for storing our favorite food type
				}
			}
		}
	}
}
