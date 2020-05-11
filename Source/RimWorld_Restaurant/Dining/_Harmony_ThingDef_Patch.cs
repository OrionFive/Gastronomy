using HarmonyLib;
using Verse;

namespace Restaurant.Dining
{
    /// <summary>
    /// So DiningSpot is not checked by Nutrient Paste Dispenser alerts
    /// </summary>
    internal static class _Harmony_ThingDef_Patch
    {
        [HarmonyPatch(typeof(ThingDef), "get_IsFoodDispenser")]
        public class IsFoodDispenser
        {
            [HarmonyPostfix]
            internal static void Postfix(ThingDef __instance, ref bool __result)
            {
                if (!__result) return;
                if (typeof(DiningSpot).IsAssignableFrom(__instance.thingClass)) __result = false;
            }
        }
    }
}