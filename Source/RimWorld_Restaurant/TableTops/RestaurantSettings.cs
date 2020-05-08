using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Restaurant.Dining;
using RimWorld;
using Verse;

namespace Restaurant.TableTops
{
    public class RestaurantSettings : MapComponent
    {
        public IntVec3 testPos;

        public readonly List<DiningSpot> diningSpots = new List<DiningSpot>();
        public bool IsOpenedRightNow { get; } = true;
        [NotNull] private List<Thing> stock = new List<Thing>();
        private int lastStockUpdateTick;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref testPos, "testPos");
        }

        public RestaurantSettings(Map map) : base(map) { }

        public override void FinalizeInit()
        {
            base.FinalizeInit();

            diningSpots.Clear();
            diningSpots.AddRange(DiningUtility.GetAllDiningSpots(map));
            Log.Message($"Finalized with {diningSpots.Count} dining spots.");
        }

        public bool HasAnyFoodFor([NotNull] Pawn pawn, bool allowDrug)
        {
            Log.Message($"{pawn.NameShortColored}: HasFoodFor: Stock: {stock.Count(s => WillConsume(pawn, allowDrug, s))}");
            return stock.Any(s => WillConsume(pawn, allowDrug, s));
        }

        public ThingDef GetBestFoodTypeFor([NotNull] Pawn pawn, bool allowDrug)
        {
            var firstOrDefault = stock.FirstOrDefault(s => WillConsume(pawn, allowDrug, s));
            Log.Message($"{pawn.NameShortColored}: GetBestFoodFor: {firstOrDefault?.def.label}");
            return firstOrDefault?.def;
        }

        private static bool WillConsume(Pawn pawn, bool allowDrug, Thing s)
        {
            return (allowDrug || !s.def.IsDrug) && pawn.WillEat(s);
        }

        public override void MapComponentTick()
        {
            if (GenTicks.TicksGame < lastStockUpdateTick + 500) return;
            lastStockUpdateTick = GenTicks.TicksGame;
            stock = new List<Thing>(map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSource).Where(t=>t.def.IsIngestible && IsInConsumableCategory(t.def.thingCategories)));
            Log.Message($"Stock: {stock.Select(s => s.def.label).ToCommaList(true)}");
        }

        private static bool IsInConsumableCategory(List<ThingCategoryDef> defThingCategories)
        {
            if (defThingCategories == null) return false;
            if (defThingCategories.Contains(ThingCategoryDefOf.Drugs)) return true;
            if (defThingCategories.Contains(ThingCategoryDefOf.FoodMeals)) return true;
            if (defThingCategories.Contains(ThingCategoryDefOf.Foods)) return true;
            if (defThingCategories.Contains(ThingCategoryDefOf.PlantMatter)) return true;
            return false;
        }
    }
}
