using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Restaurant.Dining;
using RimWorld;
using Verse;
using Verse.AI;

namespace Restaurant.Waiting
{
    public class WorkGiver_Serve : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.HaulableAlways);

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            Log.Message($"{pawn.GetRestaurant().Stock.Count} potential work things...");
            return pawn.GetRestaurant().Stock;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return !pawn.GetRestaurant().AvailableOrders.Any();
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var restaurant = pawn.GetRestaurant();
            if (!pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.None, 1, 1)) return false;
            var anyOrder = restaurant.AvailableOrders.FirstOrDefault(o => !o.isBeingDelivered && o.consumableDef == t.def);
            if (anyOrder==null) return false;
            if (anyOrder.patron == null || !anyOrder.patron.Spawned || anyOrder.patron.Dead || anyOrder.patron.jobs?.curDriver is JobDriver_Dine)
            {
                Log.Message($"Order canceled. null? {anyOrder.patron == null} dead? unspawned? {!anyOrder.patron?.Spawned} driver? {anyOrder.patron?.jobs?.curDriver?.GetType().Name}");
                restaurant.CancelOrder(anyOrder);
                return false;
            }
            Log.Message($"{pawn.NameShortColored} has an order for {t.Label}.");
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var restaurant = pawn.GetRestaurant();
            var order = restaurant.AvailableOrders.FirstOrDefault(o => !o.isBeingDelivered && o.consumableDef == t.def);

            return JobMaker.MakeJob(WaitingUtility.serveDef, order.patron, t);
        }
    }
}
