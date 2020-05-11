using RimWorld;
using Verse;
using Verse.AI;

namespace Restaurant.Waiting
{
    public static class WaitingUtility
    {
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

            return null;
            
            void OnDoneTalking()
            {
                var patron = toil.GetActor().CurJob.GetTarget(patronInd).Thing as Pawn;
                if (patron == null)
                {
                    toil.GetActor().jobs.EndCurrentJob(JobCondition.Errored);
                    return;
                }

                var settings = patron.Map.GetSettings();
                var desiredFoodDef = settings.GetBestFoodTypeFor(patron, !patron.IsTeetotaler());
                settings.RequestMealFor(patron, desiredFoodDef);
            }
        }
    }
}
