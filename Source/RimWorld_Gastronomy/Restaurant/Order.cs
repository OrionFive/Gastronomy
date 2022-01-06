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
        public RestaurantController restaurant;

        public Order(RestaurantController restaurant)
        {
            this.restaurant = restaurant;
        }

        public void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                restaurantIndex = consumable.GetAllRestaurants().IndexOf(restaurant);
            }
            Scribe_References.Look(ref patron, "patron");
            Scribe_Defs.Look(ref consumableDef, "consumableDef");
            Scribe_References.Look(ref consumable, "consumable");
            Scribe_Values.Look(ref hasToBeMade, "hasToBeMade");
            Scribe_Values.Look(ref delivered, "delivered");
            Scribe_Values.Look(ref restaurantIndex, "restaurantIndex");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                try
                {
                    restaurant = consumable.GetAllRestaurants()[restaurantIndex];
                }
                catch
                {
                    Log.Message($"Couldn't resolve restaurant for order of {consumableDef?.label} by {patron?.Name.ToStringShort}.");
                }
            }
        }
    }
}
