using System;
using System.Collections.Generic;
using Gastronomy.Restaurant;
using RimWorld;
using Verse;
using Verse.AI;

namespace Gastronomy.Dining
{
    public static class Toils_Dining
    {
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

                    if (!t.Position.AdjacentToCardinal(diningSpot.Position)) return false;

                    if (t.IsForbidden(pawn)) return false;

                    if (!actor.CanReserve(t)) return false;

                    if (!t.IsSociallyProper(actor)) return false;

                    if (t.IsBurning()) return false;

                    if (t.HostileTo(pawn)) return false;

                    if (t.Position.GetDangerFor(pawn, t.Map) > JobUtility.MaxDangerDining) return false;
                    return true;
                }

                var chairs = new List<Building>(4);
                diningSpot.GetReservationSpots(chairs);
                chairs.RemoveAll(c => !BaseChairValidator(c));
                //GenClosest.ClosestThingReachable(diningSpot.Position, diningSpot.Map, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.OnCell, TraverseParms.For(actor), 2, BaseChairValidator);
                if (chairs.Count == 0)
                {
                    Log.Message($"{pawn.NameShortColored} could not find a chair around {diningSpot.Position}.");
                    if (diningSpot.MayDineStanding)
                    {
                        targetPosition = RCellFinder.SpotToChewStandingNear(actor, diningSpot);
                        var chewSpotDanger = targetPosition.GetDangerFor(pawn, actor.Map);
                        if (chewSpotDanger != JobUtility.MaxDangerDining)
                        {
                            Log.Message($"{pawn.NameShortColored} could not find a save place around {diningSpot.Position} ({chewSpotDanger}).");
                            actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
                            return;
                        }
                    }
                }
                else
                {
                    var chair = chairs.RandomElement();
                    targetPosition = chair.Position;
                    actor.Reserve(chair, actor.CurJob);
                }

                actor.Map.pawnDestinationReservationManager.Reserve(actor, actor.CurJob, targetPosition);
                actor.pather.StartPath(targetPosition, PathEndMode.OnCell);
            };
            toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
            return toil;
        }

        public static Toil TurnToEatSurface(TargetIndex eatSurfaceInd, TargetIndex foodInd = TargetIndex.None)
        {
            var toil = new Toil();
            toil.initAction = delegate {
                toil.actor.jobs.curDriver.rotateToFace = eatSurfaceInd;
                if (foodInd != TargetIndex.None)
                {
                    var thing = toil.actor.CurJob.GetTarget(foodInd).Thing;
                    if (thing?.def.rotatable == true)
                    {
                        thing.Rotation = Rot4.FromIntVec3(toil.actor.CurJob.GetTarget(eatSurfaceInd).Cell - toil.actor.Position);
                    }
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            return toil;
        }

        public static Toil WaitForWaiter(TargetIndex diningSpotInd, TargetIndex waiterInd)
        {
            var toil = new Toil();
            toil.initAction = () => {
                GetDriver(toil).wantsToOrder = true;
                GetDriver(toil).OnStartedWaiting();
            };
            toil.tickAction = () => {
                if (diningSpotInd != 0 && toil.actor.CurJob.GetTarget(diningSpotInd).IsValid)
                {
                    toil.actor.rotationTracker.FaceCell(toil.actor.CurJob.GetTarget(diningSpotInd).Cell);
                }
                if(!GetDriver(toil).wantsToOrder) GetDriver(toil).ReadyForNextToil();
            };
            toil.AddFinishAction(() => GetDriver(toil).wantsToOrder = false);

            toil.defaultDuration = 3000;
            toil.WithProgressBarToilDelayReversed(diningSpotInd, 3000, true);
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.FailOnDestroyedOrNull(diningSpotInd);
            toil.FailOnDurationExpired(); // Duration over? Fail job!
            toil.FailOnMyRestaurantClosedForDining();
            toil.FailOnHasShift();
            toil.FailOnDangerous(JobUtility.MaxDangerDining);
            toil.socialMode = RandomSocialMode.Normal;
            return toil;
        }

        private static JobDriver_Dine GetDriver(Toil t) => t.actor.jobs.curDriver as JobDriver_Dine;

        public static Toil WaitForMeal(TargetIndex mealInd, TargetIndex chairInd)
        {
            var toil = new Toil();
            toil.initAction = () =>
            {
                var order = toil.actor.FindValidOrder();
                if (order?.delivered == true && (order.consumable?.Spawned == true || order.consumable?.ParentHolder == toil.actor.inventory))
                {
                    var consumable = order.consumable;
                    toil.actor.CurJob.SetTarget(mealInd, consumable);
                    Log.Message($"{toil.actor.NameShortColored} has already received order: {consumable?.Label}");
                    if (toil.actor.inventory.Contains(consumable))
                    {
                        //Log.Message($"{toil.actor.NameShortColored} has {food.Label} in inventory.");
                        GetDriver(toil).ReadyForNextToil();
                    }
                    else if (consumable.Position.AdjacentTo8Way(toil.actor.Position))
                    {
                        //Log.Message($"{toil.actor.NameShortColored} has {food.Label} on table.");
                        consumable.DeSpawn();
                        //var amount = toil.actor.inventory.innerContainer.TryAdd(order.consumable, 1, false);
                        //Log.Message($"{toil.actor.NameShortColored} received {amount} of {food.LabelShort} to his inventory.");
                        GetDriver(toil).ReadyForNextToil();
                    }
                    else
                    {
                        Log.Message($"{toil.actor.NameShortColored}'s food is somewhere else ({consumable?.Position}). Will wait.");
                        toil.actor.CurJob.SetTarget(mealInd, null);
                        order.delivered = false;
                        GetDriver(toil).OnStartedWaiting();
                    }
                }
                else if (order?.delivered == true)
                {
                    // Order not spawned? Already eaten it, or something happened to it
                    // Let it go.
                    Log.Warning($"{toil.actor.NameShortColored}'s food is gone. Already eaten?");
                    order.Restaurant.Orders.CancelOrder(order);
                    GetDriver(toil).EndJobWith(JobCondition.Incompletable);
                }
                GetDriver(toil).OnStartedWaiting();
            };
            toil.tickAction = () => {
                var target = toil.actor.CurJob.GetTarget(mealInd);
                if (target.HasThing && target.IsValid && target.Thing.ParentHolder == toil.actor.inventory)
                {
                    // Waiting done
                    // Set job.count to amount to consume (important for taking to carrier!)
                    toil.actor.CurJob.count = target.Thing.stackCount;
                    GetDriver(toil).ReadyForNextToil();
                }
            };
            toil.defaultDuration = 3000;
            toil.WithProgressBarToilDelayReversed(chairInd, 3000, true);
            toil.defaultCompleteMode = ToilCompleteMode.Never;
            toil.FailOnHasShift();
            toil.FailOnDangerous(JobUtility.MaxDangerDining);
            toil.FailOnDurationExpired(); // Duration over? Fail job!
            toil.socialMode = RandomSocialMode.Normal;
            return toil;
        }

        public static Toil WithProgressBarToilDelayReversed(
            this Toil toil,
            TargetIndex ind,
            int toilDuration,
            bool interpolateBetweenActorAndTarget = false,
            float offsetZ = -0.5f)
        {
            return toil.WithProgressBar(ind, () => (float) ((double) toil.actor.jobs.curDriver.ticksLeftThisToil / (double) toilDuration), interpolateBetweenActorAndTarget, offsetZ);
        }


        public static Toil WaitDuringDinner(TargetIndex lookAtInd, int minDuration, int maxDuration)
        {
            var toil = Toils_General.Wait(Rand.Range(minDuration, maxDuration), lookAtInd);
            toil.socialMode = RandomSocialMode.Normal;
            return toil;
        }

        public static Toil MakeTableMessy(TargetIndex diningSpotInd, Func<IntVec3> patronPos)
        {
            var toil = new Toil {atomicWithPrevious = true};
            toil.initAction = () => {
                if (toil.actor.CurJob.GetTarget(diningSpotInd).Thing is DiningSpot diningSpot)
                {
                    diningSpot.SetSpotMessy(patronPos.Invoke());
                }
            };
            return toil;
        }

        public static Toil OnCompletedMeal(Pawn pawn)
        {
            return new Toil {atomicWithPrevious = true, initAction = () =>
            {
                pawn.FindValidOrder()?.Restaurant.Orders.OnFinishedEatingOrder(pawn);
            }};
        }

        /// <summary>
        /// Kept to not break saves.
        /// </summary>
        public static Toil Obsolete() => new Toil {atomicWithPrevious = true};
    }
}
