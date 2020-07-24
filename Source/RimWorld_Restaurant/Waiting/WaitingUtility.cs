using Verse;

namespace Restaurant.Waiting
{
    public static class WaitingUtility
    {
        public static readonly JobDef takeOrderDef = DefDatabase<JobDef>.GetNamed("Restaurant_TakeOrder");
        public static readonly JobDef serveDef = DefDatabase<JobDef>.GetNamed("Restaurant_Serve");
        public static readonly JobDef makeTableDef = DefDatabase<JobDef>.GetNamed("Restaurant_MakeTable");

        public static IntVec3 GetChairPosition(Pawn patron)
        {
            if(patron.pather.MovingNow)
                return patron.pather.Destination.Cell;
            return patron.Position;
        }
    }
}
