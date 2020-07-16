using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Restaurant.TableTops
{
    public class ITab_Register : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(420f, 480f);
        private bool showSettings = true;
        private bool showRadius = false;
        private bool showStats = true;

        public ITab_Register()
        {
            size = WinSize;
            labelKey = "TabRegister";
        }

        protected Building_CashRegister Register => (Building_CashRegister) SelThing;

        protected override void FillTab()
        {
            var rect = new Rect(0f, 16f, WinSize.x, WinSize.y).ContractedBy(10f);

            if (showSettings)
            {
                var smallRect = new Rect(rect) {height = 1 * 24};
                rect.yMin += smallRect.height + 10;

                DrawSettings(smallRect);
            }

            if (showRadius)
            {
                var smallRect = new Rect(rect) {height = 50};
                rect.yMin += smallRect.height + 10;

                DrawRadius(smallRect);
            }

            if (showStats)
            {
                var smallRect = new Rect(rect) {height = 5 * 24 + 20};
                rect.yMin += smallRect.height + 10;

                DrawStats(smallRect);
            }
        }

        private void DrawSettings(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            {
                listing.CheckboxLabeled("Opened", ref Register.settings.openForBusiness, "When checked, patrons can make orders during business hours."); // TODO: localize

                // TODO: Timetable
            }
            listing.End();
        }

        private void DrawRadius(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            {
                string strRadius = "TabRegisterRadius".Translate().Truncate(rect.width * 0.6f);
                string strRadiusValue = Register.radius >= 999f ? "Unlimited".TranslateSimple().Truncate(rect.width * 0.3f) : Register.radius.ToString("F0");
                listing.Label(strRadius + ": " + strRadiusValue);
                float oldValue = Register.radius;
                Register.radius = listing.Slider(Register.radius, 3f, 100f);
                if (Register.radius >= 100f)
                {
                    Register.radius = 999f;
                }

                if (Register.radius != oldValue)
                {
                    Register.ScanDiningSpots();
                }
            }
            listing.End();
        }

        private void DrawStats(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(1f, 1f, 1f, 0.08f));
            rect = rect.ContractedBy(10);
            var listing = new Listing_Standard();
            listing.Begin(rect);
            {
                var patrons = Register.settings.Patrons;
                var orders = Register.settings.Orders;
                var stock = Register.settings.Stock;
                var ordersForServing = Register.settings.AvailableOrdersForServing.ToArray();
                var ordersForCooking = Register.settings.AvailableOrdersForCooking.ToArray();

                listing.LabelDouble("Seats:", Register.settings.Seats.ToString()); // TODO: localize
                listing.LabelDouble("Patrons:", patrons.Count.ToString(), patrons.Select(p=>p.LabelShort).ToCommaList(true)); // TODO: localize
                listing.LabelDouble("Total orders:", orders.Count.ToString(), orders.Select(GetOrderLabel).ToCommaList(true)); // TODO: localize
                listing.LabelDouble("Orders need serving:", ordersForServing.Count().ToString(), ordersForServing.Select(GetOrderLabel).ToCommaList(true)); // TODO: localize
                listing.LabelDouble("Orders need cooking:", ordersForCooking.Count().ToString(), ordersForCooking.Select(GetOrderLabel).ToCommaList(true)); // TODO: localize
                listing.LabelDouble("Stocked meals:", stock.Sum(s=>s.stackCount).ToString(), stock.Select(s=>s.def).Distinct().Select(s=>s.label).ToCommaList()); // TODO: localize
            }
            listing.End();
        }

        private static string GetOrderLabel(Order order)
        {
            return $"{order.patron.NameShortColored} ({order.consumableDef.label})";
        }

        public override void TabUpdate()
        {
            Register.DrawGizmos();
        }
    }
}
