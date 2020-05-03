using System.Runtime.CompilerServices;
using HarmonyLib;
using Verse;

namespace Restaurant.Patching
{
    /// <summary>
    /// So we can call the base method on ThingWithComps and avoid whatever overrides it
    /// </summary>
    public class ThingWithComps
    {
        [HarmonyPatch(typeof(Verse.ThingWithComps), "SpawnSetup")]
        public class SpawnSetup
        {
            [HarmonyReversePatch]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Base(Verse.ThingWithComps instance, Map map, bool respawningAfterLoad) { } // Can't remove unused parameters
        }

        [HarmonyPatch(typeof(Verse.ThingWithComps), "Destroy")]
        public class Destroy
        {
            [HarmonyReversePatch]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Base(Verse.ThingWithComps instance, DestroyMode mode) { }
        }

        [HarmonyPatch(typeof(Verse.ThingWithComps), "DeSpawn")]
        public class DeSpawn
        {
            [HarmonyReversePatch]
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Base(Verse.ThingWithComps instance, DestroyMode mode) { }
        }
    }
}
