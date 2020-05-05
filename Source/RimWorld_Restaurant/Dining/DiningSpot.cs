using Restaurant.TableTops;
using RimWorld;
using Verse;
using ThingWithComps = Restaurant.Patching.ThingWithComps;

namespace Restaurant.Dining
{
    /// <summary>
    /// This is currently a NutrientPasteDispenser... but maybe that's not needed at all
    /// </summary>
    public class DiningSpot : Building_NutrientPasteDispenser
    {
        public CashRegister register;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            ThingWithComps.SpawnSetup.Base(this, map, respawningAfterLoad);
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            ThingWithComps.DeSpawn.Base(this, mode);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            ThingWithComps.Destroy.Base(this, mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref register, "register");
        }

        #region NutrientPasteDispenser overrides

        public override bool HasEnoughFeedstockInHoppers()
        {
            return true;
        }

        public override Building AdjacentReachableHopper(Pawn pawn)
        {
            return null;
        }

        #endregion
    }
}
