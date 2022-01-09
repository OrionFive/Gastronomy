using System.Collections.Generic;
using Gastronomy.Dining;
using Gastronomy.Restaurant;
using RimWorld;
using Verse;
using Verse.AI;

namespace Gastronomy.Waiting
{
    public class JobDriver_Serve : JobDriver
    {
        private Pawn Patron => job.GetTarget(TargetIndex.A).Pawn;
        private Thing Food => job.GetTarget(TargetIndex.B).Thing;
        private Thing Silver => job.GetTarget(TargetIndex.B).Thing;
        private IntVec3 DiningSpot => job.GetTarget(TargetIndex.C).Cell;
        private Thing Register => job.GetTarget(TargetIndex.C).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            var food = Food;
            var patron = Patron;
            var patronJob = patron.GetDriver<JobDriver_Dine>();
            var diningSpot = patronJob?.DiningSpot;

            var order = RestaurantUtility.WaiterGetOrderFor(pawn, patron);
            if (order == null)
            {
                Log.Message($"{patron.NameShortColored} has no existing order.");
                return false;
            }

            if (order.Restaurant.Orders.IsBeingDelivered(order, pawn))
            {
                var waiter = patron.Map.reservationManager.FirstRespectedReserver(order.consumable, pawn);
                Log.Message($"{pawn.NameShortColored}: Order for {patron.NameShortColored} is already being delivered by {waiter?.NameShortColored}.");
                return false;
            }

            if (diningSpot == null)
            {
                Log.Warning($"{pawn.NameShortColored} FAILED to serve {patron?.NameShortColored}, because no dining spot is set. patronJob = {patron?.jobs.curDriver?.GetType().Name}");
                return false;
            }

            if (food == null)
            {
                Log.Warning($"{pawn.NameShortColored} FAILED to serve {patron.NameShortColored}, because food is not set.");
                return false;
            }

            if (food.ParentHolder != food.Map)
            {
                Log.Warning($"{pawn.NameShortColored} FAILED to serve {food.Label} to {patron.NameShortColored}, because it is inside {(food.ParentHolder is Thing parentThing ? parentThing.Label : food.ParentHolder.ToString())}");
                return false;
            }

            if (!pawn.Reserve(food, job, food.stackCount, 1, null, errorOnFailed))
            {
                Log.Message($"{pawn.NameShortColored} FAILED to reserve food {food.Label}.");
                return false;
            }

            order.consumable = food;
            order.hasToBeMade = false;

            //Log.Message($"{pawn.NameShortColored} reserved food {food.Label}.");
            job.count = 1;
            job.SetTarget(TargetIndex.C, diningSpot);
            return true;
        }

        public override IEnumerable<Toil> MakeNewToils()
        {
            var wait = Toils_General.Wait(50, TargetIndex.A).FailOnNotDiningQueued(TargetIndex.A);

            //this.FailOnNotDining(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnForbidden(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.B);
            yield return Toils_Waiting.UpdateOrderConsumableTo(TargetIndex.A, TargetIndex.B);
            yield return Toils_Waiting.FindRandomAdjacentCell(TargetIndex.A, TargetIndex.C); // A is the patron, C is the spot
            yield return Toils_Haul.CarryHauledThingToCell(TargetIndex.C);
            yield return wait;
            yield return Toils_Jump.JumpIf(wait, () => pawn.jobs.curJob?.GetTarget(TargetIndex.A).Pawn?.GetDriver<JobDriver_Dine>()==null); // Driver not available
            yield return Toils_Waiting.GetDiningSpot(TargetIndex.A, TargetIndex.C);
            yield return Toils_Haul.CarryHauledThingToCell(TargetIndex.C);
            yield return Toils_Jump.JumpIf(wait, () => pawn.jobs.curJob?.GetTarget(TargetIndex.A).Pawn?.GetDriver<JobDriver_Dine>()==null); // Driver not available
            yield return Toils_Waiting.AnnounceServing(TargetIndex.A, TargetIndex.B);
            yield return Toils_Waiting.ClearOrder(TargetIndex.A, TargetIndex.B, TargetIndex.B, TargetIndex.C); // Got no silver or register? Job successful
            yield return Toils_Misc.TakeItemFromInventoryToCarrier(pawn, TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.Touch);
            yield return Toils_Haul.DepositHauledThingInContainer(TargetIndex.C, TargetIndex.None).PlaySoundAtStart(WaitingDefOf.CashRegister_Register_Kaching);

        }
    }
}
