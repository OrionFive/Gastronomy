using System;
using System.Collections.Generic;
using System.Linq;
using Gastronomy.Dining;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Gastronomy.Restaurant
{
    public class RestaurantStock : IExposable
    {
        public class Stock
        {
            public ThingDef def;
            public int ordered;
            [NotNull] public readonly List<Thing> items = new List<Thing>();
        }

        private const float MinOptimality = 50;
        private const int JoyOptimalityWeight = 400;

        private class ConsumeOptimality
        {
            public Pawn pawn;
            public Thing thing;
            public float value;
        }

        //[NotNull] private readonly List<Thing> stockCache = new List<Thing>();
        [NotNull] private readonly List<ConsumeOptimality> eatOptimalityCache = new List<ConsumeOptimality>();
        [NotNull] private readonly List<ConsumeOptimality> joyOptimalityCache = new List<ConsumeOptimality>();
        [NotNull] private Map Map => Restaurant.Map;
        [NotNull] private RestaurantMenu Menu => Restaurant.Menu;
        [NotNull] private RestaurantController Restaurant { get; }
        [NotNull] private readonly Dictionary<ThingDef, Stock> stockCache = new Dictionary<ThingDef, Stock>();
        [NotNull] public IReadOnlyDictionary<ThingDef, Stock> AllStock => stockCache;

        public RestaurantStock([NotNull] RestaurantController restaurant)
        {
            Restaurant = restaurant;
        }

        public void ExposeData() { }

        public bool HasAnyFoodFor([NotNull] Pawn pawn, bool allowDrug)
        {
            //Log.Message($"{pawn.NameShortColored}: HasFoodFor: Defs: {stockCache.Select(item=>item.Value).Count(stock => WillConsume(pawn, allowDrug, stock.def))}");
            return stockCache.Keys.Any(def => WillConsume(pawn, allowDrug, def));
        }

        public class FoodOptimality
        {
            public Thing Thing { get; }
            public float Optimality { get; }
            public RestaurantController Restaurant { get; }

            public FoodOptimality(RestaurantController restaurant, Thing thing, float optimality)
            {
                Thing = thing;
                Optimality = optimality;
                Restaurant = restaurant;
            }
        }

        public static Thing GetBestMealFor(IEnumerable<RestaurantController> restaurants, [NotNull] Pawn pawn, out RestaurantController restaurant, bool allowDrug, bool includeEat = true, bool includeJoy = true)
        {
            restaurant = null;
            if (restaurants == null) return null;
            var options = restaurants.SelectMany(controller => controller.Stock.GetMealOptions(pawn, allowDrug, includeEat, includeJoy));

            //Log.Message($"{pawn.NameShortColored}: Meal options: {options.GroupBy(o => o.Thing.def).Select(o => $"{o.Key.label} ({o.FirstOrDefault()?.Optimality:F2})").ToCommaList()}");
            if (options.TryMaxBy(def => def.Optimality, out var best))
            {
                restaurant = best.Restaurant;
                //Log.Message($"{pawn.NameShortColored}: GetBestMealFor: {best.Thing.LabelCap} with optimality {best.Optimality:F2} at {restaurant?.Name}.");
                return best.Thing;
            }

            return null;
        }

        public static Thing GetRandomMealFor(IEnumerable<RestaurantController> restaurants, [NotNull] Pawn pawn, out RestaurantController restaurant, bool allowDrug, bool includeEat = true, bool includeJoy = true)
        {
            restaurant = null;
            if (restaurants == null) return null;
            var options = restaurants.SelectMany(controller => controller.Stock.GetMealOptions(pawn, allowDrug, includeEat, includeJoy));

            if (options.TryRandomElementByWeight(def => def.Optimality, out var random))
            {
                restaurant = random.Restaurant;
                //Log.Message($"{pawn.NameShortColored} picked {random.Thing.Label} with a score of {random.Optimality} at {restaurant?.Name}.\nOptions were:\n{options.Select(o=>$"- {o.Thing.LabelCap} ({o.Optimality:F0}) at {o.Restaurant.Name}").ToLineList()}");
                return random.Thing;
            }

            return null;
        }

        private IEnumerable<FoodOptimality> GetMealOptions([NotNull] Pawn pawn, bool allowDrug, bool includeEat, bool includeJoy)
        {
            return stockCache.Values
                .Where(stock => WillConsume(pawn, allowDrug, stock.def))
                .Where(stock => CanAfford(pawn, stock.def))
                .Select(stock => stock.items.FirstOrDefault()) // we only check the first one (so it could be that someone gets ingredients they didn't like...)
                .Where(consumable => Restaurant.Orders.CanBeOrdered(consumable))
                .Select(consumable => new FoodOptimality(Restaurant, consumable, GetMealOptimalityScore(pawn, consumable, includeEat, includeJoy)))
                .Where(def => def.Optimality >= MinOptimality);
        }

        private bool CanAfford(Pawn pawn, ThingDef def)
        {
            if (Restaurant.guestPricePercentage <= 0) return true;
            if (!pawn.CanHaveDebt()) return true;
            return pawn.GetSilver() >= def.GetPrice(Restaurant);
        }

        private float GetMealOptimalityScore([NotNull] Pawn pawn, Thing thing, bool includeEat = true, bool includeJoy = true)
        {
            if (thing == null) return 0;
            if (!IsAllowedIfDrug(pawn, thing.def!))
            {
                //Log.Message($"{pawn.NameShortColored}: {thing.LabelCap} Not allowed (drug)");
                return 0;
            }
            //var debugMessage = new StringBuilder($"{pawn.NameShortColored}: {thing.LabelCap} ");

            float score = 0;
            if (includeEat && pawn.needs.food != null)
            {
                var optimality = GetCachedOptimality(pawn, thing, eatOptimalityCache, CalcEatOptimality);
                var factor = NutritionVsNeedFactor(pawn, thing.def);
                score += optimality * factor;
                //debugMessage.Append($"EAT = {optimality:F0} * {factor:F2} ");
            }

            if (includeJoy && pawn.needs.joy != null)
            {
                var optimality = GetCachedOptimality(pawn, thing, joyOptimalityCache, CalcJoyOptimality);
                var factor = JoyVsNeedFactor(pawn, thing.def);
                score += optimality * factor;
                //debugMessage.Append($"JOY = {optimality:F0} * {factor:F2} ");
            }

            //debugMessage.Append($"= {score:F0}");
            //Log.Message(debugMessage.ToString());
            return score;
        }

        private static float CalcEatOptimality([NotNull] Pawn pawn, [NotNull] Thing thing)
        {
            return Mathf.Max(0, FoodUtility.FoodOptimality(pawn, thing, thing.def, 25));
        }

        private static float CalcJoyOptimality([NotNull] Pawn pawn, [NotNull] Thing thing)
        {
            var def = thing.def;
            var toleranceFactor = pawn.needs.joy.tolerances.JoyFactorFromTolerance(def.ingestible.JoyKind);
            var drugCategoryFactor = GetDrugCategoryFactor(def);
            return toleranceFactor * drugCategoryFactor * JoyOptimalityWeight;
        }

        private static float GetDrugCategoryFactor(ThingDef def)
        {
            return def.ingestible.drugCategory switch
            {
                DrugCategory.None => 3.5f,
                DrugCategory.Social => 3.0f,
                DrugCategory.Medical => 1.5f,
                _ => 1.0f
            };
        }

        private static bool IsAllowedIfDrug([NotNull] Pawn pawn, [NotNull] ThingDef def)
        {
            if (!def.IsDrug) return true;
            if (pawn.drugs == null) return true;
            if (pawn.InMentalState) return true;
            if (pawn.IsTeetotaler()) return false;
            if (pawn.story?.traits.DegreeOfTrait(TraitDefOf.DrugDesire) > 0) return true; // Doesn't care about schedule no matter the schedule
            var drugPolicyEntry = pawn.GetPolicyFor(def);
            //Log.Message($"{pawn.NameShortColored} vs {def.label} as drug: for joy = {drugPolicyEntry?.allowedForJoy}");
            if (drugPolicyEntry?.allowedForJoy == false) return false;
            return true;
        }

        private static float GetCachedOptimality(Pawn pawn, [NotNull] Thing thing, [NotNull] List<ConsumeOptimality> optimalityCache, [NotNull] Func<Pawn, Thing, float> calcFunction)
        {
            // Expensive, must be cached
            var optimality = optimalityCache.FirstOrDefault(o => o.pawn == pawn && o.thing == thing);
            if (optimality == null)
            {
                // Optimality can be negative
                optimality = new ConsumeOptimality { pawn = pawn, thing = thing, value = calcFunction(pawn, thing) };
                optimalityCache.Add(optimality);
            }

            // From 0 to 300-400ish
            return optimality.value;
        }

        private static float NutritionVsNeedFactor(Pawn pawn, ThingDef def)
        {
            var need = pawn.needs.food?.NutritionWanted ?? 0;
            if (need < 0.1f) return 0;
            var provided = def.ingestible.CachedNutrition;
            if (provided < 0.01f) return 0;
            var similarity = 1 - Mathf.Abs(need - provided) / need;
            var score = Mathf.Max(0, need * similarity);
            //Log.Message($"{pawn.NameShortColored}: {def.LabelCap} EAT Need = {need:F2} Provided = {provided:F2} Similarity = {similarity:F2} Score = {score:F2}");
            return score;
        }

        private static float JoyVsNeedFactor(Pawn pawn, ThingDef def)
        {
            var need = 1 - pawn.needs.joy?.CurLevelPercentage ?? 0;
            if (def.ingestible.joyKind == null) return 0;
            var score = def.ingestible.joy * need;
            //Log.Message($"{pawn.NameShortColored}: {def.LabelCap} JOY Need = {need:F2} Provided = {def.ingestible.joy:F2} Score = {score:F2}");
            return score;
        }

        private static bool WillConsume(Pawn pawn, bool allowDrug, ThingDef def)
        {
            if (def == null) return false;
            var fineAsDrug = allowDrug || !def.IsDrug;
            var fineAsFood = def.ingestible?.preferability == FoodPreferability.Undefined || def.ingestible?.preferability == FoodPreferability.NeverForNutrition || pawn.WillEat(def);
            var result = fineAsDrug && fineAsFood;
            //Log.Message($"{pawn.NameShortColored} will consume {def.label}? will eat = {pawn.WillEat_NewTemp(def)}, preferability = {def.ingestible?.preferability}, allowDrug = {allowDrug}, result = {result}");
            return result;
        }

        public Thing GetServableThing(Order order, Pawn pawn)
        {
            if (stockCache.TryGetValue(order.consumableDef, out var stock))
            {
                return stock.items.OrderBy(o => pawn.Position.DistanceToSquared(o.Position))
                    .FirstOrDefault(o => pawn.CanReserveAndReach(o, PathEndMode.Touch, JobUtility.MaxDangerServing, o.stackCount, 1));
            }
            return null;
        }

        public void RareTick()
        {
            RefreshStock();
        }

        public void RefreshStock()
        {
            // Refresh entire stock
            foreach (var stock in stockCache)
            {
                stock.Value.items.Clear();
                stock.Value.ordered = 0;
            }

            FindStockItems();

            // Slowly empty optimality caches again
            if (eatOptimalityCache.Count > 0) eatOptimalityCache.RemoveAt(0);
            if (joyOptimalityCache.Count > 0) joyOptimalityCache.RemoveAt(0);
        }

        private void FindStockItems()
        {
            foreach (var thing in GetConsumablesInRange()
                .Where(t => t.def is { IsIngestible: true, IsCorpse: false } && Menu.IsOnMenu(t) && !t.IsForbidden(Faction.OfPlayer)))
            {
                if (!stockCache.TryGetValue(thing.def, out var stock))
                {
                    stock = new Stock { def = thing.def };
                    stockCache.Add(thing.def, stock);
                }

                stock.items.Add(thing);
            }
        }

        private IEnumerable<Thing> GetConsumablesInRange()
        {
            var yieldedThings = new HashSet<Thing>();
            var fields = new List<IntVec3>();
            foreach (var buildingCashRegister in Restaurant.Registers) fields.AddRange(buildingCashRegister.Fields);
            foreach (var cell in fields)
            {
                var thingList = cell.GetThingList(Map);
                foreach (var t in thingList)
                {
                    if (t.def.ingestible == null || t.def.category != ThingCategory.Item) continue;

                    yieldedThings.Add(t);
                }
            }

            return yieldedThings;
        }

        [NotNull]
        public IReadOnlyCollection<Thing> GetAllStockOfDef(ThingDef def)
        {
            if (!stockCache.TryGetValue(def, out var stock)) return Array.Empty<Thing>();
            return stock.items;
        }

        public bool IsAvailable([NotNull] Thing consumable)
        {
            return stockCache.TryGetValue(consumable.def)?.items.Contains(consumable) == true;
        }
    }
}