using HarmonyLib;
using Verse;
using Verse.AI;

namespace Restaurant.Patching
{
    /// <summary>
    /// Useless. Left as template
    /// </summary>
    //public class Pawn_JobTracker_Patch
    //{
    //    [HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.EndCurrentJob))]
    //    public class SpawnSetup
    //    {
    //        public static void Prefix(Pawn ___pawn, bool ___debugLog)
    //        {
    //            var driver = ___pawn.jobs.curDriver;
    //            if (___debugLog && driver != null)
    //            {
    //                var toil = Traverse.Create(driver).Property<Toil>("CurToil").Value;
    //                if (toil != null)
    //                {
    //                    Log.Message($"{___pawn.NameShortColored} ended with toil: initAction={toil.initAction?.Method.DeclaringType.Name} tickAction={toil.tickAction?.Method.Name}");
    //                }
    //            }
    //        }
    //    }
    //}
}
