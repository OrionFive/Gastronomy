using RimWorld;
using Verse;
// ReSharper disable InconsistentNaming
#pragma warning disable 649

namespace Gastronomy
{
    [DefOf]
    internal static class GenericDefOf
    {
        [MayRequire("CashRegister")]
        public static readonly ThingDef CashRegister_CashRegister;
    }
}