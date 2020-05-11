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
        private List<Pawn> spawnedDiningPawnsResult = new List<Pawn>();

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
            //Log.Message($"{pawn.NameShortColored}: HasFoodFor: Defs: {stock.Select(item=>item.def).Count(s => WillConsume(pawn, allowDrug, s))}");
            return stock.Select(item=>item.def).Any(s => WillConsume(pawn, allowDrug, s));
        }

        public ThingDef GetBestFoodTypeFor([NotNull] Pawn pawn, bool allowDrug)
        {
            var best = stock.Select(item=>item.def).Distinct().Where(def => WillConsume(pawn, allowDrug, def)).MaxBy(def => FoodUtility.FoodOptimality(pawn, null, def, 0));
            //Log.Message($"{pawn.NameShortColored}: GetBestFoodFor: {best?.label}");
            return best;
        }
        
        private static bool WillConsume(Pawn pawn, bool allowDrug, ThingDef s)
        {
            return (allowDrug || !s.IsDrug) && pawn.WillEat(s);
        }

        public override void MapComponentTick()
        {
            if (GenTicks.TicksGame < lastStockUpdateTick + 500) return;
            lastStockUpdateTick = GenTicks.TicksGame;
            stock = new List<Thing>(map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSource).Where(t=>t.def.IsIngestible && IsInConsumableCategory(t.def.thingCategories)));
            //Log.Message($"Stock: {stock.Select(s => s.def.label).ToCommaList(true)}");
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

        public void RequestMealFor(Pawn patron, ThingDef foodDef)
        {
            Log.Message($"{patron.NameShortColored} has ordered {foodDef.label}.");
        }

        public List<Pawn> SpawnedDiningPawns
        {
            get
            {
                spawnedDiningPawnsResult.Clear();
                spawnedDiningPawnsResult.AddRange(map.mapPawns.AllPawnsSpawned.Where(pawn => pawn.jobs?.curDriver is JobDriver_Dine));
                return spawnedDiningPawnsResult;
            }
        }
    }
}
