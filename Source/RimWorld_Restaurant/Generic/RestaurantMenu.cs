using RimWorld;
using Verse;

namespace Restaurant
{
    public class RestaurantMenu : IExposable
    {
        private ThingFilter menuFilter;
        private ThingFilter menuGlobalFilter;

        public void ExposeData()
        {
            Scribe_Deep.Look(ref menuFilter, "menuFilter");
        }

        public bool IsOnMenu(ThingDef def)
        {
            if (menuFilter == null) InitMenuFilter();
            return menuFilter.Allows(def);
        }

        public bool IsOnMenu(Thing thing)
        {
            if (menuFilter == null) InitMenuFilter();
            return menuFilter.Allows(thing);
        }

        public void GetMenuFilters(out ThingFilter filter, out ThingFilter globalFilter)
        {
            if (menuFilter == null) InitMenuFilter();
            filter = menuFilter;
            if (menuGlobalFilter == null) InitMenuGlobalFilter();
            globalFilter = menuGlobalFilter;
        }

        private void InitMenuFilter()
        {
            menuFilter = new ThingFilter();
            menuFilter.SetAllowAll(menuGlobalFilter);
        }

        private void InitMenuGlobalFilter()
        {
            menuGlobalFilter = new ThingFilter();
            menuGlobalFilter.SetAllow(ThingCategoryDefOf.Foods, true);
            menuGlobalFilter.SetAllow(ThingCategoryDefOf.Drugs, true);
            menuGlobalFilter.allowedQualitiesConfigurable = true;
        }
    }
}
