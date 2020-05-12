using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Restaurant.Dining
{
    public class JobDriver_Dine : JobDriver
    {
        public DiningSpot DiningSpot => job.GetTarget(TargetIndex.A).Thing as DiningSpot;
        public Thing Food => job.GetTarget(TargetIndex.C).Thing;
        public Pawn Waiter => job.GetTarget(TargetIndex.B).Pawn;

        public bool wantsToOrder;
        private ThingDef preferredFoodDef;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref wantsToOrder, "wantsToOrder");
            Scribe_Defs.Look(ref preferredFoodDef, "preferredFoodDef");
        }

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

            preferredFoodDef = job.plantDefToSow; // Abusing this for storage of def
            return true;
        }

        //public override string GetReport()
        //{
        //    //if (job?.plantDefToSow == null) return base.GetReport();
        //    return "JobDineGoReport".Translate();
        //}

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Declare these early - jumping points
            var waitForWaiter = DiningUtility.WaitForWaiter(TargetIndex.A, TargetIndex.B).FailOnRestaurantClosed();
            var waitForMeal = DiningUtility.WaitForMeal(pawn, TargetIndex.B, TargetIndex.C);

            this.FailOn(() => DiningSpot.Destroyed);
            yield return DiningUtility.GoToDineSpot(pawn, TargetIndex.A).FailOnRestaurantClosed();
            yield return DiningUtility.TurnToEatSurface(TargetIndex.A);
            yield return Toils_Jump.JumpIf(waitForMeal, () => pawn.GetRestaurant().GetOrderFor(pawn) != null);
            yield return waitForWaiter;
            yield return waitForMeal;
            yield return Toils_Reserve.Reserve(TargetIndex.C);
            yield return Toils_Ingest.FinalizeIngest(pawn, TargetIndex.C);
            yield return Toils_Jump.JumpIf(waitForWaiter, () => pawn.needs.food.CurLevelPercentage < 0.9f);
        }

        public void OnOrderTaken(ThingDef foodDef, Pawn waiter)
        {
            wantsToOrder = false; // This triggers WaitForWaiter
            Log.Message($"{pawn.NameShortColored}'s order has been taken by {waiter.NameShortColored}.");
        }

        public void ServeFood(Thing food)
        {
            job.SetTarget(TargetIndex.C, food); // This triggers WaitForMeal
        }
    }
}
