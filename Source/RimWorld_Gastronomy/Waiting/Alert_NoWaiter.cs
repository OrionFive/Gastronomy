using System.Collections.Generic;
using System.Linq;
using CashRegister;
using Gastronomy.Restaurant;
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
                foreach (var restaurant in map.GetComponent<RestaurantsManager>().restaurants)
                {
                    if (restaurant == null) continue;
                    if (restaurant.diningSpots.Count == 0) continue;
                    if (!restaurant.IsOpenedRightNow) continue;
                    if (restaurant.Registers.Count == 0) continue;
                    if (restaurant.Registers.Any(r => r.shifts.Any(s => s.assigned.Count > 0))) continue;

                    getReport = true;
                    break;
                }
            }
        }
	}
}
