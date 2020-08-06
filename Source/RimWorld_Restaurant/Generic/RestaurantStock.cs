using System;
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
        private const float MinOptimality = 100f;

        private class ConsumeOptimality
        {
            public Pawn pawn;
            public ThingDef def;
            public float value;
        }

        [NotNull] private readonly List<Thing> stockCache = new List<Thing>();
        [NotNull] private readonly List<ConsumeOptimality> eatOptimalityCache = new List<ConsumeOptimality>();
        [NotNull] private readonly List<ConsumeOptimality> joyOptimalityCache = new List<ConsumeOptimality>();
        [NotNull] public IReadOnlyCollection<Thing> AllStock => stockCache.AsReadOnly();
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
            return stockCache.Select(item => item.def)
                .Any(s => WillConsume(pawn, allowDrug, s));
        }

        public ThingDef GetBestFoodTypeFor([NotNull] Pawn pawn, bool allowDrug)
        {
            if (stockCache.Select(item => item.def).Distinct()
                .Where(def => WillConsume(pawn, allowDrug, def))
                .Select(def => new {def, optimality = GetFoodOptimalityScore(pawn, def)})
                .Where(def => def.optimality >= MinOptimality)
                .TryMaxBy(def => def.optimality, out var best))
            {
                //Log.Message($"{pawn.NameShortColored}: GetBestFoodFor: {best?.label}");
                return best.def;
            }
            return null;
        }

        public ThingDef GetRandomFoodTypeFor([NotNull] Pawn pawn, bool allowDrug)
        {
            if (stockCache.Select(item => item.def).Distinct()
                .Where(def => WillConsume(pawn, allowDrug, def))
                .Select(def => new {def, optimality = GetFoodOptimalityScore(pawn, def)})
                .Where(def => def.optimality >= MinOptimality)
                .TryRandomElementByWeight(def => def.optimality, out var random))
            {
                Log.Message($"{pawn.NameShortColored} picked {random.def.label} with a score of {random.optimality}");
                return random.def;
            }
            return null;
        }

        private float GetFoodOptimalityScore(Pawn pawn, ThingDef def)
        {
            var optEat = GetCachedOptimality(pawn, def, eatOptimalityCache, CalcEatOptimality);
            var optJoy = GetCachedOptimality(pawn, def, joyOptimalityCache, CalcJoyOptimality);
            var eatNeedFactor = NutritionVsNeedFactor(pawn, def);
            var joyNeedFactor = JoyVsNeedFactor(pawn, def);
            var score = optEat * eatNeedFactor + optJoy * joyNeedFactor;
            Log.Message($"{pawn.NameShortColored}: {def.LabelCap} {optEat:F2} * {eatNeedFactor:F2} + {optJoy:F2} * {joyNeedFactor:F2} = {score:F2}");
            return score;
        }

        private static float CalcEatOptimality(Pawn pawn, ThingDef def)
        {
            if (!IsAllowedIfDrug(pawn, def)) return 0;
            return Mathf.Max(0, FoodUtility.FoodOptimality(pawn, null, def, 0));
        }

        private static float CalcJoyOptimality(Pawn pawn, ThingDef def)
        {
            if (pawn.needs.joy == null) return 0;
            if (!IsAllowedIfDrug(pawn, def)) return 0;
            var toleranceFactor = pawn.needs.joy.tolerances.JoyFactorFromTolerance(def.ingestible.JoyKind);
            var drugCategoryFactor = GetDrugCategoryFactor(def);
            return toleranceFactor * drugCategoryFactor * 100;
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

        private static bool IsAllowedIfDrug(Pawn pawn, ThingDef def)
        {
            if (!def.IsDrug) return true;
            if (pawn.drugs == null) return true;
            if (pawn.InMentalState) return true;
            if (pawn.IsTeetotaler()) return false;
            if (pawn.story?.traits.DegreeOfTrait(TraitDefOf.DrugDesire) > 0) return true; // Doesn't care about schedule no matter the schedule
            if (!pawn.drugs.CurrentPolicy[def].allowedForJoy) return false;
            return true;
        }

        private static float GetCachedOptimality(Pawn pawn, ThingDef def, [NotNull] List<ConsumeOptimality> optimalityCache, [NotNull] Func<Pawn, ThingDef, float> calcFunction)
        {
            // Expensive, must be cached
            var optimality = optimalityCache.FirstOrDefault(o => o.pawn == pawn && o.def == def);
            if (optimality == null)
            {
                // Optimality can be negative
                optimality = new ConsumeOptimality {pawn = pawn, def = def, value = calcFunction(pawn, def)};
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

        private static bool WillConsume(Pawn pawn, bool allowDrug, ThingDef s)
        {
            return s != null && (allowDrug || !s.IsDrug) && pawn.WillEat(s);
        }

        public Thing GetServableThing(Order order, Pawn pawn)
        {
            return stockCache.Where(o => o.Spawned && o.def == order.consumableDef).OrderBy(o => pawn.Position.DistanceToSquared(o.Position)).FirstOrDefault(o => pawn.CanReserveAndReach(o, PathEndMode.Touch, Danger.None, o.stackCount, 1));
        }

        public void RareTick()
        {
            // Refresh entire stock
            stockCache.Clear();
            stockCache.AddRange(Map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSource).Where(t => t.def.IsIngestible && !t.def.IsCorpse && Menu.IsOnMenu(t)));

            // Slowly empty caches again
            if (eatOptimalityCache.Count > 0) eatOptimalityCache.RemoveAt(0);
            if (joyOptimalityCache.Count > 0) joyOptimalityCache.RemoveAt(0);
        }
    }
}
