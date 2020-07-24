using System.Collections.Generic;
using Restaurant.Dining;
using RimWorld;
using Verse;
using Verse.AI;

namespace Restaurant.Waiting
{
    public class JobDriver_TakeOrder : JobDriver
    {
        private DiningSpot DiningSpot => job.GetTarget(TargetIndex.A).Thing as DiningSpot;
        private Pawn Patron => job.GetTarget(TargetIndex.B).Pawn;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            var patron = Patron;
            var patronJob = patron.jobs.curDriver as JobDriver_Dine;
            var diningSpot = patronJob?.DiningSpot;

            if (diningSpot == null)
            {
                Log.Message($"{pawn.NameShortColored} couldn't take order from {patron?.NameShortColored}: patronJob = {patron.jobs.curDriver?.GetType().Name}");
                return false;
            }

            if (!pawn.Reserve(patron, job, 1, -1, null, errorOnFailed))
            {
                Log.Message($"{pawn.NameShortColored} FAILED to reserve patron {patron.NameShortColored}.");
                return false;
            }

            Log.Message($"{pawn.NameShortColored} reserved patron {patron.NameShortColored}.");
            job.SetTarget(TargetIndex.A, diningSpot);
            return true;
        }

        //public override string GetReport()
        //{
        //    //if (job?.plantDefToSow == null) return base.GetReport();
        //    return "JobDineGoReport".Translate();
        //}

        protected override IEnumerable<Toil> MakeNewToils()
        {
            var wait = Toils_General.Wait(50, TargetIndex.A).FailOnNotDiningQueued(TargetIndex.B);
            var end = Toils_General.Wait(5);

            this.FailOnNotDiningQueued(TargetIndex.B);

            yield return Toils_Waiting.FindRandomAdjacentCell(TargetIndex.A, TargetIndex.A); // A is first the dining spot, then where we'll stand
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell).FailOnRestaurantClosed();
            yield return Toils_Interpersonal.WaitToBeAbleToInteract(pawn);
            yield return wait;
            yield return Toils_Jump.JumpIf(wait, () => !(Patron?.jobs.curDriver is JobDriver_Dine)); // Not dining right now
            yield return Toils_Waiting.GetDiningSpot(TargetIndex.B, TargetIndex.A);
            yield return Toils_Waiting.TakeOrder(TargetIndex.B);
            yield return Toils_Interpersonal.SetLastInteractTime(TargetIndex.B);
            yield return Toils_Jump.JumpIf(end, () => DiningSpot?.IsSpotReady(WaitingUtility.GetChairPosition(Patron)) == true);
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return Toils_Waiting.MakeTableReady(TargetIndex.A, TargetIndex.B);
            yield return end;
        }
    }
}
