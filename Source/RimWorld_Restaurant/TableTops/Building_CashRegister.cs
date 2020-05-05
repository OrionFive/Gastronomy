using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Restaurant.Dining;
using RimWorld;
using Verse;

namespace Restaurant.TableTops
{
    public class Building_CashRegister : Building_TableTop
    {
        [NotNull] private readonly List<DiningSpot> diningSpots = new List<DiningSpot>();
        public float radius;
        public RestaurantSettings settings;

        [NotNull] private List<Thing> stock = new List<Thing>();
        public bool IsOpenedRightNow { get; } = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref radius, "radius", 20);
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            settings = Map.GetSettings();
            ScanDiningSpots();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            // Created fresh
            if (!respawningAfterLoad) settings = Map.GetSettings();
        }

        public bool HasAnyFoodFor([NotNull] Pawn pawn)
        {
            Log.Message($"Stock: {stock.Count}");
            return stock.Any();
        }

        public Thing GetBestFoodFor([NotNull] Pawn pawn)
        {
            Log.Message($"Stock: {stock.Count}");
            return stock.FirstOrDefault();
        }

        public override void TickRare()
        {
            base.TickRare();
            stock = new List<Thing>(Map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSource));
            Log.Message($"Stock: {stock.Select(s => s.def.label).ToCommaList(true)}");
        }

        public override void DrawExtraSelectionOverlays()
        {
            base.DrawExtraSelectionOverlays();
            DrawGizmos();
        }

        public void DrawGizmos()
        {
            foreach (var diningSpot in diningSpots.Where(diningSpot => diningSpot != null))
            {
                GenDraw.DrawLineBetween(this.TrueCenter(), diningSpot.TrueCenter());
            }
        }

        public void ScanDiningSpots()
        {
            diningSpots.Clear();
            diningSpots.AddRange(DiningUtility.GetAllDiningSpots(Map));
        }
    }
}
