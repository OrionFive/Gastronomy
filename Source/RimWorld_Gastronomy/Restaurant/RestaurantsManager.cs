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
        private readonly Dictionary<Pawn, RestaurantController> diningAt = new Dictionary<Pawn, RestaurantController>();

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
            foreach (var restaurant in restaurants) restaurant.FinalizeInit();
            if (restaurants.Count == 0) AddRestaurant(); // AddRestaurant also calls FinalizeInit

            // Check unclaimed registers
            foreach (var register in map.listerBuildings.AllBuildingsColonistOfClass<Building_CashRegister>())
            {
                if (restaurants.Any(r => r.Registers.Contains(register))) continue;
                restaurants[0].LinkRegister(register);
            }
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

        public void RegisterDiningAt(Pawn patron, RestaurantController controller)
        {
            if (diningAt.TryGetValue(patron, out var current))
            {
                if (current == controller)
                {
                    //Log.Message($"{patron.NameShortColored} tried to register dining at {controller.Name}, but is already registered.");
                }
                else if (controller == null)
                {
                    diningAt.Remove(patron);
                    //Log.Message($"{patron.NameShortColored} has unregistered from dining at {current.Name}.");
                }
                else
                {
                    diningAt.Remove(patron);
                    Log.Message($"{patron.NameShortColored} has switched from dining at {current.Name} to {controller.Name}.");
                    diningAt.Add(patron, controller);
                }
            }
            else
            {
                if (controller != null)
                {
                    //Log.Message($"{patron.NameShortColored} is now registered as dining at {controller.Name}.");
                    diningAt.Add(patron, controller);
                }
                else
                {
                    Log.Warning($"{patron.NameShortColored} tried to unregister dining, but wasn't registered.");
                }
            }
        }

        public RestaurantController GetRestaurantDining(Pawn patron)
        {
            return diningAt.TryGetValue(patron, out var controller) ? controller : null;
        }

        public bool NameIsInUse(string name, RestaurantController restaurant)
        {
            return restaurants.Any(controller => controller != restaurant && controller.Name.EqualsIgnoreCase(name));
        }
    }
}