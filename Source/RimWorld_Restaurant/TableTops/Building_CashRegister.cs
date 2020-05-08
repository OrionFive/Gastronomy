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
            if (!respawningAfterLoad) settings = Map.GetSettings();
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
