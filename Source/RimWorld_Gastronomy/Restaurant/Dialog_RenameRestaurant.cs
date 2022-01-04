using Verse;

namespace Gastronomy.Restaurant
{
    internal class Dialog_RenameRestaurant : Dialog_Rename
    {
        private readonly RestaurantController restaurant;

        public Dialog_RenameRestaurant(RestaurantController restaurant)
        {
            this.restaurant = restaurant;
            curName = restaurant.Name;
        }

        public override void SetName(string name)
        {
            restaurant.Name = name;
        }

        public override AcceptanceReport NameIsValid(string name)
        {
            var result = base.NameIsValid(name);
            if (!result.Accepted) return result;
            if (restaurant.GetRestaurantsManager().NameIsInUse(name, restaurant))
            {
                return "NameIsInUse".Translate();
            }
            return true;
        }
    }
}