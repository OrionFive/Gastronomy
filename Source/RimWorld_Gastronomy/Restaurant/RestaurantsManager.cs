using System.Collections.Generic;
using System.Linq;
using CashRegister;
using JetBrains.Annotations;
using Verse;

namespace Gastronomy.Restaurant
{
    public class RestaurantsManager : MapComponent
    {
        public List<RestaurantController> restaurants = new List<RestaurantController>();

        public RestaurantsManager(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref restaurants, "restaurants", LookMode.Deep, map);
            restaurants ??= new List<RestaurantController>();
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            if (restaurants.Count == 0) AddRestaurant();
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

        public RestaurantController GetLinkedRestaurant([NotNull]Building_CashRegister register)
        {
            return restaurants.FirstOrDefault(controller => controller.Registers.Contains(register));
        }

        [NotNull]
        public RestaurantController AddRestaurant()
        {
            var restaurant = new RestaurantController(map);
            restaurants.Add(restaurant);

            // Find an unused name, numbering upwards
            for (int i = 0; i < 100; i++)
            {
                var name = "RestaurantDefaultName".Translate(restaurants.Count + i);
                if (NameIsInUse(name, restaurant)) continue;

                restaurant.Name = name;
                break;
            }
            restaurant.FinalizeInit();
            return restaurant;
        }

        public void DeleteRestaurant(RestaurantController restaurant)
        {
            restaurant?.CleanUpForRemoval();
            restaurants.Remove(restaurant);
        }

        public bool NameIsInUse(string name, RestaurantController restaurant)
        {
            return restaurants.Any(controller => controller != restaurant && controller.Name == name);
        }
    }
}