using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace Gastronomy.Dining
{
	/// <summary>
	/// So we can call the base method on ThingWithComps and avoid whatever overrides it
	/// </summary>
	[HarmonyPatch(typeof(DiningSpot))]
	static class _DiningSpot_ReplaceBaseMethods_Patch
	{
		[Conditional("DEBUG")]
		private static void DumpChanges(string methodName, IEnumerable<CodeInstruction> before, IEnumerable<CodeInstruction> after)
		{
			Log.Warning($"::: {methodName} Before :::");
			foreach (var i in before) Log.Warning(i.ToString());
			Log.Warning($"::: {methodName} After :::");
			foreach (var i in after) Log.Warning(i.ToString());
		}

		[HarmonyPatch(nameof(DiningSpot.SpawnSetup))]
		[HarmonyTranspiler, UsedImplicitly]
		private static IEnumerable<CodeInstruction> SpawnSetup(IEnumerable<CodeInstruction> instructions)
		{
			var code = instructions.MethodReplacer(
				AccessTools.Method(typeof(Building_NutrientPasteDispenser), nameof(Building_NutrientPasteDispenser.SpawnSetup)),
				AccessTools.Method(typeof(ThingWithComps), nameof(ThingWithComps.SpawnSetup))
			);
			DumpChanges("SpawnSetup", instructions, code);
			return code;
		}

		[HarmonyPatch(nameof(DiningSpot.DeSpawn))]
		[HarmonyTranspiler, UsedImplicitly]
		private static IEnumerable<CodeInstruction> DeSpawn(IEnumerable<CodeInstruction> instructions)
		{
			var code = instructions.MethodReplacer(
				AccessTools.Method(typeof(Building), nameof(Building.DeSpawn)),
				AccessTools.Method(typeof(ThingWithComps), nameof(ThingWithComps.DeSpawn))
			);
			DumpChanges("DeSpawn", instructions, code);
			return code;
		}

		[HarmonyPatch(nameof(DiningSpot.Destroy))]
		[HarmonyTranspiler, UsedImplicitly]
		private static IEnumerable<CodeInstruction> Destroy(IEnumerable<CodeInstruction> instructions)
		{
			var code = instructions.MethodReplacer(
				AccessTools.Method(typeof(Building), nameof(Building.Destroy)),
				AccessTools.Method(typeof(ThingWithComps), nameof(ThingWithComps.Destroy))
			);
			DumpChanges("Destroy", instructions, code);
			return code;
		}
	}
}
