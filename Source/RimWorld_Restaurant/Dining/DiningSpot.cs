using System;
using System.Collections.Generic;
using Restaurant.TableTops;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using ThingWithComps = Restaurant.Patching.ThingWithComps;

namespace Restaurant.Dining
{
    /// <summary>
    /// This is currently a NutrientPasteDispenser... but maybe that's not needed at all
    /// </summary>
    public class DiningSpot : Building_NutrientPasteDispenser
    {
        private RestaurantSettings settings;
        public const string jobReportString = "DiningJobReportString";

        public override void PostMapInit()
        {
            base.PostMapInit();
            settings = Map.GetSettings();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            ThingWithComps.SpawnSetup.Base(this, map, respawningAfterLoad);

            if (!respawningAfterLoad)
            {
                RegisterUtility.OnDiningSpotCreated(this);
               settings = Map.GetSettings();
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            RegisterUtility.OnDiningSpotRemoved(this);
            ThingWithComps.DeSpawn.Base(this, mode);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            ThingWithComps.Destroy.Base(this, mode);
        }

        public override ThingDef DispensableDef => throw new NotImplementedException();

        public override Thing TryDispenseFood()
        {
            if (!settings.IsOpenedRightNow) return null;

            // TODO: Implement this method correctly
            float num = def.building.nutritionCostPerDispense - 0.0001f;
            List<ThingDef> list = new List<ThingDef>();
            do
            {
                Thing thing = FindFeedInAnyHopper();
                if (thing == null)
                {
                    Log.Error("Did not find enough food in hoppers while trying to dispense.");
                    return null;
                }
                int num2 = Mathf.Min(thing.stackCount, Mathf.CeilToInt(num / thing.GetStatValue(StatDefOf.Nutrition)));
                num -= (float)num2 * thing.GetStatValue(StatDefOf.Nutrition);
                list.Add(thing.def);
                thing.SplitOff(num2);
            }
            while (!(num <= 0f));
            def.building.soundDispense.PlayOneShot(new TargetInfo(base.Position, base.Map));
            Thing thing2 = ThingMaker.MakeThing(ThingDefOf.MealNutrientPaste);
            CompIngredients compIngredients = thing2.TryGetComp<CompIngredients>();
            for (int i = 0; i < list.Count; i++)
            {
                compIngredients.RegisterIngredient(list[i]);
            }
            return thing2;
        }        

        #region NutrientPasteDispenser overrides

        public override bool HasEnoughFeedstockInHoppers() => true;

        public override Building AdjacentReachableHopper(Pawn pawn) => null;

        #endregion
    }
}
