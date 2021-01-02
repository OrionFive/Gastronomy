using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace Gastronomy.TableTops
{
	public class JobDriver_EmptyRegister : JobDriver
	{
		private const TargetIndex IndexRegister = TargetIndex.A;
		private const TargetIndex IndexSilver = TargetIndex.B;
		private Building_CashRegister Register => job.GetTarget(IndexRegister).Thing as Building_CashRegister;
		private Thing Silver => job.GetTarget(IndexSilver).Thing;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			var register = Register;

			return pawn.Reserve(register, job, 1, 1, null, errorOnFailed);
		}
		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedOrNull(IndexRegister);
			this.FailOnForbidden(IndexRegister);
			yield return Toils_Goto.GotoThing(IndexRegister, PathEndMode.Touch);
			yield return Toils_General.Do(GetSilver);
			//yield return Toils_Haul.StartCarryThing(IndexSilver, true).FailOnDestroyedOrNull(IndexSilver);
			yield return Toils_General.DoAtomic(Haul).FailOnDestroyedOrNull(IndexSilver);
		}

		private void Haul()
		{
			if (HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, Silver, false))
			{
				pawn.jobs.StartJob(HaulAIUtility.HaulToStorageJob(pawn, Silver));
			}
		}

		private void GetSilver()
		{
			Register.GetDirectlyHeldThings().TryDrop(Silver, ThingPlaceMode.Near, out _, (thing, i) => pawn.CurJob.SetTarget(IndexSilver, thing));
		}
	}
}