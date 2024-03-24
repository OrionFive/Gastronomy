using Verse;

namespace Gastronomy.Restaurant
{
    internal class Dialog_RenameRestaurant : Dialog_Rename<RestaurantController>
    {
        public Dialog_RenameRestaurant(RestaurantController restaurantController) : base(restaurantController)
        {
        }
        
        public override AcceptanceReport NameIsValid(string name)
        {
            var result = base.NameIsValid(name);
            if (!result.Accepted) return result;
            if (renaming.GetRestaurantsManager().NameIsInUse(name, renaming))
            {
                return "NameIsInUse".Translate();
            }
            return true;
        }        
    }
}