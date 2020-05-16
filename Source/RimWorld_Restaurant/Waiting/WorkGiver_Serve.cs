using System.Collections.Generic;
using System.Linq;
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
            return !pawn.GetRestaurant().AvailableOrdersForServing.Any();
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var restaurant = pawn.GetRestaurant();
            if (pawn == t) return false;
            if (!pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.None, 1, 1)) return false;
            var anyOrder = restaurant.AvailableOrdersForServing.FirstOrDefault(o => o.consumableDef == t.def && !restaurant.IsBeingDelivered(o, pawn) && o.patron?.HasDiningQueued() == true);
            if (anyOrder == null) return false;
            if (!anyOrder.patron.Spawned || anyOrder.patron.Dead)
            {
                Log.Message($"Order canceled. null? {anyOrder.patron == null} dead? {anyOrder.patron.Dead} unspawned? {!anyOrder.patron?.Spawned}");
                restaurant.CancelOrder(anyOrder);
                return false;
            }

            Log.Message($"{pawn.NameShortColored} can serve {t.Label} to {anyOrder.patron.NameShortColored}.");
            anyOrder.hasToBeMade = false;
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var restaurant = pawn.GetRestaurant();
            var order = restaurant.AvailableOrdersForServing.FirstOrDefault(o => o.consumableDef == t.def && !restaurant.IsBeingDelivered(o, pawn));

            return JobMaker.MakeJob(WaitingUtility.serveDef, order.patron, t);
        }
    }
}
