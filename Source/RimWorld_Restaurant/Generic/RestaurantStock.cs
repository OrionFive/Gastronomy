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
        private class FoodOptimality
        {
            public Pawn pawn;
            public ThingDef def;
            public float value;
        }

        [NotNull] private readonly List<Thing> stockCache = new List<Thing>();
        [NotNull] private readonly List<FoodOptimality> optimalityCache = new List<FoodOptimality>();
        [NotNull] public IEnumerable<Thing> AllStock => stockCache.AsReadOnly();
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
            var pawnJoyNeed = PawnJoyNeed(pawn);
            var best = stockCache.Select(item => item.def).Distinct()
                .Where(def => WillConsume(pawn, allowDrug, def))
                .MaxBy(def => GetFoodOptimality(pawn, def));
            //Log.Message($"{pawn.NameShortColored}: GetBestFoodFor: {best?.label}");
            return best;
        }

        public ThingDef GetRandomFoodTypeFor([NotNull] Pawn pawn, bool allowDrug)
        {
            var pawnJoyNeed = PawnJoyNeed(pawn);
            var random = stockCache.Select(item => item.def).Distinct()
                .Where(def => WillConsume(pawn, allowDrug, def))
                .Where(def => IsAllowedIfDrug(def, pawn))
                .RandomElementByWeight(def => GetFoodOptimality(pawn, def) * GetJoyOptimality(def, pawnJoyNeed));
            Log.Message($"{pawn.NameShortColored}: {random.LabelCap} Joy need = {pawnJoyNeed} JoyOptimality: {GetJoyOptimality(random, pawnJoyNeed)}");
            return random;
        }

        private static bool IsAllowedIfDrug(ThingDef def, Pawn pawn)
        {
            if (!def.IsDrug) return true;
            if (pawn.drugs == null) return true;
            if (!pawn.drugs.CurrentPolicy[def].allowedForJoy) return false;
            return pawn.story?.traits.DegreeOfTrait(TraitDefOf.DrugDesire) > 0;
        }

        private static float PawnJoyNeed(Pawn pawn)
        {
            if (pawn.needs.joy == null) return 0f;
            if (pawn.needs.food == null) return 0.5f;
            var joy = pawn.needs.joy.CurLevelPercentage;
            var food = pawn.needs.food.CurLevelPercentage;
            // 1 if we need joy much more, 0 if we need food much more, 0.5 if even
            return (1 - joy + food) * 0.5f;
        }

        private float GetFoodOptimality(Pawn pawn, ThingDef def)
        {
            var optimality = optimalityCache.FirstOrDefault(o => o.pawn == pawn && o.def == def);
            if (optimality == null)
            {
                // Optimality can be negative
                var value = Mathf.Max(0, FoodUtility.FoodOptimality(pawn, null, def, 0));
                optimality = new FoodOptimality {pawn = pawn, def = def, value = value};
                optimalityCache.Add(optimality);
            }
            // From 0 to 300-400ish
            return optimality.value;
        }

        private static float GetJoyOptimality(ThingDef def, float pawnJoyNeed)
        {
            if (def.ingestible.joyKind == null) return 1 - pawnJoyNeed;
            // 0 if need and provided joy are very different, 1 if they are the same
            return 1 - Mathf.Abs(pawnJoyNeed - def.ingestible.joy);
        }

        private static bool WillConsume(Pawn pawn, bool allowDrug, ThingDef s)
        {
            return (allowDrug || !s.IsDrug) && pawn.WillEat(s);
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

            // Slowly empty cache again
            if (optimalityCache.Count > 0)
            {
                optimalityCache.RemoveAt(0);
            }
        }
    }
}
