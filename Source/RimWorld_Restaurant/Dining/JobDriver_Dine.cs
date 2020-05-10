using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Restaurant.Dining
{
    public class JobDriver_Dine : JobDriver
    {
        private DiningSpot DiningSpot => job.GetTarget(TargetIndex.A).Thing as DiningSpot;
        private Thing Food => job.GetTarget(TargetIndex.C).Thing;
        private Pawn Waiter => job.GetTarget(TargetIndex.B).Pawn;
        private ThingDef PreferredFoodDef => job.plantDefToSow; // Abusing this for storage of def

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (pawn.Faction != null) // Not sure why this check is needed
            {
                var diningSpot = DiningSpot;

                if (!pawn.Reserve(diningSpot, job, diningSpot.GetMaxReservations(), 0, null, errorOnFailed))
                {
                    Log.Message($"{pawn.NameShortColored} FAILED to reserve dining spot at {diningSpot.Position}.");
                    return false;
                }

                Log.Message($"{pawn.NameShortColored} reserved dining spot at {diningSpot.Position}.");
            }

            return true;
        }

        public override string GetReport()
        {
            //if (job?.plantDefToSow == null) return base.GetReport();
            return "JobDineGoReport".Translate();
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOn(() => DiningSpot.Destroyed);
            yield return DiningUtility.GoToDineSpot(pawn, TargetIndex.A).FailOnRestaurantClosed();
            yield return DiningUtility.TurnToEatSurface(TargetIndex.A);
            var waitForWaiter = DiningUtility.WaitForWaiter(pawn, TargetIndex.A, TargetIndex.B).FailOnRestaurantClosed();
            yield return waitForWaiter;
            yield return DiningUtility.Order(pawn, TargetIndex.B);
            yield return DiningUtility.WaitForMeal(pawn, TargetIndex.B, TargetIndex.C);
            //yield return Toils_Ingest.FinalizeIngest(pawn, TargetIndex.C);
            yield return Toils_Jump.JumpIf(waitForWaiter, () => pawn.needs.food.CurLevelPercentage < 0.9f);
        }
    }
}
