using RimWorld;
using Verse;

namespace Restaurant.Dining
{
    public class DiningSpot : Building_NutrientPasteDispenser
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            Patching.ThingWithComps.SpawnSetup.Base(this, map, respawningAfterLoad);
        }
        
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Patching.ThingWithComps.DeSpawn.Base(this, mode);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            Patching.ThingWithComps.Destroy.Base(this, mode);
        }
    }
}