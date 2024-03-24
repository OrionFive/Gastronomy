using System.Collections.Generic;
using CashRegister;
using Gastronomy.Restaurant;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Gastronomy.Dining
{
    public class JobDriver_Dine : JobDriver
    {
        public bool wantsToOrder;
        private int startedWaitingTick;

        public DiningSpot DiningSpot => job.GetTarget(SpotIndex).Thing as DiningSpot;
        public Pawn Waiter => job.GetTarget(WaiterIndex).Pawn;
        public Thing Meal => job.GetTarget(MealIndex).Thing;

        private const TargetIndex SpotIndex = TargetIndex.A;
        private const TargetIndex WaiterIndex = TargetIndex.B;
        private const TargetIndex MealIndex = TargetIndex.C;

        public override string GetReport()
        {
            var restaurant = pawn.GetRestaurantsManager().GetRestaurantDining(pawn);
            return restaurant != null ? "JobDineGoReportSpecific".Translate(restaurant.Name) : "JobDineGoReport".Translate();
        }

        private float ChewDurationMultiplier => 1f / pawn.GetStatValue(StatDefOf.EatingSpeed);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref wantsToOrder, "wantsToOrder");
            Scribe_Values.Look(ref startedWaitingTick, "startedWaitingTick");
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
            return true;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            // Declare these early - jumping points
            var waitForWaiter = Toils_Dining.WaitForWaiter(SpotIndex, WaiterIndex);
            var waitForMeal = Toils_Dining.WaitForMeal(MealIndex, SpotIndex);

            this.FailOn(() => DiningSpot.Destroyed);
            yield return Toils_Dining.GoToDineSpot(pawn, SpotIndex).FailOnMyRestaurantClosedForDining();
            yield return Toils_Dining.TurnToEatSurface(SpotIndex);
            // Already has ordered? Jump to waiting for meal; Also set restaurant
            yield return Toils_Jump.JumpIf(waitForMeal, () =>
            {
                var order = pawn.FindValidOrder();
                var restaurant = pawn.GetRestaurantsManager().GetRestaurantDining(pawn);
                if (order != null && order.Restaurant != restaurant)
                {
                    Log.Warning($"{pawn.NameShortColored} is registered at {restaurant?.Name} but their order is at {order?.Restaurant?.Name}. Switching...");
                    pawn.GetRestaurantsManager().RegisterDiningAt(pawn, order.Restaurant);
                }
                return order != null;
            });
            yield return Toils_Dining.Obsolete();
            yield return waitForWaiter;
            yield return waitForMeal;
            yield return Toils_Misc.TakeItemFromInventoryToCarrier(pawn, MealIndex).FailOnDestroyedOrNull(MealIndex);
            //yield return Toils_Reserve.Reserve(MealIndex, 1, 1);
            yield return Toils_Dining.TurnToEatSurface(SpotIndex, MealIndex);
            yield return Toils_Dining.WaitDuringDinner(SpotIndex, 100, 250);
            yield return Toils_Ingest.ChewIngestible(pawn, ChewDurationMultiplier, MealIndex, SpotIndex);
            yield return Toils_Ingest.FinalizeIngest(pawn, MealIndex);
            yield return Toils_Dining.OnCompletedMeal(pawn);
            yield return Toils_Dining.MakeTableMessy(SpotIndex, () => pawn.Position);
            yield return Toils_Jump.JumpIf(waitForWaiter, () => pawn.needs.food.CurLevelPercentage < 0.9f);
            yield return Toils_General.DoAtomic(() => pawn.GetRestaurantsManager().RegisterDiningAt(pawn, null));
            yield return Toils_Dining.WaitDuringDinner(SpotIndex, 100, 250);
        }

        public void OnTransferredFood(Thing consumable, ThingOwner payTarget, out Thing paidSilver)
        {
            paidSilver = null;
            var hasIt = pawn.inventory.Contains(consumable);
            if (hasIt)
            {
                //Log.Message($"{pawn.NameShortColored} has taken {consumable.Label} to his inventory.");
                pawn.PayForMeal(payTarget, out paidSilver);
                job.SetTarget(MealIndex, consumable); // This triggers WaitForMeal
                if (pawn.CanHaveDebt())
                {
                    DiningUtility.GiveBoughtFoodThought(pawn);
                }
            }
            else
            {
                //Log.Warning($"{pawn.NameShortColored} doesn't have {consumable.Label} in his inventory.");
            }
        }

        public void OnStartedWaiting()
        {
            startedWaitingTick = GenTicks.TicksGame;
        }

        public float HoursWaited => (GenTicks.TicksGame - startedWaitingTick) * 1f / GenDate.TicksPerHour;


        // Mostly copied from JobDriver_Ingest
        public override bool ModifyCarriedThingDrawPos(ref Vector3 drawPos, ref bool flip)
        {
            var placeCell = job.GetTarget(SpotIndex).Cell;
            if (pawn.pather.Moving) return false;
            Thing carriedThing = pawn.carryTracker.CarriedThing;
            if (carriedThing == null || !carriedThing.IngestibleNow) return false;
            if (placeCell.IsValid && placeCell.AdjacentToCardinal(pawn.Position) && placeCell.HasEatSurface(pawn.Map) && carriedThing.def.ingestible.ingestHoldUsesTable)
            {
                drawPos = new Vector3((placeCell.x + pawn.Position.x) * 0.5f + 0.5f, drawPos.y, (placeCell.z + pawn.Position.z) * 0.5f + 0.5f);
                return true;
            }

            if (carriedThing.def.ingestible.ingestHoldOffsetStanding != null)
            {
                HoldOffset holdOffset = carriedThing.def.ingestible.ingestHoldOffsetStanding.Pick(pawn.Rotation);
                if (holdOffset != null)
                {
                    drawPos += holdOffset.offset;
                    flip = holdOffset.flip;
                    return true;
                }
            }

            return false;
        }
    }
}
