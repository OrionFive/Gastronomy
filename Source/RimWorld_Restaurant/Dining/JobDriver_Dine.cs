using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Restaurant.Dining
{
    public class JobDriver_Dine : JobDriver
    {
        private ThingDef preferredFoodDef;

        public bool wantsToOrder;
        public DiningSpot DiningSpot => job.GetTarget(TargetIndex.A).Thing as DiningSpot;
        public Pawn Waiter => job.GetTarget(TargetIndex.B).Pawn;
        public Thing Food => job.GetTarget(TargetIndex.C).Thing;

        //public override string GetReport()
        //{
        //    //if (job?.plantDefToSow == null) return base.GetReport();
        //    return "JobDineGoReport".Translate();
        //}

        private float ChewDurationMultiplier => 1f / pawn.GetStatValue(StatDefOf.EatingSpeed);

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

                if (diningSpot == null || !diningSpot.Spawned || !pawn.Reserve(diningSpot, job, diningSpot.GetMaxReservations(), 0, null, errorOnFailed))
                {
                    Log.Message($"{pawn.NameShortColored} FAILED to reserve dining spot at {diningSpot.Position}.");
                    return false;
                }
            }

            preferredFoodDef = job.plantDefToSow; // Abusing this for storage of def
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            // Declare these early - jumping points
            var waitForWaiter = DiningUtility.WaitForWaiter(TargetIndex.A, TargetIndex.B).FailOnRestaurantClosed().FailOnDangerous();
            var waitForMeal = DiningUtility.WaitForMeal(TargetIndex.B, TargetIndex.C).FailOnDangerous();

            this.FailOn(() => DiningSpot.Destroyed);
            yield return DiningUtility.GoToDineSpot(pawn, TargetIndex.A).FailOnRestaurantClosed();
            yield return DiningUtility.TurnToEatSurface(TargetIndex.A);
            yield return Toils_Jump.JumpIf(waitForMeal, () => pawn.GetRestaurant().GetOrderFor(pawn) != null);
            yield return waitForWaiter;
            yield return waitForMeal;
            yield return Toils_Misc.TakeItemFromInventoryToCarrier(pawn, TargetIndex.C);
            yield return Toils_Reserve.Reserve(TargetIndex.C, 1, 1);
            yield return DiningUtility.TurnToEatSurface(TargetIndex.A, TargetIndex.C);
            yield return DiningUtility.WaitDuringDinner(TargetIndex.A, 100, 250);
            yield return Toils_Ingest.ChewIngestible(pawn, ChewDurationMultiplier, TargetIndex.C, TargetIndex.A);
            yield return Toils_Ingest.FinalizeIngest(pawn, TargetIndex.C);
            yield return DiningUtility.OnCompletedMeal(pawn);
            yield return DiningUtility.MakeTableMessy(TargetIndex.A, () => pawn.Position);
            yield return Toils_Jump.JumpIf(waitForWaiter, () => pawn.needs.food.CurLevelPercentage < 0.9f);
            yield return DiningUtility.WaitDuringDinner(TargetIndex.A, 100, 250);
        }

        public void OnOrderTaken(ThingDef foodDef, Pawn waiter)
        {
            wantsToOrder = false; // This triggers WaitForWaiter
            Log.Message($"{pawn.NameShortColored}'s order has been taken by {waiter.NameShortColored}.");
        }

        public void OnTransferredFood(Thing food)
        {
            //Log.Message($"{pawn.NameShortColored} has taken {food.Label} to his inventory. {pawn.inventory.Contains(food)}");
            job.SetTarget(TargetIndex.C, food); // This triggers WaitForMeal
        }

        // Mostly copied from JobDriver_Ingest
        public override bool ModifyCarriedThingDrawPos(ref Vector3 drawPos, ref bool behind, ref bool flip)
        {
            var placeCell = job.GetTarget(TargetIndex.A).Cell;
            if (pawn.pather.Moving) return false;
            Thing carriedThing = pawn.carryTracker.CarriedThing;
            if (carriedThing == null || !carriedThing.IngestibleNow) return false;
            if (placeCell.IsValid && placeCell.AdjacentToCardinal(pawn.Position) && placeCell.HasEatSurface(pawn.Map) && carriedThing.def.ingestible.ingestHoldUsesTable)
            {
                drawPos = new Vector3((placeCell.x + pawn.Position.x) * 0.5f + 0.5f, drawPos.y, (placeCell.z + pawn.Position.z) * 0.5f + 0.5f);
                behind = pawn.Rotation != Rot4.South;
                return true;
            }

            if (carriedThing.def.ingestible.ingestHoldOffsetStanding != null)
            {
                HoldOffset holdOffset = carriedThing.def.ingestible.ingestHoldOffsetStanding.Pick(pawn.Rotation);
                if (holdOffset != null)
                {
                    drawPos += holdOffset.offset;
                    behind = holdOffset.behind;
                    flip = holdOffset.flip;
                    return true;
                }
            }

            return false;
        }
    }
}
