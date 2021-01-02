using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Gastronomy.TableTops
{
	public class WorkGiver_EmptyRegister : WorkGiver_Scanner
	{
		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => pawn.Map.listerBuildings.AllBuildingsColonistOfClass<Building_CashRegister>();

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			return t is Building_CashRegister register && register.ShouldEmpty;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			if (t is Building_CashRegister register)
			{
				var silver = register.GetDirectlyHeldThings()?.FirstOrDefault();
				if (silver != null)
				{
					return JobMaker.MakeJob(RegisterUtility.emptyRegisterDef, t, silver);
				}
			}

			return null;
		}
	}
}
