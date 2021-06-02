using System.Collections.Generic;
using Gastronomy.Restaurant;
using Gastronomy.TableTops;
using RimWorld;
using Verse;
using Verse.AI;

namespace Gastronomy.Waiting
{
	public class WorkGiver_StandBy : WorkGiver_Scanner
	{
		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => pawn.GetRestaurant().Registers;

		public override bool ShouldSkip(Pawn pawn, bool forced = false)
		{
			var restaurant = pawn.GetRestaurant();

			return !forced && !restaurant.HasToWork(pawn);
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is Building_CashRegister register)) return false;
			if (!register.HasToWork(pawn) || !register.standby) return false;
			if (RestaurantUtility.IsRegionDangerous(pawn, JobUtility.MaxDangerServing, register.GetRegion()) && !forced) return false;
			return true;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			var register = (Building_CashRegister) t;

			return JobMaker.MakeJob(WaitingUtility.standByDef, register, register.InteractionCell);
		}
	}
}
