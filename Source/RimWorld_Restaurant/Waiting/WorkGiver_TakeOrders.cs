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
            return pawn.Map.GetSettings().SpawnedDiningPawns;
        }

        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (!InteractionUtility.CanInitiateInteraction(pawn)) return true;

            var list = pawn.Map.GetSettings().SpawnedDiningPawns;

            return !list.Any(p => {
                var driver = p.jobs.curDriver as JobDriver_Dine;
                return driver?.wantsToOrder == true;
            });
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!(t is Pawn p)) return false;
            return p.jobs.curDriver is JobDriver_Dine dine && dine.wantsToOrder;
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

            return JobMaker.MakeJob(WaitingUtility.waitDef, diningSpot, patron);
        }
    }
}
