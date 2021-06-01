using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Gastronomy.Waiting
{
	public class Alert_NoWaiter : Alert
	{
		protected string explanationKey;
		private float nextCheck;
		private bool getReport;

		// ReSharper disable once PublicConstructorInAbstractClass
		public Alert_NoWaiter()
		{
			defaultLabel = "AlertNoWaiter".Translate();
			defaultExplanation = "AlertNoWaiterExplanation".Translate();
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
				if (restaurant.Registers.Count == 0) continue;
				if (restaurant.Registers.Any(r => r.IsActive)) continue;
				
				getReport = true;
				break;
			}
		}
	}
}