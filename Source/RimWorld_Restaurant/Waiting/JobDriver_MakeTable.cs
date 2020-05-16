using System.Collections.Generic;
using Restaurant.Dining;
using Verse;
using Verse.AI;

namespace Restaurant.Waiting {
    public class JobDriver_MakeTable : JobDriver
    {
        private DiningSpot DiningSpot => job.GetTarget(TargetIndex.A).Thing as DiningSpot;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            var diningSpot = DiningSpot;

            if (!pawn.Reserve(diningSpot, job, 1, 1, null, errorOnFailed))
            {
                Log.Message($"{pawn.NameShortColored} FAILED to reserve dining spot for making table at {diningSpot.Position}.");
                return false;
            }

            Log.Message($"{pawn.NameShortColored} reserved spot for making table {diningSpot.Position}.");
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var begin = WaitingUtility.GetDiningSpotCellForMakingTable(TargetIndex.A, TargetIndex.B);
            var end = Toils_General.Wait(10, TargetIndex.A);

            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnForbidden(TargetIndex.A);
            yield return begin;
            yield return Toils_Jump.JumpIf(end, () => pawn.CurJob?.GetTarget(TargetIndex.B).IsValid == false);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.OnCell);
            yield return WaitingUtility.MakeTableReady(TargetIndex.A, TargetIndex.B);
            yield return end;
        }
    }
}
