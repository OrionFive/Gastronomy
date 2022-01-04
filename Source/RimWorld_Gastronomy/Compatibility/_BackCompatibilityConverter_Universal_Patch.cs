using System;
using System.Xml;
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
			internal static bool Prefix(Type baseType, string providedClassName, XmlNode node, ref Type __result)
			{
                if (baseType == typeof(MapComponent))
                {
                    if (providedClassName == "Gastronomy.Restaurant.RestaurantController" || providedClassName == "Gastronomy.RestaurantController") // also old namespace
                    {
						// Give restaurant a name
                        var nameNode = node.OwnerDocument.CreateNode(XmlNodeType.Element, "name", null);
                        nameNode.InnerText = "RestaurantDefaultName".Translate(1);
						node.AppendChild(nameNode);
						// Wrap old restaurant controller in restaurants component
                        node.InnerXml = $@"<restaurants>{node.OuterXml}</restaurants>";
                        node.Attributes["Class"].Value = "Gastronomy.Restaurant.RestaurantsManager";

                        __result = typeof(RestaurantsManager);
                        return false;
                    }
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
