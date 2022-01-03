using RimWorld;
using Verse;
// ReSharper disable InconsistentNaming
#pragma warning disable 649

namespace Gastronomy.Waiting
{
    [DefOf]
    internal static class WaitingDefOf
    {
        public static readonly JobDef Gastronomy_TakeOrder;
        public static readonly JobDef Gastronomy_Serve;
        public static readonly JobDef Gastronomy_MakeTable;
        public static readonly JobDef Gastronomy_StandBy;

        public static readonly WorkTypeDef Gastronomy_Waiting;

        [MayRequire("CashRegister")]
        public static readonly SoundDef CashRegister_Register_Kaching;
    }
}
