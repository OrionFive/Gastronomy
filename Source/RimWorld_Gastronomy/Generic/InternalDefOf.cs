using RimWorld;
using Verse;
// ReSharper disable InconsistentNaming
#pragma warning disable 649

namespace Gastronomy
{
    [DefOf]
    internal static class InternalDefOf
    {
        public static readonly ThingDef Gastronomy_DiningSpot;
        public static readonly JobDef Gastronomy_Dine;
        public static readonly ThoughtDef Gastronomy_BoughtFood;
        public static readonly ThoughtDef Gastronomy_Serviced;
        public static readonly ThoughtDef Gastronomy_ServicedMood;
        public static readonly ThoughtDef Gastronomy_HadToWait;

        [MayRequire("CashRegister")]
        public static readonly ThingDef CashRegister_CashRegister;
        [MayRequire("CashRegister")]
        public static readonly SoundDef CashRegister_Register_Kaching;
    }
}