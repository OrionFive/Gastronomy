using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using Verse.AI;

namespace Restaurant.Dining
{
    public static class DiningUtility
    {
        public static readonly ThingDef diningSpotDef = ThingDef.Named("Restaurant_DiningSpot");
        public static readonly JobDef dineDef = DefDatabase<JobDef>.GetNamed("Restaurant_Dine");
        public static readonly HashSet<ThingDef> thingsWithCompCanDineAt = new HashSet<ThingDef>();

        public static IEnumerable<DiningSpot> GetAllDiningSpots([NotNull] Map map)
        {
            return map.listerBuildings.AllBuildingsColonistOfClass<DiningSpot>();
        }

        public static DiningSpot FindDiningSpotFor([NotNull] Pawn pawn, out ThingDef foodDef, bool allowDrug)
        {
            const int maxRegionsToScan = 100;
            foodDef = null;

            var settings = pawn.GetRestaurant();
            if (settings == null) return null;

            if (!settings.IsOpenedRightNow || !settings.HasAnyFoodFor(pawn, allowDrug)) return null;

            bool Validator(Thing thing)
            {
                var spot = (DiningSpot) thing;
                return !spot.IsForbidden(pawn) && spot.IsSociallyProper(pawn) && pawn.CanReserve(spot, spot.GetMaxReservations(), 0);
            }

            var diningSpot = (DiningSpot) GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(diningSpotDef), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, Validator, null, 0, maxRegionsToScan, false,
                RegionType.Set_Passable, true);
            if (diningSpot == null) return null;

            foodDef = settings.GetBestFoodTypeFor(pawn, allowDrug);
            return foodDef == null ? null : diningSpot;
        }

        public static void RegisterDiningSpotHolder(ThingWithComps thing)
        {
            thingsWithCompCanDineAt.Add(thing.def);
        }

        public static bool CanPossiblyDineAt(ThingDef def)
        {
            return thingsWithCompCanDineAt.Contains(def);
        }

        public static Toil GoToDineSpot(Pawn pawn, TargetIndex dineSpotInd)
        {
            var toil = new Toil();
            toil.initAction = () => {
                Pawn actor = toil.actor;
                IntVec3 targetPosition = IntVec3.Invalid;
                var diningSpot = (DiningSpot) actor.CurJob.GetTarget(dineSpotInd).Thing;

                bool BaseChairValidator(Thing t)
                {
                    if (t.def.building == null || !t.def.building.isSittable) return false;

                    if (t.IsForbidden(pawn)) return false;

                    if (!actor.CanReserve(t)) return false;

                    if (!t.IsSociallyProper(actor)) return false;

                    if (t.IsBurning()) return false;

                    if (t.HostileTo(pawn)) return false;

                    return true;
                }

                var chair = GenClosest.ClosestThingReachable(diningSpot.Position, diningSpot.Map, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.OnCell, TraverseParms.For(actor), 2,
                    t => BaseChairValidator(t) && t.Position.GetDangerFor(pawn, t.Map) == Danger.None);
                if (chair == null)
                {
                    Log.Message($"{pawn.NameShortColored} could not find a chair around {diningSpot.Position}.");
                    if (diningSpot.MayDineStanding)
                    {
                        targetPosition = RCellFinder.SpotToChewStandingNear(actor, diningSpot);
                        var chewSpotDanger = targetPosition.GetDangerFor(pawn, actor.Map);
                        if (chewSpotDanger != Danger.None)
                        {
                            actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
                            return;
                        }
                    }
                }

                if (chair != null)
                {
                    targetPosition = chair.Position;
                    actor.Reserve(chair, actor.CurJob);
                }

                actor.Map.pawnDestinationReservationManager.Reserve(actor, actor.CurJob, targetPosition);
                actor.pather.StartPath(targetPosition, PathEndMode.OnCell);
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            return toil;
        }

        public static Toil TurnToEatSurface(TargetIndex eatSurfaceInd)
        {
            var toil = new Toil();
            toil.initAction = delegate { toil.actor.jobs.curDriver.rotateToFace = eatSurfaceInd; };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        public static Toil WaitForWaiter(Pawn pawn, TargetIndex diningSpotInd, TargetIndex waiterInd)
        {
            var toil = new Toil();
            toil.initAction = () => {
                var driver = toil.actor.jobs.curDriver as JobDriver_Dine;
                driver.wantsToOrder = true;
            };
            toil.tickAction = () => {
                if (diningSpotInd != 0 && toil.actor.CurJob.GetTarget(diningSpotInd).IsValid)
                {
                    toil.actor.rotationTracker.FaceCell(toil.actor.CurJob.GetTarget(diningSpotInd).Cell);
                }
            };
            toil.AddFinishAction(() => {
                var driver = toil.actor.jobs.curDriver as JobDriver_Dine;
                driver.wantsToOrder = false;
            });

            toil.defaultDuration = 1500;
            toil.WithProgressBarToilDelay(TargetIndex.A);
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.FailOnDestroyedOrNull(diningSpotInd);
            toil.socialMode = RandomSocialMode.SuperActive;

            return toil;
        }

        public static Toil WaitForMeal(Pawn pawn, TargetIndex waiterInd, TargetIndex mealInd)
        {
            var toil = new Toil();
            toil.initAction = () => toil.actor.jobs.curDriver.ticksLeftThisToil = 500;
            toil.tickAction = () => {
                //if (diningSpotInd != 0 && toil.actor.CurJob.GetTarget(diningSpotInd).IsValid)
                //{
                //    toil.actor.rotationTracker.FaceCell(toil.actor.CurJob.GetTarget(diningSpotInd).Cell);
                //}
            };
            toil.defaultDuration = 500;
            toil.WithProgressBarToilDelay(TargetIndex.A);
            toil.defaultCompleteMode = ToilCompleteMode.Delay;
            toil.socialMode = RandomSocialMode.SuperActive;
            return toil;
        }
    }
}