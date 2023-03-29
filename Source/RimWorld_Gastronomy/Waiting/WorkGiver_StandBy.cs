using System.Collections.Generic;
using System.Linq;
using CashRegister;
using Gastronomy.Restaurant;
using RimWorld;
using Verse;
using Verse.AI;

namespace Gastronomy.Waiting
{
	public class WorkGiver_StandBy : WorkGiver_Scanner
	{
		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.GetAllRestaurants().SelectMany(r => r.Registers).Distinct().ToArray();
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (!(t is Building_CashRegister register)) return false;
			if (!register.GetRestaurant().IsOpenedRightNow) return false;
			if (!register.HasToWork(pawn) || !register.standby) return false;
			if (RestaurantUtility.IsRegionDangerous(pawn, JobUtility.MaxDangerServing, register.GetRegion()) && !forced) return false;
			return true;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			var register = (Building_CashRegister) t;

			return JobMaker.MakeJob(WaitingDefOf.Gastronomy_StandBy, register, register.InteractionCell);
		}
	}
}
