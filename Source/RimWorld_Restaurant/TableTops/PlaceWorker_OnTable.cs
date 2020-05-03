using System.Collections.Generic;
using Verse;

namespace Restaurant.TableTops
{
    public class PlaceWorker_OnTable : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            IEnumerable<Thing> things = map.thingGrid.ThingsAt(loc);

            bool hasTable = false;
            bool isBlocked = false;
            foreach (var t in things)
            {
                if (t is TableTop) isBlocked = true;
                if (t.def.surfaceType == SurfaceType.Eat) hasTable = true;
            }

            if (!hasTable)
            {
                return "MustPlaceOnTable".Translate();
            }

            if (isBlocked)
            {
                return "MustPlaceOnFreeSurface".Translate();
            }
            return true;
        }

        public override bool ForceAllowPlaceOver(BuildableDef otherDef)
        {
            return otherDef is ThingDef thingDef && thingDef.surfaceType == SurfaceType.Eat;
        }        
    }
}
