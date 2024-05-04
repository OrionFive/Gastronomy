using System;
using System.Collections.Generic;
using System.Linq;
using CashRegister.TableTops;
using Gastronomy.Restaurant;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Gastronomy.Dining;

public static class DiningUtility
{
    public static readonly HashSet<ThingDef> ThingsWithCompCanDineAt = [];

    static DiningUtility()
    {
        TableTop_Events.onThingAffectedBySpawnedBuilding.AddListener(NotifyAffectedBySpawn);
        TableTop_Events.onThingAffectedByDespawnedBuilding.AddListener(NotifyAffectedByDespawn);
    }

    private static void NotifyAffectedBySpawn(Thing thing, Building building)
    {
        if (thing is DiningSpot)
        {
            thing.Destroy(DestroyMode.Cancel);
        }
    }

    private static void NotifyAffectedByDespawn(this Thing affected, Building building)
    {
        // Notify potential dining spots
        if (CanPossiblyDineAt(affected.def)) affected.TryGetComp<CompCanDineAt>()?.Notify_BuildingDespawned(building);
    }

    public static IEnumerable<DiningSpot> GetAllDiningSpots([NotNull] Map map)
    {
        return map.listerThings.ThingsOfDef(DiningDefOf.Gastronomy_DiningSpot).OfType<DiningSpot>();
    }

    public static IEnumerable<DiningSpot> FindDiningSpotsFor([NotNull] Pawn pawn, bool allowDrug, Predicate<Thing> extraSpotValidator = null)
    {
        // TODO: There should be some kind of caching for this, probably
        var restaurants = pawn.GetAllRestaurants().Where(r => r.CanDineHere(pawn));

        //Log.Message($"{pawn.NameShortColored} is looking for dining spots in {restaurants.Select(r=>r.Name).ToCommaList()}...");

        bool Validator(DiningSpot spot)
        {
            //Log.Message($"Validating spot for {pawn.NameShortColored}: forbidden = {spot.IsForbidden(pawn)}, social = {spot.IsSociallyProper(pawn)}, political = {IsPoliticallyProper(pawn, spot)}, "
            //            + $"canReserve = {CanReserve(pawn, spot)}, canDineHere = {spot.GetRestaurantsServing().Any(r => r.CanDineHere(pawn))}, open = {spot.GetRestaurantsServing().Any(r => r.IsOpenedRightNow)}, isDangerous = {RestaurantUtility.IsRegionDangerous(pawn, JobUtility.MaxDangerDining, spot.GetRegion())},"
            //            + $"extraValidator = {extraSpotValidator == null || extraSpotValidator.Invoke(spot)}, canReach = {pawn.CanReach(spot, PathEndMode.ClosestTouch, JobUtility.MaxDangerDining)}");
            return !spot.IsForbidden(pawn) && spot.IsSociallyProper(pawn) && IsPoliticallyProper(pawn, spot) && CanReserve(pawn, spot)
                   && !RestaurantUtility.IsRegionDangerous(pawn, JobUtility.MaxDangerDining, spot.GetRegion()) && (extraSpotValidator == null || extraSpotValidator.Invoke(spot)) &&
                   pawn.CanReach(spot, PathEndMode.ClosestTouch, JobUtility.MaxDangerDining);
        }

        return restaurants.SelectMany(r => r.diningSpots).Distinct().Where(Validator);
    }

    private static bool IsPoliticallyProper(Pawn pawn, DiningSpot spot)
    {
        return !spot.HostileTo(pawn);
    }

    private static bool CanReserve(Pawn pawn, DiningSpot spot)
    {
        var maxReservations = spot.GetMaxReservations();
        if (maxReservations == 0) return false;
        return pawn.CanReserve(spot, maxReservations, 0);
    }

    public static void RegisterDiningSpotHolder(ThingWithComps thing)
    {
        ThingsWithCompCanDineAt.Add(thing.def);
    }

    public static bool CanPossiblyDineAt(ThingDef def)
    {
        return ThingsWithCompCanDineAt.Contains(def);
    }

    public static bool IsAbleToDine(this Pawn getter)
    {
        var canManipulate = getter.RaceProps.ToolUser && getter.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation);
        if (!canManipulate) return false;

        var canTalk = getter.health.capacities.CapableOf(PawnCapacityDefOf.Talking);
        if (!canTalk) return false;

        var canMove = getter.health.capacities.CapableOf(PawnCapacityDefOf.Moving);
        if (!canMove) return false;

        if (getter.InMentalState) return false;

        return true;
    }

    public static DrugPolicyEntry GetPolicyFor(this Pawn pawn, ThingDef def)
    {
        var policy = pawn.drugs.CurrentPolicy;
        for (var i = 0; i < policy.Count; i++)
        {
            var entry = policy[i];
            if (entry.drug == def) return entry;
        }

        return null;
    }

    /// <summary>
    ///     Pay for all money owed
    /// </summary>
    public static void PayForMeal(this Pawn pawn, ThingOwner payTarget, out Thing paidSilver)
    {
        paidSilver = null;

        var debt = pawn.GetAllRestaurants().Select(r => new { restaurant = r, debt = r.Debts.GetDebt(pawn) }).FirstOrDefault(d => d.debt != null);
        if (debt == null) return;

        var debtAmount = Mathf.FloorToInt(debt.debt.amount);
        if (debtAmount < 0) return;
        var cash = pawn.inventory.innerContainer.FirstOrDefault(t => t?.def == ThingDefOf.Silver);
        if (cash == null) return;

        var payAmount = Mathf.Min(cash.stackCount, debtAmount);
        var paid = pawn.inventory.innerContainer.TryTransferToContainer(cash, payTarget, payAmount, out paidSilver, false);
        debt.restaurant.Debts.PayDebt(pawn, paid);
    }

    public static void GiveBoughtFoodThought(Pawn pawn)
    {
        if (pawn.needs.mood == null) return;
        pawn.needs.mood.thoughts.memories.RemoveMemoriesOfDef(DiningDefOf.Gastronomy_BoughtFood);
        pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtMaker.MakeThought(DiningDefOf.Gastronomy_BoughtFood, GetBoughtFoodStage(pawn)));
    }

    public static void GiveServiceThought(Pawn patron, Pawn waiter, float hoursWaited)
    {
        if (patron.needs.mood == null) return;

        var stage = GetServiceStage(patron, waiter);
        patron.needs.mood.thoughts.memories.TryGainMemory(ThoughtMaker.MakeThought(DiningDefOf.Gastronomy_Serviced, stage), waiter);
        patron.needs.mood.thoughts.memories.TryGainMemory(ThoughtMaker.MakeThought(DiningDefOf.Gastronomy_ServicedMood, stage));
    }

    public static void GiveWaitThought(Pawn patron)
    {
        patron.needs.mood?.thoughts.memories.TryGainMemory(DiningDefOf.Gastronomy_HadToWait);
    }

    private static int GetServiceStage(Pawn patron, Pawn waiter)
    {
        var score = 1 * waiter.GetStatValue(StatDefOf.SocialImpact);
        score += waiter.story.traits.DegreeOfTrait(TraitDefOf.Industriousness) * 0.25f;
        score += waiter.story.traits.DegreeOfTrait(DefDatabase<TraitDef>.GetNamed("Beauty")) * 0.25f;
        score += waiter.story.traits.HasTrait(TraitDefOf.Kind) ? 0.25f : 0;
        score += patron.story.traits.HasTrait(TraitDefOf.Kind) ? 0.15f : 0;
        score += waiter.story.traits.HasTrait(TraitDefOf.Abrasive) ? -0.2f : 0;
        score += waiter.story.traits.HasTrait(TraitDefOf.AnnoyingVoice) ? -0.2f : 0;
        score += waiter.story.traits.HasTrait(TraitDefOf.CreepyBreathing) ? -0.1f : 0;
        if (waiter.needs.mood != null) score += (waiter.needs.mood.CurLevelPercentage - 0.5f) * 0.6f; // = +-0.3
        score += patron.relations.OpinionOf(waiter) / 200f; // = +-0.5
        var stage = Mathf.RoundToInt(Mathf.Clamp(score, 0, 2) * 2); // 0-4
        //Log.Message($"Service score of {waiter.NameShortColored} serving {patron.NameShortColored}:\n"
        //            + $"opinion = {patron.relations.OpinionOf(waiter) * 1f / 200:F2}, mood = {(waiter.needs.mood.CurLevelPercentage - 0.5f) * 0.6f} final = {score:F2}, stage = {stage}");

        return stage;
    }

    private static int GetBoughtFoodStage(Pawn pawn)
    {
        var order = pawn.FindValidOrder();
        var restaurant = order?.Restaurant;

        if (restaurant == null) return 0;
        if (restaurant.guestPricePercentage <= 0) return 0;
        var stage = PriceTypeUtlity.ClosestPriceType(restaurant.guestPricePercentage) switch
        {
            PriceType.Undefined => 0,
            PriceType.VeryCheap => 1,
            PriceType.Cheap => 2,
            PriceType.Normal => 3,
            PriceType.Expensive => 4,
            PriceType.Exorbitant => 5,
            _ => throw new ArgumentOutOfRangeException("Gastronomy received an invalid PriceType.")
        };
        if (pawn.story.traits.HasTrait(TraitDefOf.Greedy)) stage += 1;
        if (pawn.story.traits.HasTrait(TraitDef.Named("Gourmand"))) stage -= 1;
        return Mathf.Clamp(stage, 0, 5);
    }

    public static void OnDiningSpotCreated([NotNull] DiningSpot diningSpot)
    {
        foreach (var restaurant in diningSpot.GetAllRestaurants())
        {
            restaurant.OnDiningSpotsChanged();
        }
    }

    public static void OnDiningSpotRemoved([NotNull] Map map)
    {
        foreach (var restaurant in map.GetRestaurantsManager().restaurants)
        {
            restaurant.OnDiningSpotsChanged();
        }
    }

    // Copied from ToilEffects, had to remove Faction check
    public static Toil WithProgressBar(
        this Toil toil,
        TargetIndex ind,
        Func<float> progressGetter,
        bool interpolateBetweenActorAndTarget = false,
        float offsetZ = -0.5f)
    {
        Effecter effecter = null;
        toil.AddPreTickAction(() =>
        {
            //if (toil.actor.Faction != Faction.OfPlayer)
            //    return;
            if (effecter == null)
            {
                effecter = EffecterDefOf.ProgressBar.Spawn();
            }
            else
            {
                var target = toil.actor.CurJob.GetTarget(ind);
                if (!target.IsValid || (target.HasThing && !target.Thing.Spawned))
                    effecter.EffectTick((TargetInfo)toil.actor, TargetInfo.Invalid);
                else if (interpolateBetweenActorAndTarget)
                    effecter.EffectTick(toil.actor.CurJob.GetTarget(ind).ToTargetInfo(toil.actor.Map), (TargetInfo)toil.actor);
                else
                    effecter.EffectTick(toil.actor.CurJob.GetTarget(ind).ToTargetInfo(toil.actor.Map), TargetInfo.Invalid);
                var mote = ((SubEffecter_ProgressBar)effecter.children[0]).mote;
                if (mote == null)
                    return;
                mote.progress = Mathf.Clamp01(progressGetter());
                mote.offsetZ = offsetZ;
            }
        });
        toil.AddFinishAction(() =>
        {
            if (effecter == null)
                return;
            effecter.Cleanup();
            effecter = null;
        });
        return toil;
    }

    public static bool HasToPay(this Pawn patron)
    {
        return patron.IsGuest();
    }

    public static bool CanHaveDebt(this Pawn patron)
    {
        return patron is { Dead: false, IsPrisoner: false } && patron.HasToPay();
    }

    public static bool IsChairAdjacent(IntVec3 position, Map map)
    {
        for (var i = 0; i < 4; i++)
        {
            var intVec = position + new Rot4(i).FacingCell;
            var things = intVec.GetThingList(map);
            foreach (var thing in things)
            {
                // Check if it's a sittable thing
                if (thing.def.building is not { isSittable: true }) continue;
                // Check if it's facing correctly
                if (thing.def.rotatable && intVec + thing.Rotation.FacingCell != position) continue;
                return true;
            }
        }

        return false;
    }
}