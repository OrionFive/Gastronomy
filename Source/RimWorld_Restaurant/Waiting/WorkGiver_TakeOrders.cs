using System.Collections.Generic;
using Restaurant.Dining;
using RimWorld;
using Verse;
using Verse.AI;

namespace Restaurant.Waiting
{
    public class WorkGiver_TakeOrders : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.GetRestaurant().SpawnedDiningPawns;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (!InteractionUtility.CanInitiateInteraction(pawn)) return true;

            var list = pawn.GetRestaurant().SpawnedDiningPawns;

            var anyPatrons = list.Any(p => {
                var driver = p.jobs.curDriver as JobDriver_Dine;
                return driver?.wantsToOrder == true;
            });
            return !anyPatrons;
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Pawn p)) return false;
            var canReserve = pawn.Map.reservationManager.CanReserve(pawn, p, 1, -1, null, forced);
            if (!canReserve)
            {
                var ignored = pawn.Map.reservationManager.CanReserve(pawn, p, 1, -1, null, true);
                var reserver = pawn.Map.reservationManager.FirstRespectedReserver(p, pawn);
                Log.Message($"{pawn.NameShortColored} can't reserve {p.NameShortColored}. Is reserved by {reserver?.NameShortColored} who is doing {reserver?.CurJobDef?.label}. " 
                            + $"When ignoring other reservation: canReserve = {ignored}");
                return false;
            }
            var wantsToOrder = p.jobs.curDriver is JobDriver_Dine dine && dine.wantsToOrder;
            return wantsToOrder;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var patron = (Pawn)t;
            var driver = patron.jobs.curDriver as JobDriver_Dine;
            var diningSpot = driver?.DiningSpot;

            if (diningSpot == null)
            {
                Log.Message($"{pawn.NameShortColored} couldn't serve {patron.NameShortColored}: patronJob = {patron.jobs.curDriver?.GetType().Name}");
                return null;
            }
            Log.Message($"{pawn.NameShortColored} can get a waiting job on {patron.NameShortColored}.");

            return JobMaker.MakeJob(WaitingUtility.waitDef, diningSpot, patron);
        }
    }
}
