using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Restaurant.Dining
{
    internal static class _Harmony_JobGiver_GetFood_Patch
    {
        [HarmonyPatch(typeof(JobGiver_GetFood), "TryGiveJob")]
        public class TryGiveJob
        {
            [HarmonyPostfix]
            internal static void Postfix(Pawn pawn, ref Job __result)
            {
                if (__result?.def == JobDefOf.Ingest && __result?.targetA.HasThing == true && __result?.targetA.Thing is DiningSpot)
                {
                    Log.Message($"{pawn.NameShortColored} is now dining instead of ingesting.");
                    __result.def = DiningUtility.dineDef;
                }
            }
        }
    }
}
