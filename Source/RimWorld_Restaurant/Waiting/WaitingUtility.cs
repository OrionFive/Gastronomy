using System.Collections.Generic;
using Restaurant.Dining;
using RimWorld;
using Verse;
using Verse.AI;

namespace Restaurant.Waiting
{
    public static class WaitingUtility
    {
        public static readonly JobDef takeOrderDef = DefDatabase<JobDef>.GetNamed("Restaurant_TakeOrder");
        public static readonly JobDef serveDef = DefDatabase<JobDef>.GetNamed("Restaurant_Serve");
        public static readonly JobDef makeTableDef = DefDatabase<JobDef>.GetNamed("Restaurant_MakeTable");

        public static Toil TakeOrder(TargetIndex patronInd)
        {
            // Talk to patron
            var toil = Toils_Interpersonal.Interact(patronInd, InteractionDefOf.Chitchat);
            toil.initAction = () => {
                var patron = toil.actor.CurJob.GetTarget(patronInd).Pawn;
                if (patron != null)
                {
                    PawnUtility.ForceWait(patron, toil.defaultDuration);
                }
            };
            toil.tickAction = () => toil.actor.rotationTracker.FaceCell(toil.actor.CurJob.GetTarget(patronInd).Cell);
            toil.socialMode = RandomSocialMode.Off;
            toil.defaultDuration = 500;
            toil.WithProgressBarToilDelay(patronInd, true);
            toil.activeSkill = () => SkillDefOf.Social;
            toil.FailOnDownedOrDead(patronInd);
            toil.FailOnMentalState(patronInd);
            toil.AddPreInitAction(CreateOrder);

            return toil;
            
            void CreateOrder()
            {
                if (!(toil.GetActor().CurJob.GetTarget(patronInd).Thing is Pawn patron))
                {
                    toil.GetActor().jobs.EndCurrentJob(JobCondition.Errored);
                    return;
                }

                var settings = patron.GetRestaurant();
                var desiredFoodDef = settings.GetBestFoodTypeFor(patron, !patron.IsTeetotaler());
                settings.CreateOrder(patron, desiredFoodDef);

                if (!(patron.jobs.curDriver is JobDriver_Dine driver))
                {
                    Log.Error($"{patron.NameShortColored} is not dining!");
                    return;
                }
                driver.OnOrderTaken(desiredFoodDef, toil.GetActor());
            }
        }

        public static Toil FindRandomAdjacentCell(TargetIndex adjacentToInd, TargetIndex cellInd)
        {
            Toil findCell = new Toil {atomicWithPrevious = true};
            findCell.initAction = delegate {
                Pawn actor = findCell.actor;
                Job curJob = actor.CurJob;
                LocalTargetInfo target = curJob.GetTarget(adjacentToInd);
                if (target.HasThing && (!target.Thing.Spawned || target.Thing.Map != actor.Map))
                {
                    Log.Error(actor + " could not find standable cell adjacent to " + target + " because this thing is either unspawned or spawned somewhere else.");
                    actor.jobs.curDriver.EndJobWith(JobCondition.Errored);
                }
                else
                {
                    // Try radius 2-4
                    for (int radius = 1; radius < 5; radius++)
                    {
                        if (CellFinder.TryFindRandomReachableCellNear(target.Cell, actor.Map, radius, TraverseParms.For(TraverseMode.NoPassClosedDoors), c => c.Standable(actor.Map) && c.GetFirstPawn(actor.Map) == null, null, out var result))
                        {
                            curJob.SetTarget(cellInd, result);
                            Log.Message($"{actor.NameShortColored} found a place to stand at {result}. radius = {radius}");
                            return;
                        }
                    }
                    Log.Error(actor + " could not find standable cell adjacent to " + target);
                    actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
                }
            };
            return findCell;
        }

        public static Toil ClearOrder(TargetIndex patronInd, TargetIndex foodInd)
        {
            Toil clearOrder = new Toil {atomicWithPrevious = true};
            clearOrder.initAction = delegate {
                Pawn actor = clearOrder.actor;
                Job curJob = actor.CurJob;
                LocalTargetInfo targetPatron = curJob.GetTarget(patronInd);
                LocalTargetInfo targetFood = curJob.GetTarget(foodInd);

                var patron = targetPatron.Pawn;
                if (!targetPatron.HasThing || patron == null)
                {
                    Log.Error($"Can't clear order. No patron.");
                    return;
                }

                var food = targetFood.Thing;
                if (!targetFood.HasThing || food == null)
                {
                    Log.Error($"Can't clear order. No food.");
                    return;
                }

                if (patron.jobs.curDriver is JobDriver_Dine patronDriver)
                {
                    var transferred = actor.carryTracker.innerContainer.TryTransferToContainer(food, patron.inventory.innerContainer, false);
                    if (transferred)
                    {
                        patronDriver.OnTransferredFood(food);
                        Log.Message($"{actor.NameShortColored} has completed order for {patron.NameShortColored} with {food.Label}.");
                        actor.GetRestaurant().CompleteOrderFor(patron);
                    }
                    else
                    {
                        Log.Error($"{actor.NameShortColored} failed to transfer {food?.Label} to {patron.NameShortColored}.");
                    }
                }
            };
            return clearOrder;
        }

        public static Toil GetDiningSpot(TargetIndex patronInd, TargetIndex diningSpotInd)
        {
            Toil toil = new Toil {atomicWithPrevious = true};
            toil.initAction = () => {
                var patron = toil.actor.CurJob?.GetTarget(patronInd).Pawn;
                if (patron == null)
                {
                    Log.Message($"Couldn't get patron.");
                    toil.actor.jobs.EndCurrentJob(JobCondition.Errored);
                }
                else
                {
                    var diningSpot = patron.GetDriver<JobDriver_Dine>()?.DiningSpot;
                    if (diningSpot == null)
                    {
                        Log.Message($"Couldn't get dining spot from {patron.NameShortColored} doing {patron.jobs.curDriver?.GetType().Name}.");
                        toil.actor.jobs.EndCurrentJob(JobCondition.Errored);
                    }
                    else
                    {
                        toil.actor.CurJob?.SetTarget(diningSpotInd, diningSpot);
                    }
                }
            };
            return toil;
        }

        public static Toil MakeTableReady(TargetIndex diningSpotInd, TargetIndex patronInd)
        {
            Toil toil = new Toil {defaultCompleteMode = ToilCompleteMode.Delay, defaultDuration = 100};
            toil.WithProgressBarToilDelay(diningSpotInd, true);
            toil.AddFinishAction(() => {
                var target = toil.actor.CurJob.GetTarget(patronInd);
                IntVec3 chairPos;

                if (target.HasThing)
                {
                    var patron = target.Pawn;
                    chairPos = GetChairPosition(patron);
                }
                else if (target.IsValid) chairPos = target.Cell;
                else
                {
                    toil.actor.jobs.EndCurrentJob(JobCondition.Errored);
                    return;
                }

                Log.Message($"About to make spot ready ({toil.actor.CurJob.GetTarget(diningSpotInd).Thing?.Label}) at {toil.actor.CurJob.GetTarget(diningSpotInd).Cell}.");
                if (toil.actor.CurJob.GetTarget(diningSpotInd).Thing is DiningSpot diningSpot)
                {
                    diningSpot.SetSpotReady(chairPos);
                }
            });
            toil.WithEffect(EffecterDefOf.Clean, diningSpotInd);
            toil.PlaySustainerOrSound(() => SoundDefOf.Interact_CleanFilth);
            return toil;
        }

        public static Toil UpdateOrderConsumableTo(TargetIndex patronInd, TargetIndex consumableInd)
        {
            Toil toil = new Toil {atomicWithPrevious = true};
            toil.initAction = () => {
                var patron = toil.actor.CurJob?.GetTarget(patronInd).Pawn;
                if (patron == null) toil.actor.jobs.EndCurrentJob(JobCondition.Errored);
                else
                {
                    var consumable = toil.actor.CurJob.GetTarget(consumableInd).Thing;
                    if (consumable == null) toil.actor.jobs.EndCurrentJob(JobCondition.Errored);
                    else
                    {
                        //Log.Message($"{toil.actor.NameShortColored} updated the consumable {toil.actor.GetRestaurant().GetOrderFor(patron).consumable.Label} to {consumable.Label}.");
                        toil.actor.GetRestaurant().GetOrderFor(patron).consumable = consumable;
                    }
                }
            };
            return toil;
        }

        public static Toil GetDiningSpotCellForMakingTable(TargetIndex diningSpotInd, TargetIndex jobCellInd)
        {
            Toil toil = new Toil {atomicWithPrevious = true};
            toil.initAction = () => {
                if (toil.actor.CurJob?.GetTarget(diningSpotInd).Thing is DiningSpot diningSpot)
                {
                    var cell = diningSpot.GetUnmadeSpotCells().InRandomOrder().FirstOrFallback(LocalTargetInfo.Invalid);
                    toil.actor.CurJob.SetTarget(jobCellInd, cell);
                }
                else
                {
                    toil.actor.jobs.EndCurrentJob(JobCondition.Errored);
                }
            };
            return toil;
        }

        public static IntVec3 GetChairPosition(Pawn patron)
        {
            if(patron.pather.MovingNow)
                return patron.pather.Destination.Cell;
            return patron.Position;
        }
    }
}
