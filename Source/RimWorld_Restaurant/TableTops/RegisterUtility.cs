using System.Linq;
using JetBrains.Annotations;
using Verse;

namespace Restaurant.TableTops
{
    public static class RegisterUtility
    {
        public static ThingDef cashRegisterDef = ThingDef.Named("Restaurant_CashRegister");

        private static CashRegister GetFirstRegister([NotNull] Map map)
        {
            return map.listerThings.ThingsOfDef(cashRegisterDef)?.OfType<CashRegister>().FirstOrDefault();
        }

        public static RestaurantSettings GetSettings([NotNull] Map map)
        {
            return GetFirstRegister(map)?.settings ?? new RestaurantSettings();
        }
    }
}
