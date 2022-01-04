using System.Collections.Generic;
using CashRegister;
using Gastronomy.Restaurant;
using RimWorld;
using UnityEngine;
using Verse;

namespace Gastronomy.Waiting
{
	public class Alert_NoRegister : Alert
	{
		protected string explanationKey;
		private float nextCheck;
		private AlertReport report;

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

			return report;
		}

		private void CheckMaps()
		{
			report.active = false;

            foreach (var map in Find.Maps)
            {
                if (!map.IsPlayerHome || !map.mapPawns.AnyColonistSpawned) continue;
                foreach (var restaurant in map.GetComponent<RestaurantsManager>().restaurants)
                {
                    if (restaurant == null) continue;
                    if (restaurant.diningSpots.Count == 0) continue;
                    if (((IList<Building_CashRegister>)restaurant.Registers).Count > 0) continue;

                    report = new AlertReport { active = true, culpritsThings = new List<Thing>(restaurant.diningSpots) };
                    break;
                }
            }
        }
	}
}
