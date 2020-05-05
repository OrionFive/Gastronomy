using Verse;

namespace Restaurant.TableTops
{
    public class RestaurantSettings : IExposable
    {
        public IntVec3 testPos;

        public void ExposeData()
        {
            Scribe_Values.Look(ref testPos, "testPos");
        }
    }
}
