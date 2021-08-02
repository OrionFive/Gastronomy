using System;
using Gastronomy.Restaurant;
using HarmonyLib;
using Verse;

namespace Gastronomy.Compatibility
{
	/// <summary>
	/// So save games don't break
	/// </summary>
	internal static class _BackCompatibilityConverter_Universal_Patch
	{
		[HarmonyPatch(typeof(BackCompatibilityConverter_Universal), nameof(BackCompatibilityConverter_Universal.GetBackCompatibleType))]
		public class GetBackCompatibleType
		{
			internal static bool Prefix(string providedClassName, ref Type __result)
			{
				if (providedClassName == "Gastronomy.RestaurantController")
				{
					__result = typeof(RestaurantController); // Namespace changed
					return false;
				}

				return true;
			}
		}

		//[HarmonyPatch(typeof(BackCompatibilityConverter_1_2), nameof(BackCompatibilityConverter_1_2.BackCompatibleDefName))]
		//public class BackCompatibleDefName
		//{
		//	internal static bool Prefix(Type defType, string defName, ref string __result)
		//	{
		//		if (defType == typeof(WorkGiverDef) && defName == "Gastronomy_EmptyRegister")
		//		{
		//			__result = "CashRegister_EmptyRegister";
		//			return false;
		//		}
		//
		//		if (defType == typeof(JobDef) && defName == "Gastronomy_EmptyRegister")
		//		{
		//			__result = "CashRegister_EmptyRegister";
		//			return false;
		//		}
		//
		//		if (defType == typeof(ThingDef) && defName == "Gastronomy_CashRegister")
		//		{
		//			__result = "CashRegister_CashRegister";
		//			return false;
		//		}
		//
		//		return true;
		//	}
		//}
	}

}
