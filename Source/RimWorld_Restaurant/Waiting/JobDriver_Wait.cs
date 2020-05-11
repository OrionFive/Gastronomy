using System.Collections.Generic;
using Restaurant.Dining;
using RimWorld;
using Verse;
using Verse.AI;

namespace Restaurant.Waiting
{
    public class JobDriver_Wait : JobDriver
    {
        private Thing Food => job.GetTarget(TargetIndex.C).Thing;
        private Pawn Patron => job.GetTarget(TargetIndex.B).Pawn;
        //private ThingDef OrderedFoodDef => job.plantDefToSow; // Abusing this for storage of def

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            var patron = Patron;
            var patronJob = patron.jobs.curDriver as JobDriver_Dine;
            var diningSpot = patronJob?.DiningSpot;

            if (diningSpot == null)
            {
                Log.Message($"{pawn.NameShortColored} couldn't serve {patron?.NameShortColored}: patronJob = {patron.jobs.curDriver?.GetType().Name}");
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
            this.FailOnNotDining(TargetIndex.B);
            yield return Toils_Misc.FindRandomAdjacentReachableCell(TargetIndex.A, TargetIndex.A); // A is first the dining spot, then where we'll stand
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell).FailOnRestaurantClosed();
            yield return Toils_Interpersonal.WaitToBeAbleToInteract(pawn);
            yield return WaitingUtility.TakeOrder(pawn, TargetIndex.B);
        }
    }
}
