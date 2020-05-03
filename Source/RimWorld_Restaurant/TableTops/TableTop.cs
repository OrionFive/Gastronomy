using System.Linq;
using RimWorld;
using Verse;

namespace Restaurant
{
    public class TableTop : Building
    {
        private Building table;

        public Building Table => table;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            table = map.thingGrid.ThingsAt(Position).OfType<Building>().FirstOrDefault(b => b.def.surfaceType == SurfaceType.Eat);
        }

        public override void ExposeData()
        {
            Scribe_References.Look(ref table, "table");
            if (table == null) Log.Error($"TableTop has no table at {Position}!");
        }

        public virtual void Notify_BuildingDespawned(Thing thing)
        {
            if (thing == table)
            {
                table = null;
                this.Uninstall();
            }
        }
    }
}
