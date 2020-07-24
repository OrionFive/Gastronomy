using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using TimetableUtility = Restaurant.Timetable.TimetableUtility;

namespace Restaurant.TableTops
{
    public class ITab_Register : ITab
    {
        private static readonly Vector2 WinSize = new Vector2(600f, 480f);
        private bool showSettings = true;
        private bool showRadius = false;
        private bool showStats = true;
        private Vector2 menuScrollPosition;

        public ITab_Register()
        {
            size = WinSize;
            labelKey = "TabRegister";
        }

        protected Building_CashRegister Register => (Building_CashRegister) SelThing;

        protected override void FillTab()
        {
            var rectTop = new Rect(0, 16, WinSize.x, 40).ContractedBy(10);
            var rectLeft = new Rect(0f, 40+20+16, WinSize.x/2, WinSize.y).ContractedBy(10f);
            var rectRight = new Rect(WinSize.x/2, 40+20+16, WinSize.x/2, WinSize.y).ContractedBy(10f);

            DrawTop(rectTop);
            DrawLeft(rectLeft);
            DrawRight(rectRight);
        }

        private void DrawTop(Rect rect)
        {
            TimetableUtility.DoHeader(new Rect(rect) {height = 24});
            rect.yMin += 24;
            TimetableUtility.DoCell(new Rect(rect) {height = 30}, Register.settings.timetableOpen);
        }

        private void DrawRight(Rect rect)
        {
            // Menu
            {
                var menuRect = new Rect(rect);
                menuRect.yMax += 20;
                ThingFilterUI.DoThingFilterConfigWindow(menuRect, ref menuScrollPosition, Register.settings.menuFilter, Register.settings.menuGlobalFilter, 
                    1, null, HiddenSpecialThingFilters(), true);
            }
        }

        private void DrawLeft(Rect rect)
        {
            if (showSettings)
            {
                var smallRect = new Rect(rect) {height = 30};
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
                listing.CheckboxLabeled("TabRegisterOpened".Translate(), ref Register.settings.openForBusiness, "TabRegisterOpenedTooltip".Translate());
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
                var stock = Register.settings.Stock.ToArray();
                var ordersForServing = Register.settings.AvailableOrdersForServing.ToArray();
                var ordersForCooking = Register.settings.AvailableOrdersForCooking.ToArray();

                listing.LabelDouble("TabRegisterSeats".Translate(), Register.settings.Seats.ToString());
                listing.LabelDouble("TabRegisterPatrons".Translate(), patrons.Count.ToString(), patrons.Select(p=>p.LabelShort).ToCommaList());
                listing.LabelDouble("TabRegisterTotalOrders".Translate(), orders.Count.ToString(), orders.Select(GetOrderLabel).ToCommaList());
                listing.LabelDouble("TabRegisterNeedsServing".Translate(), ordersForServing.Count().ToString(), ordersForServing.Select(GetOrderLabel).ToCommaList());
                listing.LabelDouble("TabRegisterNeedsCooking".Translate(), ordersForCooking.Count().ToString(), ordersForCooking.Select(GetOrderLabel).ToCommaList());
                listing.LabelDouble("TabRegisterStocked".Translate(), stock.Sum(s=>s.stackCount).ToString(), stock.Select(s=>s.def).Distinct().Select(s=>s.label).ToCommaList());
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

        private static IEnumerable<SpecialThingFilterDef> HiddenSpecialThingFilters()
        {
            yield return SpecialThingFilterDefOf.AllowFresh;
        }
    }
}
