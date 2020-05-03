using RimWorld;
using Verse;

namespace Restaurant.Dining
{
    public class DiningSpot : Building_NutrientPasteDispenser
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            Log.Message($"Dining spot created at {Position}. Spawned? {Spawned}");
            Patching.ThingWithComps.SpawnSetup.Base(this, map, respawningAfterLoad);
        }

        
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Log.Message($"Dining spot removed at {Position}.");
            Patching.ThingWithComps.DeSpawn.Base(this, mode);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Log.Message($"Dining spot destroyed at {Position}. Spawned? {Spawned}");
            Patching.ThingWithComps.Destroy.Base(this, mode);
        }
    }
}