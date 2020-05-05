using System.Collections.Generic;
using Restaurant.TableTops;
using RimWorld;
using UnityEngine;
using Verse;

namespace Restaurant.Dining
{
    public class CompCanDineAt : ThingComp
    {
        private bool allowDining;
        private List<DiningSpot> diningSpots = new List<DiningSpot>();

        public CompProperties_CanDineAt Props => props as CompProperties_CanDineAt;

        public bool CanDineAt => allowDining;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref allowDining, "switchOn");
            Scribe_Collections.Look(ref diningSpots, "diningSpots", LookMode.Reference);
        }

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            DiningUtility.RegisterDiningSpotHolder(parent);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo item in base.CompGetGizmosExtra())
            {
                yield return item;
            }

            if (parent.Faction == Faction.OfPlayer)
            {
                var command_Toggle = new Command_Toggle
                {
                    hotKey = KeyBindingDefOf.Command_TogglePower,
                    icon = ContentFinder<Texture2D>.Get("UI/Commands/ToggleDining"),
                    defaultLabel = "CommandToggleDining".Translate(),
                    defaultDesc = "CommandToggleDiningDesc".Translate(),
                    isActive = () => allowDining,
                    toggleAction = ToggleDining
                };
                yield return command_Toggle;
            }
        }

        private void ToggleDining()
        {
            allowDining = !allowDining;
            if (allowDining)
            {
                TrySpawnDiningSpots();
            }
            else
            {
                TryRemoveDiningSpots();
            }
        }

        private void TryRemoveDiningSpots()
        {
            foreach (var diningSpot in diningSpots)
            {
                if (diningSpot.Destroyed) continue;
                diningSpot?.Destroy();
            }
            diningSpots.Clear();
        }

        private void TrySpawnDiningSpots()
        {
            foreach (var pos in parent.OccupiedRect())
            {
                // In case there already are dining spots
                if (diningSpots.Any(s => s.Position == pos)) continue;

                var map = parent.Map;
                if (PlaceWorker_OnTable.NotOccupied(pos, map))
                {
                    var diningSpot = (DiningSpot) GenSpawn.Spawn(DiningUtility.diningSpotDef, pos, map);
                    diningSpots.Add(diningSpot);
                }
            }
        }

        public override void PostDeSpawn(Map map)
        {
            if (allowDining) ToggleDining();
        }

        public void Notify_BuildingDespawned(Building building, Map map)
        {
            // Map is required separately, because building.Map is not valid for despawned objects
            // A building was despawned at my current position
            if (building != parent && allowDining)
            {
                TrySpawnDiningSpots();
            }
        }
    }
}
