using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;
using Verse;

namespace Gastronomy.Restaurant
{
    public class RestaurantOrders : IExposable
    {
        private List<Order> orders = new List<Order>();

        [NotNull] public ReadOnlyCollection<Order> AllOrders => orders.AsReadOnly();
        [NotNull] public IEnumerable<Order> AvailableOrdersForServing => orders.Where(o => !o.delivered && Stock.GetAllStockOfDef(o.consumableDef).Count > 0);
        [NotNull] public IEnumerable<Order> AvailableOrdersForCooking => orders.Where(o => !o.delivered && !CanBeOrdered(o.consumable));
        [NotNull] private RestaurantStock Stock => Restaurant.Stock;
        [NotNull] private RestaurantMenu Menu => Restaurant.Menu;
        [NotNull] private RestaurantController Restaurant { get; }

        public RestaurantOrders([NotNull] RestaurantController restaurant)
        {
            Restaurant = restaurant;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref orders, "orders", LookMode.Deep, Restaurant);
            orders ??= new List<Order>();
        }

        public void RareTick()
        {
            orders.RemoveAll(o => !o.patron.Spawned || o.patron.Dead || IsInvalidOrder(o));
        }

        private bool IsInvalidOrder(Order o)
        {
            if (o.delivered) return false;
            if (o.consumable == null || !o.consumable.SpawnedOrAnyParentSpawned) return false;
            if (!Menu.IsOnMenu(o.consumableDef)) return true;
            // TODO: Preference to allow ordering out of stock items

            if (!Restaurant.MayDineHere(o.patron))
            {
                Log.Message($"Order for {o.patron?.NameShortColored} has been removed. May not eat here.");
                return true;
            }

            if (!IsBeingDelivered(o) && !Stock.IsAvailable(o.consumable))
            {
                Log.Message($"Order for {o.patron?.NameShortColored} ({o.consumableDef?.label}) had to be canceled. Not in stock.");
                return true;
            }
            return false;
        }

        public bool CanBeOrdered([CanBeNull] Thing consumable)
        {
            if (consumable == null) return false;
            // Do we have any item of that type that isn't part of any order?
            var orderedAmount = AllOrders.Where(o => o.consumable == consumable).Sum(o => 1);
            var stockedAmount = Stock.IsAvailable(consumable) ? consumable.stackCount : 0;
            // Log.Message($"{consumable.LabelCap} can be ordered? ordered = {orderedAmount}, stocked = {stockedAmount}");
            return stockedAmount > orderedAmount;
        }

        public void CreateOrder(Pawn patron, Thing consumable)
        {
            //Log.Message($"{patron.NameShortColored} has ordered {consumableDef.label}.");

            // Already ordered?
            if (orders.Any(o => o.patron == patron))
            {
                Log.Message($"{patron.NameShortColored} has already ordered. Ignoring.");
                return;
            }

            // Already prepared?
            var available = Stock.IsAvailable(consumable);
            var ordered = orders.Any(o => o.consumable == consumable);

            /*
            if (available <= ordered)
            {
                Log.Message($"{consumableDef.label} has to be prepared first. {available} available and {ordered} ordered.");
            }
            else
            {
                Log.Message($"{consumableDef.label} can be delivered. {available} available and {ordered} ordered.");
                //map.reservationManager.Reserve(patron, patron.CurJob, thing, 1, 1);
            }
            */

            orders.Add(new Order(Restaurant) {consumable = consumable, consumableDef = consumable.def, patron = patron, hasToBeMade = !available || ordered});
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
            //if (order != null) Log.Message($"Found an order of {order.consumableDef.label} for {patron.NameShortColored}. hasToBeMade? {order.hasToBeMade} IsBeingDelivered? {IsBeingDelivered(order)} hasBeenDelivered? {order.delivered}");
            return order;
        }

        public void OnFinishedEatingOrder(Pawn patron)
        {
            orders.RemoveAll(o => o.patron == patron);
        }

        public bool IsBeingDelivered(Order order, Pawn waiter = null)
        {
            if (order.hasToBeMade) return false;
            if (order.delivered) return false;
            if (order.consumable == null) return false;
            if (!order.consumable.SpawnedOrAnyParentSpawned) return false;
            //Log.Message($"Consumable found: {order.consumable.Label} at {order.consumable.Position}");
            var reserver = Restaurant.Map.reservationManager.FirstRespectedReserver(order.consumable, waiter ?? order.patron);
            if (reserver == null) return false;
            return reserver != waiter;
        }

        /// <summary>
        /// Check if the order is somehow broken.
        /// </summary>
        /// <returns>True of order is fine, false if it's not.</returns>
        public bool CheckOrderOfWaitingPawn(Pawn patron)
        {
            if (patron == null)
            {
                Log.Warning("Patron not set.");
                return false;
            }
            var order = orders?.FirstOrDefault(o => o?.patron == patron);
            if (order == null)
            {
                return true; // No order
            }
            if (order.delivered && (order.consumable?.Spawned == false || order.consumable?.ParentHolder == patron.inventory))
            {
                // Order not spawned? Already eaten it, or something happened to it
                // Clear order
                Log.Warning($"{patron.NameShortColored}'s food is gone. Already eaten?");
                CancelOrder(order);
                return false;
            }

            return true;
        }
    }
}
