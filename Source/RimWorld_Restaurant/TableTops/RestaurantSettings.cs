using System.Collections.Generic;
using Restaurant.Dining;
using Verse;

namespace Restaurant.TableTops
{
    public class RestaurantSettings : MapComponent
    {
        public IntVec3 testPos;

        public readonly List<DiningSpot> diningSpots = new List<DiningSpot>();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref testPos, "testPos");
        }

        public RestaurantSettings(Map map) : base(map) { }

        public override void FinalizeInit()
        {
            base.FinalizeInit();

            diningSpots.Clear();
            diningSpots.AddRange(DiningUtility.GetAllDiningSpots(map));
        }
    }
}
