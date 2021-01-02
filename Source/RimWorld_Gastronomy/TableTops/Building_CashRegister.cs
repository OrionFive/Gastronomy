using System.Collections.Generic;
using System.Linq;
using Gastronomy.Dining;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace Gastronomy.TableTops
{
    public class Building_CashRegister : Building_TableTop, IHaulDestination, IThingHolder
    {
        private StorageSettings storageSettings;
        protected ThingOwner innerContainer;

        [NotNull] private readonly List<DiningSpot> diningSpots = new List<DiningSpot>();
        public float radius;
        public RestaurantController restaurant;

        public bool IsOpenedRightNow { get; } = true;

        public Building_CashRegister()
        {
            innerContainer = new ThingOwner<Thing>(this, false);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref radius, "radius", 20);
            Scribe_Deep.Look(ref storageSettings, "storageSettings", this);
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            restaurant = this.GetRestaurant();
            ScanDiningSpots();
        }

        public override void PostMake()
        {
            base.PostMake();
            storageSettings = GetNewStorageSettings();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad) restaurant = this.GetRestaurant();
        }

        public void DrawGizmos()
        {
            foreach (var diningSpot in diningSpots.Where(diningSpot => diningSpot != null))
            {
                GenDraw.DrawLineBetween(this.TrueCenter(), diningSpot.TrueCenter());
            }
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            innerContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
            innerContainer.ClearAndDestroyContents();
            base.Destroy(mode);
        }

        public override string GetInspectString() => innerContainer?.ContentsString.CapitalizeFirst();

        public void ScanDiningSpots()
        {
            diningSpots.Clear();
            diningSpots.AddRange(DiningUtility.GetAllDiningSpots(Map));
        }

        public StorageSettings GetStoreSettings() => storageSettings ?? (storageSettings = GetNewStorageSettings());

        private StorageSettings GetNewStorageSettings()
        {
            var s = new StorageSettings(this);
            if (def.building.defaultStorageSettings != null)
            {
                s.CopyFrom(def.building.defaultStorageSettings);
            }

            return s;
        }

        public StorageSettings GetParentStoreSettings() => def.building.fixedStorageSettings;

        public bool StorageTabVisible => false;
        public bool ShouldEmpty => GetDirectlyHeldThings()?.Any(t => t.def == ThingDefOf.Silver) == true;
        public bool Accepts(Thing t) => t.def == ThingDefOf.Silver;

        public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());

        public ThingOwner GetDirectlyHeldThings() => innerContainer ?? (innerContainer = new ThingOwner<Thing>(this, false));
    }
}
