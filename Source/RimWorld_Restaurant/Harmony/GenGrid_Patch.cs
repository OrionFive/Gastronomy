using HarmonyLib;
using Verse;

namespace Restaurant.Harmony
{
    /// <summary>
    /// So pawns won't eat where the register is placed
    /// </summary>
    internal static class GenGrid_Patch
    {
        [HarmonyPatch(typeof(GenGrid), "HasEatSurface")]
        public class HasEatSurface
        {
            [HarmonyPostfix]
            internal static void Postfix(IntVec3 c, Map map, ref bool __result)
            {
                if (!__result) return;

                if (c.GetFirstThing<TableTop>(map) != null) __result = false;
            }
        }
    }
}