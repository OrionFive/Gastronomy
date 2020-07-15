using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Restaurant.Dining;
using RimWorld;
using Verse;
using Verse.AI;

namespace Restaurant
{
    public class RestaurantSettings : MapComponent
    {
        public readonly List<DiningSpot> diningSpots = new List<DiningSpot>();
        private int lastStockUpdateTick;
        [NotNull] private List<Order> orders = new List<Order>();
        [NotNull] private List<Pawn> spawnedDiningPawnsResult = new List<Pawn>();
        [NotNull] private List<Thing> stock = new List<Thing>();
        public IntVec3 testPos;

        public RestaurantSettings(Map map) : base(map) { }
        public bool IsOpenedRightNow { get; } = true;

        [NotNull]public List<Pawn> SpawnedDiningPawns
        {
            get
            {
                spawnedDiningPawnsResult.Clear();
                spawnedDiningPawnsResult.AddRange(map.mapPawns.AllPawnsSpawned.Where(pawn => pawn.jobs?.curDriver is JobDriver_Dine));
                return spawnedDiningPawnsResult;
            }
        }
        [NotNull]public IReadOnlyCollection<Thing> Stock => stock.AsReadOnly();
        [NotNull]public IEnumerable<Order> AvailableOrdersForServing => orders.Where(o => !o.delivered);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref testPos, "testPos");
            Scribe_Collections.Look(ref orders, "orders", LookMode.Deep);
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();

            diningSpots.Clear();
            diningSpots.AddRange(DiningUtility.GetAllDiningSpots(map));
            //Log.Message($"Finalized with {diningSpots.Count} dining spots.");
        }

        public bool HasAnyFoodFor([NotNull] Pawn pawn, bool allowDrug)
        {
            //Log.Message($"{pawn.NameShortColored}: HasFoodFor: Defs: {stock.Select(item=>item.def).Count(s => WillConsume(pawn, allowDrug, s))}");
            return stock.Select(item => item.def).Any(s => WillConsume(pawn, allowDrug, s));
        }

        public ThingDef GetBestFoodTypeFor([NotNull] Pawn pawn, bool allowDrug)
        {
            var best = stock.Select(item => item.def).Distinct().Where(def => WillConsume(pawn, allowDrug, def)).MaxBy(def => FoodUtility.FoodOptimality(pawn, null, def, 0));
            //Log.Message($"{pawn.NameShortColored}: GetBestFoodFor: {best?.label}");
            return best;
        }

        private static bool WillConsume(Pawn pawn, bool allowDrug, ThingDef s)
        {
            return (allowDrug || !s.IsDrug) && pawn.WillEat(s);
        }

        public override void MapComponentTick()
        {
            if (GenTicks.TicksGame < lastStockUpdateTick + 500) return;
            lastStockUpdateTick = GenTicks.TicksGame;
            stock = new List<Thing>(map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSource).Where(t => t.def.IsIngestible && IsInConsumableCategory(t.def.thingCategories)));
            //Log.Message($"Stock: {stock.Select(s => s.def.label).ToCommaList(true)}");
        }

        private static bool IsInConsumableCategory(List<ThingCategoryDef> defThingCategories)
        {
            if (defThingCategories == null) return false;
            if (defThingCategories.Contains(ThingCategoryDefOf.Drugs)) return true;
            if (defThingCategories.Contains(ThingCategoryDefOf.FoodMeals)) return true;
            if (defThingCategories.Contains(ThingCategoryDefOf.Foods)) return true;
            if (defThingCategories.Contains(ThingCategoryDefOf.PlantMatter)) return true;
            return false;
        }

        public void CreateOrder(Pawn patron, ThingDef consumableDef)
        {
            Log.Message($"{patron.NameShortColored} has ordered {consumableDef.label}.");

            // Already ordered?
            if (orders.Any(o => o.patron == patron))
            {
                Log.Message($"{patron.NameShortColored} has already ordered. Ignoring.");
                return;
            }

            // Already prepared?
            var available = stock.Where(item => item.def == consumableDef).Sum(item => item.stackCount);
            var ordered = orders.Count(o => o.consumableDef == consumableDef);

            if (available <= ordered)
            {
                Log.Message($"{consumableDef.label} has to be prepared first {available} available and {ordered} ordered.");
            }
            else
            {
                Log.Message($"{consumableDef.label} can be delivered. {available} available and {ordered} ordered.");
                //map.reservationManager.Reserve(patron, patron.CurJob, thing, 1, 1);
            }

            orders.Add(new Order
            {
                consumableDef = consumableDef,
                patron = patron,
                hasToBeMade = available <= ordered
            });
        }

        public bool IsBeingDelivered(Order order, Pawn pawn)
        {
            if (order.hasToBeMade) return false;
            if (order.delivered) return false;
            if (order.consumable == null) return false;
            //Log.Message($"Consumable found: {order.consumable.Label} at {order.consumable.Position}");
            return map.reservationManager.IsReservedAndRespected(order.consumable, pawn);
        }

        public void CancelOrder(Order order)
        {
            orders.Remove(order);
        }

        public void CompleteOrderFor(Pawn patron)
        {
            var order = orders.FirstOrDefault(o => o.patron == patron);
            if (order == null)
            {
                Log.Error($"Completed order for {patron.NameShortColored}. But there was none.");
                return;
            }
            order.delivered = true;
        }

        public Order GetOrderFor(Pawn patron)
        {
            var order = orders.FirstOrDefault(o => o.patron == patron);
            if (order != null) Log.Message($"Found an order of {order.consumableDef.label} for {patron.NameShortColored}. hasToBeMade? {order.hasToBeMade} IsBeingDelivered? {IsBeingDelivered(order, patron)} hasBeenDelivered? {order.delivered}");
            return order;
        }

        public void OnFinishedEatingOrder(Pawn patron)
        {
            orders.RemoveAll(o => o.patron == patron);
        }

        public Thing GetServableThing(Order order, Pawn pawn)
        {
            return Stock.Where(o => o.Spawned && o.def == order.consumableDef).OrderBy(o => pawn.Position.DistanceToSquared(o.Position)).FirstOrDefault(o => pawn.CanReserveAndReach(o, PathEndMode.Touch, Danger.None, 1, 1));
        }
    }
}
