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
        [NotNull] private List<DiningSpot> diningSpots = new List<DiningSpot>();
        public float radius;
        public RestaurantSettings settings;

        [NotNull] private List<Thing> stock = new List<Thing>();
        public bool IsOpenedRightNow { get; } = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref radius, "radius", 20);
            Scribe_Deep.Look(ref settings, "settings");
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            TakeOrMakeSettings();
            ScanDiningSpots();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            // Created fresh
            if (!respawningAfterLoad) TakeOrMakeSettings();
        }

        private void TakeOrMakeSettings()
        {
            settings = RegisterUtility.GetSettings(Map);
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
            if (radius < GenRadial.MaxRadialPatternRadius)
            {
                GenDraw.DrawRadiusRing(Position, radius);
            }

            foreach (var diningSpot in diningSpots.Where(diningSpot => diningSpot != null))
            {
                GenDraw.DrawLineBetween(this.TrueCenter(), diningSpot.TrueCenter());
            }
        }

        public void ScanDiningSpots()
        {
            foreach (var diningSpot in diningSpots)
            {
                // diningSpot.
            }

            diningSpots.Clear();
            foreach (var pos in GenRadial.RadialCellsAround(Position, radius, true))
            {
                var diningSpot = pos.GetFirstThing<DiningSpot>(Map);
                if (diningSpot != null) diningSpots.Add(diningSpot);
            }
        }
    }
}
