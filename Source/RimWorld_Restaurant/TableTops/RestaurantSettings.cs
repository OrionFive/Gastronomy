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
            Log.Message($"Stock: {stock.Count}");
            return stock.Any(s => WillConsume(pawn, allowDrug, s));
        }

        public Thing GetBestFoodFor([NotNull] Pawn pawn, bool allowDrug)
        {
            Log.Message($"Stock: {stock.Count}");
            return stock.FirstOrDefault(s => WillConsume(pawn, allowDrug, s));
        }

        private static bool WillConsume(Pawn pawn, bool allowDrug, Thing s)
        {
            return (allowDrug || !s.def.IsDrug) && pawn.WillEat(s);
        }

        public override void MapComponentTick()
        {
            if (GenTicks.TicksGame < lastStockUpdateTick + 250) return;
            lastStockUpdateTick = GenTicks.TicksGame;

            stock = new List<Thing>(map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSource).Where(t=>t.def.IsIngestible && t.def.IsNutritionGivingIngestible));
            Log.Message($"Stock: {stock.Select(s => s.def.label).ToCommaList(true)}");
        }
    }
}
