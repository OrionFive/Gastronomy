using System;
using System.Collections.Generic;
using System.Linq;
using Gastronomy.Restaurant;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace Gastronomy.Dining
{
    public enum SpotState
    {
        Blocked = -1,
        Clear = 0, // 0-5
        Ready = 6,
        Messy1 = 7,
        Messy2 = 8
    }

    public class DiningSpot : Building_NutrientPasteDispenser
    {
        public const string jobReportString = "DiningJobReportString";

        private List<SpotState> spotStates = Enumerable.Repeat(SpotState.Clear, 4).ToList();
        private int decoVariation;

        public override ThingDef DispensableDef => throw new NotImplementedException();
        public bool MayDineStanding { get; } = false;
        public static SpotState RandomMessyState => (SpotState) Rand.RangeInclusive((int) SpotState.Messy1, (int) SpotState.Messy2);

        public int DecoVariation
        {
            get => decoVariation;
            set
            {
                decoVariation = value;
                UpdateMesh();
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref spotStates, "spotStates");
            Scribe_Values.Look(ref decoVariation, "decoVariation");
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            UpdateMesh();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);

            if (!respawningAfterLoad)
            {
                DiningUtility.OnDiningSpotCreated(this);
            }
        }

        public int GetMaxReservations() => GetReservationSpots().Count(s => s >= SpotState.Clear);
        public int GetMaxSeats() => GetReservationSpots().Count(s => s != SpotState.Blocked);

        public bool CanDineHere(Pawn pawn) => GetRestaurants().Any(restaurant => restaurant.IsOpenedRightNow && restaurant.MayDineHere(pawn));

        public IEnumerable<RestaurantController> GetRestaurants()
        {
            return this.GetRestaurantsManager().restaurants.Where(r => r.diningSpots.Contains(this));
        }

        /// <summary>
        /// [0] = up, [1] = right, [2] = down, [3] = left
        /// chairs gets filled with unblocked chairs.
        /// </summary>
        [NotNull]
        public SpotState[] GetReservationSpots(List<Building> chairs = null)
        {
            spotStates ??= new List<SpotState>(4) {SpotState.Clear, SpotState.Clear, SpotState.Clear, SpotState.Clear};
            chairs?.Clear();
            var position = Position;
            var map = Map;
            var result = new SpotState[4];
            for (int i = 0; i < 4; i++)
            {
                result[i] = SpotState.Blocked;
                var intVec = position + new Rot4(i).FacingCell;
                if (MayDineStanding && intVec.Standable(map))
                {
                    result[i] = spotStates[i];
                }
                else
                {
                    var chair = intVec.GetEdifice(map);
                    if (chair == null) continue;

                    var facingCorrectly = !chair.def.rotatable || intVec + chair.Rotation.FacingCell == position;

                    if (chair.def.building.isSittable && facingCorrectly)
                    {
                        result[i] = spotStates[i];
                        chairs?.Add(chair);
                    }
                }

                //Log.Message($"Checked {intVec}: {result[i]} chair? {intVec.GetEdifice(map)?.Label} sittable? {intVec.GetEdifice(map)?.def.building.isSittable} facing? {intVec + intVec.GetEdifice(map)?.Rotation.FacingCell}");
            }

            return result;
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            DiningUtility.OnDiningSpotRemoved(this);
            base.DeSpawn(mode);
        }

        [UsedImplicitly]
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            base.Destroy(mode);
        }

        private void UpdateMesh()
        {
            if (Spawned)
            {
                Map.mapDrawer.MapMeshDirty(Position, MapMeshFlag.Things, false, false);
            }
        }

        public void SetSpotReady(IntVec3 chairPos) => SetSpotState(chairPos, SpotState.Ready);
        public bool IsSpotReady(IntVec3 chairPos) => GetSpotState(chairPos) == SpotState.Ready;
        public void SetSpotMessy(IntVec3 chairPos) => SetSpotState(chairPos, RandomMessyState);
        public bool IsSpotMessy(IntVec3 chairPos) => GetSpotState(chairPos) > SpotState.Ready;

        private void SetSpotState(IntVec3 chairPos, SpotState state)
        {
            var position = Position;
            for (int i = 0; i < 4; i++)
            {
                var intVec = position + new Rot4(i).FacingCell;
                if (intVec == chairPos)
                {
                    //Log.Message($"Changed spot at {position} towards {chairPos} from state {spotStates[i]} to {state}.");
                    spotStates[i] = state;
                    UpdateMesh();
                    return;
                }
            }

            Log.Warning($"Tried to set dining spot {position} with an invalid chair position {chairPos}.");
        }

        private SpotState GetSpotState(IntVec3 chairPos)
        {
            if(!Spawned || Destroyed) return SpotState.Blocked;
            var position = Position;
            for (int i = 0; i < 4; i++)
            {
                var intVec = position + new Rot4(i).FacingCell;
                if (intVec == chairPos)
                {
                    //Log.Message($"Checked spot state of {position} from {chairPos}: {spotStates[i]} ({i})");
                    return spotStates[i];
                }
            }

            Log.Warning($"Tried to get state of dining spot {position} with an invalid spot position {chairPos}. This message can probably be ignored.");
            return SpotState.Blocked;
        }

        public override Thing TryDispenseFood() => null;

        #region NutrientPasteDispenser overrides

        public override bool HasEnoughFeedstockInHoppers() => true;

        public override Building AdjacentReachableHopper(Pawn pawn) => null;

        #endregion

        public IEnumerable<LocalTargetInfo> GetUnmadeSpotCells()
        {
            var spots = GetReservationSpots();
            for (int i = 0; i < 4; i++)
            {
                if (spots[i] == SpotState.Clear || spots[i] > SpotState.Ready)
                {
                    yield return Position + new Rot4(i).FacingCell;
                }
            }
        }

        public bool IsValidDineCell(IntVec3 chairPos)
        {
            if(!Spawned || Destroyed) return false;
            var position = Position;
            for (int i = 0; i < 4; i++)
            {
                var intVec = position + new Rot4(i).FacingCell;
                if (intVec == chairPos) return true;
            }

            return false;
        }

        public bool IsSociallyProper(Pawn pawn)
        {
            var table = Position.GetEdifice(Map);
            return table?.IsSociallyProper(pawn) == true;
        }
    }
}
