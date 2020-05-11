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
    public class DiningSpot : Building_NutrientPasteDispenser
    {
        public const string jobReportString = "DiningJobReportString";

        private RestaurantSettings settings;

        public override ThingDef DispensableDef => throw new NotImplementedException();
        public bool MayDineStanding { get; } = false;

        public override void PostMapInit()
        {
            base.PostMapInit();
            settings = this.GetRestaurant();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            ThingWithComps.SpawnSetup.Base(this, map, respawningAfterLoad);

            if (!respawningAfterLoad)
            {
                RegisterUtility.OnDiningSpotCreated(this);
                settings = this.GetRestaurant();
            }
        }

        public int GetMaxReservations()
        {
            IntVec3 position = Position;
            Map map = Map;
            int num = 0;
            int result = 0;
            while (true)
            {
                if (num >= 4) break;
                var intVec = position + new Rot4(num).FacingCell;
                if (MayDineStanding && intVec.Standable(map))
                {
                    result++;
                }
                else
                {
                    var chair = intVec.GetEdifice(map);
                    if (chair != null && chair.def.building.isSittable && chair.Rotation.FacingCell == position)
                    {
                        result++;
                    }
                }

                num++;
            }

            return result;
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

        public override Thing TryDispenseFood()
        {
            if (!settings.IsOpenedRightNow) return null;

            // TODO: Implement this method correctly
            Log.Message("Trying to get food from DiningSpot!");
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
                num -= num2 * thing.GetStatValue(StatDefOf.Nutrition);
                list.Add(thing.def);
                thing.SplitOff(num2);
            } while (!(num <= 0f));

            def.building.soundDispense.PlayOneShot(new TargetInfo(Position, Map));
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
