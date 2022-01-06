using System;
using Verse;

namespace Gastronomy.Restaurant
{
    public class Order : IExposable
    {
        public Thing consumable;
        public ThingDef consumableDef;
        public Pawn patron;
        public bool hasToBeMade;
        public bool delivered;
        private int restaurantIndex;
        private RestaurantController restaurant;

        public Order(RestaurantController restaurant)
        {
            Restaurant = restaurant;
        }

        public RestaurantController Restaurant
        {
            get
            {
                if (restaurantIndex >= 0) restaurant ??= consumable?.GetAllRestaurants()[restaurantIndex];
                return restaurant;
            }
            set => restaurant = value;
        }

        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                restaurantIndex = patron.GetAllRestaurants().IndexOf(Restaurant);
            }
            Scribe_References.Look(ref patron, "patron");
            Scribe_Defs.Look(ref consumableDef, "consumableDef");
            Scribe_References.Look(ref consumable, "consumable");
            Scribe_Values.Look(ref hasToBeMade, "hasToBeMade");
            Scribe_Values.Look(ref delivered, "delivered");
            Scribe_Values.Look(ref restaurantIndex, "restaurantIndex");
        }
    }
}
