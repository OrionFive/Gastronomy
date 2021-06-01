using RimWorld;
using UnityEngine;
using Verse;

namespace Gastronomy.Waiting
{
	public class Alert_NoRegister : Alert
	{
		protected string explanationKey;
		private float nextCheck;
		private bool getReport;

		// ReSharper disable once PublicConstructorInAbstractClass
		public Alert_NoRegister()
		{
			defaultLabel = "AlertNoRegister".Translate();
			defaultExplanation = "AlertNoRegisterExplanation".Translate();
			defaultPriority = AlertPriority.High;
		}
		public override string GetLabel() => defaultLabel;

		public override AlertReport GetReport()
		{
			if (Time.realtimeSinceStartup > nextCheck)
			{
				nextCheck = Time.realtimeSinceStartup + 1.5f;
				CheckMaps();
			}

			return getReport;
		}

		private void CheckMaps()
		{
			getReport = false;

			foreach (var map in Find.Maps)
			{
				if (!map.IsPlayerHome || !map.mapPawns.AnyColonistSpawned) continue;
				var restaurant = map.GetComponent<RestaurantController>();
				if (restaurant == null) continue;
				if (restaurant.diningSpots.Count == 0) continue;
				if (restaurant.Registers.Count > 0) continue;

				getReport = true;
				break;
			}
		}
	}
}
