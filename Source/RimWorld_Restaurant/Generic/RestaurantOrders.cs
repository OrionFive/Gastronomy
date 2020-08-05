using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;
using Verse;

namespace Restaurant
{
    public class RestaurantOrders : IExposable
    {
        private List<Order> orders = new List<Order>();

        [NotNull] public ReadOnlyCollection<Order> AllOrders => orders.AsReadOnly();
        [NotNull] public IEnumerable<Order> AvailableOrdersForServing => orders.Where(o => !o.delivered && Stock.AllStock.Any(s => s.def == o.consumableDef));
        [NotNull] public IEnumerable<Order> AvailableOrdersForCooking => orders.Where(o => !o.delivered && Stock.AllStock.All(s => s.def != o.consumableDef));
        [NotNull] private RestaurantStock Stock => Restaurant.Stock;
        [NotNull] private RestaurantMenu Menu => Restaurant.Menu;
        [NotNull] private RestaurantController Restaurant { get; }

        public RestaurantOrders([NotNull] RestaurantController restaurant)
        {
            Restaurant = restaurant;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref orders, "orders", LookMode.Deep);
            if(orders == null) orders = new List<Order>();
        }

        public void RareTick()
        {
            orders.RemoveAll(o => !o.patron.Spawned || o.patron.Dead || CantBeOrdered(o));
        }

        private bool CantBeOrdered(Order o)
        {
            if (o.delivered) return false;
            if (o.consumable != null) return false;
            return !Menu.IsOnMenu(o.consumableDef);
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
            var available = Stock.AllStock.Where(item => item.def == consumableDef).Sum(item => item.stackCount);
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

            orders.Add(new Order {consumableDef = consumableDef, patron = patron, hasToBeMade = available <= ordered});
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
            if (order != null)
                Log.Message($"Found an order of {order.consumableDef.label} for {patron.NameShortColored}. hasToBeMade? {order.hasToBeMade} IsBeingDelivered? {IsBeingDelivered(order)} hasBeenDelivered? {order.delivered}");
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
            //Log.Message($"Consumable found: {order.consumable.Label} at {order.consumable.Position}");
            var reserver = Restaurant.map.reservationManager.FirstRespectedReserver(order.consumable, waiter ?? order.patron);
            if (reserver == null) return false;
            return reserver != waiter;
        }

        /// <summary>
        /// Check if the order is somehow broken.
        /// </summary>
        /// <returns>True of order is fine, false if it's not.</returns>
        public bool CheckOrderOfWaitingPawn(Pawn patron)
        {
            var order = orders.FirstOrDefault(o => o.patron == patron);
            if (order != null && order.delivered && order.consumable?.Spawned == false)
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
