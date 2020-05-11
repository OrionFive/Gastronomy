using Restaurant.Dining;
using RimWorld;
using Verse;
using Verse.AI;

namespace Restaurant.Waiting
{
    public static class WaitingUtility
    {
        public static readonly JobDef waitDef = DefDatabase<JobDef>.GetNamed("Restaurant_Wait");
        public static readonly JobDef serveDef = DefDatabase<JobDef>.GetNamed("Restaurant_Serve");

        public static Toil TakeOrder(TargetIndex patronInd)
        {
            // Talk to patron
            var toil = Toils_Interpersonal.Interact(patronInd, InteractionDefOf.Chitchat);
            toil.tickAction = () => toil.actor.rotationTracker.FaceCell(toil.actor.CurJob.GetTarget(patronInd).Cell);
            toil.defaultDuration = 500;
            toil.WithProgressBarToilDelay(patronInd, true);
            toil.activeSkill = () => SkillDefOf.Social;
            toil.FailOnDownedOrDead(patronInd);
            toil.FailOnMentalState(patronInd);
            toil.AddFinishAction(OnDoneTalking);

            return toil;
            
            void OnDoneTalking()
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
            Toil findCell = new Toil();
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
            Toil clearOrder = new Toil();
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
                    patronDriver.ServeFood(food);
                    Log.Message($"{actor.NameShortColored} has completed order for {patron.NameShortColored} with {food.Label}.");
                    actor.GetRestaurant().CompleteOrderFor(patron);
                }
            };
            return clearOrder;
        }
    }
}
