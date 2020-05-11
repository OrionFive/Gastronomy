using System.Collections.Generic;
using Restaurant.Dining;
using RimWorld;
using Verse;
using Verse.AI;

namespace Restaurant.Waiting
{
    public class JobDriver_Serve : JobDriver
    {
        private Pawn Patron => job.GetTarget(TargetIndex.A).Pawn;
        private Thing Food => job.GetTarget(TargetIndex.B).Thing;
        private IntVec3 DiningSpot => job.GetTarget(TargetIndex.C).Cell;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            var food = Food;
            var patron = Patron;
            var patronJob = patron.jobs.curDriver as JobDriver_Dine;
            var diningSpot = patronJob?.DiningSpot;

            if (diningSpot == null)
            {
                Log.Message($"{pawn.NameShortColored} couldn't serve {patron?.NameShortColored}: patronJob = {patron.jobs.curDriver?.GetType().Name}");
                return false;
            }

            if (!pawn.Reserve(food, job, 1, 1, null, errorOnFailed))
            {
                Log.Message($"{pawn.NameShortColored} FAILED to reserve food {food?.Label}.");
                return false;
            }

            Log.Message($"{pawn.NameShortColored} reserved food {food.Label}.");
            job.SetTarget(TargetIndex.C, diningSpot);
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnNotDining(TargetIndex.A);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_Haul.CarryHauledThingToCell(TargetIndex.C);
            yield return WaitingUtility.ClearOrder(TargetIndex.A, TargetIndex.B);
        }
    }

    public class JobDriver_TakeOrder : JobDriver
    {
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
            this.FailOnNotDining(TargetIndex.B);
            yield return WaitingUtility.FindRandomAdjacentCell(TargetIndex.A, TargetIndex.A); // A is first the dining spot, then where we'll stand
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell).FailOnRestaurantClosed();
            yield return Toils_Interpersonal.WaitToBeAbleToInteract(pawn);
            yield return WaitingUtility.TakeOrder(TargetIndex.B);
        }
    }
}
