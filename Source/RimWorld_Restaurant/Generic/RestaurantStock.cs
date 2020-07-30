using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Restaurant
{
    public class RestaurantStock : IExposable
    {
        [NotNull] private readonly List<Thing> stock = new List<Thing>();
        [NotNull] public IEnumerable<Thing> AllStock => stock.AsReadOnly();
        [NotNull] private Map Map => Restaurant.map;
        [NotNull] private RestaurantMenu Menu => Restaurant.Menu;
        [NotNull] private RestaurantController Restaurant { get; }

        public RestaurantStock([NotNull] RestaurantController restaurant)
        {
            Restaurant = restaurant;
        }

        public void ExposeData() { }

        public bool HasAnyFoodFor([NotNull] Pawn pawn, bool allowDrug)
        {
            //Log.Message($"{pawn.NameShortColored}: HasFoodFor: Defs: {stock.Select(item=>item.def).Count(s => WillConsume(pawn, allowDrug, s))}");
            return stock.Select(item => item.def).Any(s => WillConsume(pawn, allowDrug, s));
        }

        public ThingDef GetBestFoodTypeFor([NotNull] Pawn pawn, bool allowDrug)
        {
            var best = stock.Select(item => item.def).Distinct().Where(def => WillConsume(pawn, allowDrug, def)).MaxBy(def => FoodOptimality(pawn, def));
            //Log.Message($"{pawn.NameShortColored}: GetBestFoodFor: {best?.label}");
            return best;
        }

        public ThingDef GetRandomFoodTypeFor([NotNull] Pawn pawn, bool allowDrug)
        {
            var random = stock.Select(item => item.def).Distinct().Where(def => WillConsume(pawn, allowDrug, def)).RandomElementByWeight(def => FoodOptimality(pawn, def));
            //Log.Message($"{pawn.NameShortColored}: GetBestFoodFor: {best?.label}");
            return random;
        }

        private float FoodOptimality(Pawn pawn, ThingDef def)
        {
            // Optimality can be negative
            Log.Message($"{pawn.NameShortColored} - {def.LabelCap}");
            var dummyFoodSource = stock[0]; // Can be null again once erdelf fixes the patch
            return Mathf.Max(0, FoodUtility.FoodOptimality(pawn, dummyFoodSource, def, 0));
        }

        private static bool WillConsume(Pawn pawn, bool allowDrug, ThingDef s)
        {
            return (allowDrug || !s.IsDrug) && pawn.WillEat(s);
        }

        public Thing GetServableThing(Order order, Pawn pawn)
        {
            return stock.Where(o => o.Spawned && o.def == order.consumableDef)
                .OrderBy(o => pawn.Position.DistanceToSquared(o.Position))
                .FirstOrDefault(o => pawn.CanReserveAndReach(o, PathEndMode.Touch, Danger.None, o.stackCount, 1));
        }

        public void RareTick()
        {
            // Refresh entire stock
            stock.Clear();
            stock.AddRange(Map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSource).Where(t => t.def.IsIngestible && Menu.IsOnMenu(t)));
        }
    }
}
