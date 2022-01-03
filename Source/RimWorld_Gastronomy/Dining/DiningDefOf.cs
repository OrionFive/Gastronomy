using RimWorld;
using Verse;
// ReSharper disable InconsistentNaming
#pragma warning disable 649

namespace Gastronomy.Dining
{
    [DefOf]
    internal static class DiningDefOf
    {
        public static readonly JobDef Gastronomy_Dine;
        public static readonly ThoughtDef Gastronomy_BoughtFood;
        public static readonly ThoughtDef Gastronomy_Serviced;
        public static readonly ThoughtDef Gastronomy_ServicedMood;
        public static readonly ThoughtDef Gastronomy_HadToWait;
        public static readonly ThingDef Gastronomy_DiningSpot;
    }
}