using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Restaurant.Dining
{
    public class CompCanDineAt : ThingComp
    {
        private bool allowDining;
        public CompProperties_CanDineAt Props => props as CompProperties_CanDineAt;

        public bool CanDineAt => allowDining;
        
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref allowDining, "switchOn", false);
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
                    isActive = (() => allowDining),
                    toggleAction = delegate { allowDining = !allowDining; }
                };
                yield return command_Toggle;
            }
        }
    }
}
