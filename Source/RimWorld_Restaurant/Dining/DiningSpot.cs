using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Restaurant.TableTops;
using RimWorld;
using Verse;

namespace Restaurant.Dining
{
    public enum SpotState
    {
        Blocked = -1,
        Clear = 0,
        Ready = 1,
        Messy1 = 2,
        Messy2 = 3
    }

    public class DiningSpot : Building_NutrientPasteDispenser
    {
        public const string jobReportString = "DiningJobReportString";

        private RestaurantController restaurant;
        private List<SpotState> spotStates = new List<SpotState>(4) {SpotState.Clear, SpotState.Clear, SpotState.Clear, SpotState.Clear};

        public override ThingDef DispensableDef => throw new NotImplementedException();
        public bool MayDineStanding { get; } = false;

        public static SpotState GetMessyState => (SpotState) Rand.Range(2, 4);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref spotStates, "spotStates");
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            restaurant = this.GetRestaurant();
            UpdateMesh();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            _ThingWithComps_Base.SpawnSetup.Base(this, map, respawningAfterLoad);

            if (!respawningAfterLoad)
            {
                RegisterUtility.OnDiningSpotCreated(this);
                restaurant = this.GetRestaurant();
            }
        }

        public int GetMaxReservations() => GetReservationSpots().Count(s => s >= SpotState.Clear);
        public int GetMaxSeats() => GetReservationSpots().Count(s => s != SpotState.Blocked);

        public bool IsOpenedRightNow => restaurant.IsOpenedRightNow;

        /// <summary>
        /// [0] = up, [1] = right, [2] = down, [3] = left
        /// </summary>
        [NotNull]
        public SpotState[] GetReservationSpots()
        {
            if (spotStates == null) spotStates = new List<SpotState>(4) {SpotState.Clear, SpotState.Clear, SpotState.Clear, SpotState.Clear};
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
                    if (chair != null && chair.def.building.isSittable && intVec + chair.Rotation.FacingCell == position)
                    {
                        result[i] = spotStates[i];
                    }
                }

                //Log.Message($"Checked {intVec}: {result[i]} chair? {intVec.GetEdifice(map)?.Label} sittable? {intVec.GetEdifice(map)?.def.building.isSittable} facing? {intVec + intVec.GetEdifice(map)?.Rotation.FacingCell}");
            }

            return result;
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            RegisterUtility.OnDiningSpotRemoved(this);
            _ThingWithComps_Base.DeSpawn.Base(this, mode);
        }

        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            _ThingWithComps_Base.Destroy.Base(this, mode);
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
        public void SetSpotMessy(IntVec3 chairPos) => SetSpotState(chairPos, GetMessyState);
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

            Log.Error($"Tried to set dining spot {position} with an invalid spot position {chairPos}.");
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

        public bool IsValidSpot(IntVec3 chairPos)
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
    }
}
