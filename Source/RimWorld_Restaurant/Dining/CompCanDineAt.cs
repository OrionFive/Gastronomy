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

        public CashRegister Register { get; }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref allowDining, "switchOn");
            Scribe_Collections.Look(ref diningSpots, "diningSpots", LookMode.Reference);
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
                    disabled = Register == null,
                    disabledReason = "DiningHasNoRegister".Translate(),
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
                foreach (var pos in parent.OccupiedRect())
                {
                    var map = parent.Map;
                    if (PlaceWorker_OnTable.NotOccupied(pos, map))
                    {
                        var diningSpot = (DiningSpot) GenSpawn.Spawn(DefDatabase<ThingDef>.GetNamed("DiningSpot"), pos, map);
                        diningSpots.Add(diningSpot);
                    }
                }
            }
            else
            {
                foreach (var diningSpot in diningSpots)
                {
                    if (diningSpot.Destroyed) continue;
                    diningSpot?.Destroy();
                }

                diningSpots.Clear();
            }
        }

        public override void PostDeSpawn(Map map)
        {
            if (allowDining) ToggleDining();
        }
    }
}
