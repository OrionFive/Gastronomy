using Verse;

namespace Restaurant {
    internal class Order : IExposable
    {
        public Pawn patron;
        public ThingDef consumable;

        public void ExposeData()
        {
            Scribe_References.Look(ref patron, "patron");
            Scribe_Defs.Look(ref consumable, "consumable");
        }
    }
}
