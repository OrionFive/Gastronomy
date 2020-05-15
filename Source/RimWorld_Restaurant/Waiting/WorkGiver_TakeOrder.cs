using System.Collections.Generic;
using Restaurant.Dining;
using RimWorld;
using Verse;
using Verse.AI;

namespace Restaurant.Waiting
{
    public class WorkGiver_TakeOrder : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            Log.Message($"{pawn.GetRestaurant().SpawnedDiningPawns.Count} dining pawns...");
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
            var driver = p.GetDriver<JobDriver_Dine>();
            if (driver == null || !driver.wantsToOrder) return false;

            var canReserve = pawn.Map.reservationManager.CanReserve(pawn, p, 1, -1, null, forced);
            if (!canReserve)
            {
                var reserver = pawn.Map.reservationManager.FirstRespectedReserver(p, pawn);
                Log.Message($"{pawn.NameShortColored} can't reserve {p.NameShortColored}. Is reserved by {reserver?.NameShortColored}. ");
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var patron = (Pawn)t;
            var driver = patron.GetDriver<JobDriver_Dine>();
            var diningSpot = driver?.DiningSpot;

            if (diningSpot == null)
            {
                Log.Message($"{pawn.NameShortColored} couldn't take order from {patron.NameShortColored}: patronJob = {patron.jobs.curDriver?.GetType().Name}");
                return null;
            }
            Log.Message($"{pawn.NameShortColored} can get a take order job on {patron.NameShortColored}.");

            return JobMaker.MakeJob(WaitingUtility.takeOrdersDef, diningSpot, patron);
        }
    }
}
