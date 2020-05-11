using Restaurant.Dining;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Restaurant.Waiting
{
    public static class WaitingUtility
    {
        public static readonly JobDef waitDef = DefDatabase<JobDef>.GetNamed("Restaurant_Wait");
  
        public static Toil TakeOrder(Pawn pawn, TargetIndex patronInd)
        {
            // Talk to patron
            var toil = Toils_Interpersonal.Interact(patronInd, InteractionDefOf.Chitchat);
            toil.defaultDuration = 5000;
            toil.WithProgressBarToilDelay(patronInd, true);
            toil.activeSkill = () => SkillDefOf.Social;
            toil.FailOnDownedOrDead(patronInd);
            toil.FailOnMentalState(patronInd);
            //toil.FailOnNotDining(patronInd);
            toil.AddFinishAction(OnDoneTalking);

            return toil;
            
            void OnDoneTalking()
            {
                var patron = toil.GetActor().CurJob.GetTarget(patronInd).Thing as Pawn;
                if (patron == null)
                {
                    toil.GetActor().jobs.EndCurrentJob(JobCondition.Errored);
                    return;
                }

                var settings = patron.GetRestaurant();
                var desiredFoodDef = settings.GetBestFoodTypeFor(patron, !patron.IsTeetotaler());
                settings.RequestMealFor(patron, desiredFoodDef);

                if (!(patron.jobs.curDriver is JobDriver_Dine driver))
                {
                    Log.Error($"{patron.NameShortColored} is not dining!");
                    return;
                }
                driver.OnOrderTaken(desiredFoodDef, toil.GetActor());
            }
        }
    }
}
