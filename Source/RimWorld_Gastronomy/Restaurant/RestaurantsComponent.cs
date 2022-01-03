using System.Collections.Generic;
using Verse;

namespace Gastronomy.Restaurant
{
    public class RestaurantsComponent : MapComponent
    {
        public List<RestaurantController> restaurants = new List<RestaurantController>();

        public RestaurantsComponent(Map map) : base(map)
        {
            Log.Message($"Restaurants component created.");
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Log.Message($"Got map reference? {map!=null}");
            Scribe_Collections.Look(ref restaurants, "restaurants", LookMode.Deep, map);
            restaurants ??= new List<RestaurantController>();
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            foreach (var restaurant in restaurants) restaurant.FinalizeInit();
        }

        public override void MapGenerated()
        {
            base.MapGenerated();
            foreach (var restaurant in restaurants) restaurant.MapGenerated();
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            RestaurantUtility.OnTick();
            foreach (var restaurant in restaurants) restaurant.OnTick();
        }
    }
}