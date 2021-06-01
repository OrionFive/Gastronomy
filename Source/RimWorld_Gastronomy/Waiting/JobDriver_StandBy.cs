using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Gastronomy.Waiting
{
	public class JobDriver_StandBy : JobDriver
	{
		private const TargetIndex IndexRegister = TargetIndex.A;
		private const TargetIndex IndexRegisterInteractionCell = TargetIndex.B;
		private const TargetIndex IndexStandByCell = TargetIndex.C;

		public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

		protected override IEnumerable<Toil> MakeNewToils()
		{
			var begin = Toils_General.Wait(10, IndexRegister);
			//Toils_Waiting.GetInteractionCell(IndexRegister, IndexRegisterInteractionCell);
			var end = Toils_General.Wait(10, IndexRegister);

			this.FailOnDestroyedOrNull(IndexRegister);
			this.FailOnForbidden(IndexRegister);
			yield return begin;
			yield return Toils_Misc.FindRandomAdjacentReachableCell(IndexRegisterInteractionCell, IndexStandByCell);
			yield return Toils_Jump.JumpIf(begin, () => pawn.CurJob?.GetTarget(IndexStandByCell).Cell.GetThingList(Map).Any(t => t is Pawn) == true);
			yield return Toils_Jump.JumpIf(end, () => pawn.CurJob?.GetTarget(IndexStandByCell).IsValid == false);
			yield return Toils_Goto.GotoCell(IndexStandByCell, PathEndMode.OnCell);
			yield return Toils_Waiting.WaitForBetterJob(IndexRegister);
			yield return end;
		}
	}
}
