using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CashRegister;
using Gastronomy.Waiting;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Gastronomy.Restaurant
{
    public class ITab_Register_Restaurant : ITab_Register
    {
        private bool showSettings = true;
        private bool showRadius = false;
        private bool showStats = true;
        private Vector2 menuScrollPosition;
        private RestaurantController restaurant;

        public ITab_Register_Restaurant() : base(new Vector2(800, 500))
        {
            labelKey = "TabRegisterRestaurant";
        }

        public override bool IsVisible
        {
            get
            {
                restaurant ??= SelThing?.GetRestaurant();
                return restaurant != null;
            }
        }

        protected override void FillTab()
        {
            var rectLeft = new Rect(0f, 16, size.x/2, size.y).ContractedBy(10f);
            var rectRight = new Rect(size.x/2, 0, size.x/2, size.y).ContractedBy(10f);

            DrawLeft(rectLeft);
            DrawRight(rectRight);
        }

        private void DrawRight(Rect rect)
        {
            // Menu
            {
                var menuRect = new Rect(rect);
                menuRect.yMax -= 36;

                restaurant.Menu.GetMenuFilters(out var filter, out var parentFilter);
                ThingFilterUI.DoThingFilterConfigWindow(menuRect, ref menuScrollPosition, filter, parentFilter, 
                    1, null, HiddenSpecialThingFilters(), true);
            }
        }

        private void DrawLeft(Rect rect)
        {
            if (showSettings)
            {
                var smallRect = new Rect(rect);

                DrawSettings(ref smallRect);
                rect.yMin += smallRect.height + 10;
            }

            if (showRadius)
            {
                var smallRect = new Rect(rect) {height = 50};

                DrawRadius(smallRect);
                rect.yMin += smallRect.height + 10;
            }

            if (showStats)
            {
                var smallRect = new Rect(rect);

                DrawStats(ref smallRect);
                rect.yMin += smallRect.height + 10;
            }
        }

        private void DrawSettings(ref Rect rect)
        {
            const int ListingItems = 5;
            rect.height = 30 * ListingItems;

            var listing = new Listing_Standard();
            listing.Begin(rect);
            {
                listing.CheckboxLabeled("TabRegisterOpened".Translate(), ref restaurant.openForBusiness, "TabRegisterOpenedTooltip".Translate());
                listing.CheckboxLabeled("TabRegisterGuests".Translate(), ref restaurant.allowGuests, "TabRegisterGuestsTooltip".Translate());
                listing.CheckboxLabeled("TabRegisterColonists".Translate(), ref restaurant.allowColonists, "TabRegisterColonistsTooltip".Translate());
                listing.CheckboxLabeled("TabRegisterPrisoners".Translate(), ref restaurant.allowPrisoners, "TabRegisterPrisonersTooltip".Translate());

                DrawPrice(listing.GetRect(22));

                //bool guestsPay = restaurant.guestPricePercentage > 0;
                //listing.CheckboxLabeled("TabRegisterGuestsHaveToPay".Translate(), ref guestsPay, "TabRegisterGuestHaveToPayTooltip".Translate(MarketPriceFactor.ToStringPercent()));
                //restaurant.guestPricePercentage = guestsPay ? MarketPriceFactor : 0;
            }
            listing.End();
        }

        private void DrawPrice(Rect rect)
        {
            // Price
            var price = restaurant.guestPricePercentage <= 0? (string)"TabRegisterGuestPriceFree".Translate() : restaurant.guestPricePercentage.ToStringPercentEmptyZero();
            var label = "TabRegisterGuestPrice".Translate(price);
            var value = Widgets.HorizontalSlider(rect, restaurant.guestPricePercentage, 0, 5, false, label);
            TooltipHandler.TipRegionByKey(rect, "TabRegisterGuestPriceTooltip");

            if (value != restaurant.guestPricePercentage)
            {
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
                restaurant.guestPricePercentage = value;
            }
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
                    restaurant.RescanDiningSpots();
                }
            }
            listing.End();
        }

        private void DrawStats(ref Rect rect)
        {
            const int ListingItems = 11;
            rect.height = ListingItems * 24 + 20;

            Widgets.DrawBoxSolid(rect, new Color(1f, 1f, 1f, 0.08f));
            rect = rect.ContractedBy(10);
            var listing = new Listing_Standard();
            listing.Begin(rect);
            {
                var activeStaff = restaurant.ActiveStaff;
                var patrons = restaurant.Patrons;
                var orders = restaurant.Orders.AllOrders;
                var debts = restaurant.Debts.AllDebts;
                var stock = restaurant.Stock.AllStock;
                var ordersForServing = restaurant.Orders.AvailableOrdersForServing.ToArray();
                var ordersForCooking = restaurant.Orders.AvailableOrdersForCooking.ToArray();

                listing.LabelDouble("TabRegisterActiveStaff".Translate(), activeStaff.Count.ToString(), activeStaff.Select(p => p.LabelShort).ToCommaList());
                listing.LabelDouble("TabRegisterSeats".Translate(), restaurant.Seats.ToString());
                listing.LabelDouble("TabRegisterPatrons".Translate(), patrons.Count.ToString(), patrons.Select(p=>p.LabelShort).ToCommaList());
                DrawOrders(listing, "TabRegisterTotalOrders".Translate(), orders);
                DrawOrders(listing, "TabRegisterNeedsServing".Translate(), ordersForServing);
                DrawOrders(listing, "TabRegisterNeedsCooking".Translate(), ordersForCooking);
                DrawStock(listing, "TabRegisterStocked".Translate(), stock, restaurant);
                listing.LabelDouble("TabRegisterEarnedYesterday".Translate(), restaurant.Debts.incomeYesterday.ToStringMoney());
                listing.LabelDouble("TabRegisterEarnedToday".Translate(), restaurant.Debts.incomeToday.ToStringMoney());
                DrawDebts(listing, "TabRegisterDebts".Translate(), debts);

                //listing.LabelDouble("TabRegisterStocked".Translate(), stock.Sum(s=>s.stackCount).ToString(), stock.Select(s=>s.def).Distinct().Select(s=>s.label).ToCommaList());
            }
            listing.End();
        }

        private static void DrawOrders(Listing listing, TaggedString label, [NotNull] IReadOnlyCollection<Order> orders)
        {
            // Label
            var rect = CustomLabelDouble(listing, label, $"{orders.Count}: ", out var countSize);

            var grouped = orders.GroupBy(o => o.consumableDef);

            var rectImage  = rect.RightHalf();
            rectImage.xMin += countSize.x;
            rectImage.width = rectImage.height = countSize.y;

            // Icons for each type of order
            foreach (var group in grouped)
            {
                if (group.Key == null) continue;
                // A list of the patrons for the order
                DrawDefIcon(rectImage, group.Key, $"{group.Key.LabelCap}: {group.Select(o => o.patron.Name.ToStringShort).ToCommaList()}");
                rectImage.x += 2 + rectImage.width;
               
                // Will the next one fit?
                if (rectImage.xMax > rect.xMax) break;
            }
            listing.Gap(listing.verticalSpacing);
        }

        private static void DrawStockExpanded(Listing listing, TaggedString label, [NotNull] IReadOnlyCollection<Thing> stock)
        {
            // Label
            var rect = CustomLabelDouble(listing, label, $"{stock.Count}:", out var countSize);

            var grouped = stock.GroupBy(s => s.def);

            var rectImage  = rect.RightHalf();
            rectImage.xMin += countSize.x;
            rectImage.height = countSize.y;

            // Icons for each type of stock
            foreach (var group in grouped)
            {
                if (group.Key == null) continue;
                // Amount label
                string amountText = $" {group.Count()}x";
                var amountSize = Text.CalcSize(amountText);
                rectImage.width = amountSize.x;

                // Will it fit?
                if (rectImage.xMax + rectImage.height > rect.xMax) break;

                // Draw label
                Widgets.Label(rectImage, amountText);
                rectImage.x += rectImage.width;
                // Icon
                rectImage.width = rectImage.height;
                DrawDefIcon(rectImage, group.Key, group.Key.LabelCap);
                rectImage.x += rectImage.width;

                // Will the next one fit?
                if (rectImage.xMax > rect.xMax) break;
            }
            listing.Gap(listing.verticalSpacing);
        }

        private static void DrawStock(Listing listing, TaggedString label, [NotNull] IReadOnlyDictionary<ThingDef, RestaurantStock.Stock> stock, RestaurantController restaurant)
        {
            // Label
            var rect = CustomLabelDouble(listing, label, $"{stock.Values.Sum(pair => pair.items.Sum(item=>item.stackCount))}", out var countSize);

            var rectIcon  = rect.RightHalf();
            //rectIcon.xMin += countSize.x;
            var iconSize = rectIcon.width = rectIcon.height = countSize.y;
            
            var iconCols = Mathf.FloorToInt(rect.width / iconSize);
            var iconRows = Mathf.CeilToInt((float) stock.Count / iconCols);
            var height = iconRows * iconSize;

            var rectIcons = listing.GetRect(height);
            rectIcon.x = rectIcons.x;
            rectIcon.y = rectIcons.y;

            // Icons for each type of stock
            int col = 0;
            foreach (var group in stock.Values)
            {
                if (group.def == null) continue;
                if (group.items.Count == 0) continue;

                // Icon
                var value = group.def.GetPrice(restaurant).ToStringMoney();
                DrawDefIcon(rectIcon, group.def, $"{group.items.Sum(item => item.stackCount)}x {group.def.LabelCap} ({value})");
                rectIcon.x += iconSize;

                col++;
                if (col == iconCols)
                {
                    col = 0;
                    rectIcon.x = rectIcons.x;
                }
            }
            listing.Gap(listing.verticalSpacing);
        }

        private static void DrawDebts(Listing listing, TaggedString label, [NotNull] ReadOnlyCollection<Debt> debts)
        {
            // Label
            var rect = CustomLabelDouble(listing, label, $"{debts.Sum(debt => debt.amount).ToStringMoney()}", out var countSize);

            var rectIcon  = rect.RightHalf();
            //rectIcon.xMin += countSize.x;
            var iconSize = rectIcon.width = rectIcon.height = countSize.y;
            
            var iconCols = Mathf.FloorToInt(rect.width / iconSize);
            var iconRows = Mathf.CeilToInt((float) debts.Count / iconCols);
            var height = iconRows * iconSize;

            var rectIcons = listing.GetRect(height);
            rectIcon.x = rectIcons.x;
            rectIcon.y = rectIcons.y;

            // Icons for each type of stock
            int col = 0;
            foreach (var debt in debts)
            {
                if (debt.patron == null) continue;
                if (debt.amount <= 0) continue;

                // Icon
                DrawDefIcon(rectIcon, ThingDefOf.Silver, "TabRegisterDebtTooltip".Translate(debt.patron.Name.ToStringFull, debt.amount.ToStringMoney()), () => debt.amount = 0);
                rectIcon.x += iconSize;

                col++;
                if (col == iconCols)
                {
                    col = 0;
                    rectIcon.x = rectIcons.x;
                }
            }

            listing.Gap(listing.verticalSpacing);
        }

        private static Rect CustomLabelDouble(Listing listing, TaggedString labelLeft, TaggedString stringRight, out Vector2 sizeRight)
        {
            sizeRight = Text.CalcSize(stringRight);
            Rect rect = listing.GetRect(Mathf.Max(Text.CalcHeight(labelLeft, listing.ColumnWidth / 2f), sizeRight.y));
            Widgets.Label(rect.LeftHalf(), labelLeft);
            Widgets.Label(rect.RightHalf(), stringRight);
            return rect;
        }

        private static void DrawDefIcon(Rect rect, ThingDef def, string tooltip = null, Action onClicked = null)
        {
            if (tooltip != null)
            {
                TooltipHandler.TipRegion(rect, tooltip);
                Widgets.DrawHighlightIfMouseover(rect);
            }

            GUI.DrawTexture(rect, def.uiIcon);
            if (onClicked != null && Widgets.ButtonInvisible(rect)) onClicked.Invoke();
        }

        private static IEnumerable<SpecialThingFilterDef> HiddenSpecialThingFilters()
        {
            yield return SpecialThingFilterDefOf.AllowFresh;
        }

        public override bool CanAssignToShift(Pawn pawn)
        {
            return pawn.workSettings.WorkIsActive(WaitingUtility.waitDef);
        }
    }
}
