using System.Collections.Generic;
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

		public override IEnumerable<Toil> MakeNewToils()
		{
			var begin = Toils_General.Wait(10, IndexRegister);
			//Toils_Waiting.GetInteractionCell(IndexRegister, IndexRegisterInteractionCell);
			var end = Toils_General.Wait(10, IndexRegister);

			this.FailOnDestroyedOrNull(IndexRegister);
			this.FailOnForbidden(IndexRegister);
			yield return begin;
			yield return Toils_Waiting.FindRandomAdjacentCell(IndexRegisterInteractionCell, IndexStandByCell, 2);
			yield return Toils_Jump.JumpIf(begin, InvalidStandbyCell);
			yield return Toils_Jump.JumpIf(end, () => pawn.CurJob?.GetTarget(IndexStandByCell).IsValid == false);
			yield return Toils_Goto.GotoCell(IndexStandByCell, PathEndMode.OnCell);
			yield return Toils_Waiting.WaitForBetterJob(IndexRegister);
			yield return end;
		}

		private bool InvalidStandbyCell()
		{
			if (pawn.CurJob == null) return true;
			Thing thing = pawn.CurJob.GetTarget(IndexRegister).Thing;
			var position = thing.Position;
			var offset = thing.InteractionCell - position;
			var rotation = Rot4.FromIntVec3(offset);
			var left = rotation.Rotated(RotationDirection.Clockwise).FacingCell;
			var right = rotation.Rotated(RotationDirection.Counterclockwise).FacingCell;
			var cell = pawn.CurJob.GetTarget(IndexStandByCell).Cell;
			// Shouldn't be directly next to register
			if (cell == position + left) return true;
			if (cell == position + right) return true;
			// No pawns should be there
			return cell.GetFirstPawn(Map) != null;
		}
	}
}
