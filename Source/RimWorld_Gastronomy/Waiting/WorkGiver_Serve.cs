using System.Collections.Generic;
using System.Linq;
using Gastronomy.Dining;
using Gastronomy.Restaurant;
using RimWorld;
using Verse;
using Verse.AI;

namespace Gastronomy.Waiting
{
    public class WorkGiver_Serve : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => pawn.GetAllRestaurantsEmployed().SelectMany(r=>r.SpawnedDiningPawns).Distinct().ToArray();

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            return false;
            //var restaurant = pawn.GetRestaurant();
            //
            //// Serve even when shift just ended
            ////if(!forced && !restaurant.HasToWork(pawn)) return true;
            //
            //return !restaurant.Orders.AvailableOrdersForServing.Any();
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Pawn patron)) return false;

            if (pawn == t) return false;

            var driver = patron.GetDriver<JobDriver_Dine>();
            if (driver == null || driver.wantsToOrder) return false;

            var order = RestaurantUtility.WaiterGetOrderFor(pawn, patron);

            if (order == null) return false;
            if (order.delivered) return false;

            var restaurant = order.Restaurant;
            if (restaurant == null) return false;

            if (restaurant.Orders.IsBeingDelivered(order)) return false;


            if (!patron.Spawned || patron.Dead)
            {
                Log.Message($"Order canceled. null? {order.patron == null} dead? {order.patron.Dead} unspawned? {!order.patron?.Spawned}");
                restaurant.Orders.CancelOrder(order);
                return false;
            }

            //Log.Message($"{pawn.NameShortColored} is trying to serve {patron.NameShortColored} a {order.consumableDef.label}.");
            var consumable = restaurant.Stock.GetServableThing(order, pawn);

            // This can happen if everything is claimed or it's dangerous
            if (consumable == null) return false;

            // Stack already in use by someone?
            if (pawn.Map.reservationManager.FirstRespectedReserver(consumable, pawn) != null) return false;

            if (RestaurantUtility.IsRegionDangerous(pawn, JobUtility.MaxDangerServing, patron.GetRegion()) && !forced) return false;
            if (RestaurantUtility.IsRegionDangerous(pawn, JobUtility.MaxDangerServing, consumable.GetRegion()) && !forced) return false;

            //Log.Message($"{pawn.NameShortColored} can serve {consumable.Label} to {order.patron.NameShortColored}.");
            order.consumable = consumable; // Store for JobOnThing
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Pawn patron)) return null;

            var order = RestaurantUtility.WaiterGetOrderFor(pawn, patron);
            if(order == null) Log.Error($"{patron?.Name.ToStringShort} doesn't have an order, even though {pawn?.Name.ToStringShort} thinks they should have.");
            else
            {
                var consumable = order.consumable;
                if (consumable == null) Log.Error($"Consumable in order for {patron.NameShortColored} is suddenly null.");
                else
                {
                    return JobMaker.MakeJob(WaitingDefOf.Gastronomy_Serve, order.patron, consumable);
                }
            }

            return null;
        }
    }
}
