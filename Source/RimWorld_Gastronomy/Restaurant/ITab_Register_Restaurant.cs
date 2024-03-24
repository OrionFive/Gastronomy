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
        private bool showStats = true;
        private RestaurantController restaurant;
        private readonly ThingFilterUI.UIState menuFilterState = new ThingFilterUI.UIState();

        public ITab_Register_Restaurant() : base(new Vector2(800, 504))
        {
            labelKey = "TabRegisterRestaurant";
        }

        public override bool IsVisible => true;
        //{
        //    get
        //    {
        //        restaurant = Register.GetRestaurant();
        //        return restaurant != null;
        //    }
        //}

        public override void FillTab()
        {
            restaurant = Register.GetRestaurant();
            restaurant ??= Register.GetAllRestaurants().First();
            var fullRect = new Rect(0, 16, size.x, size.y - 16);
            var rectLeft = fullRect.LeftHalf().ContractedBy(10f);
            var rectRight = fullRect.RightHalf().ContractedBy(10f);

            DrawLeft(rectLeft);
            DrawRight(rectRight);
        }

        private void DrawRight(Rect rect)
        {
            // Menu
            {
                var menuRect = new Rect(rect);
                //menuRect.yMax -= 36;

                restaurant.Menu.GetMenuFilters(out var filter, out var parentFilter);
                ThingFilterUI.DoThingFilterConfigWindow(menuRect, menuFilterState, filter, parentFilter, 1, null, HiddenSpecialThingFilters(), true);
            }
        }

        private void DrawLeft(Rect rect)
        {
            var topBar = rect.TopPartPixels(24);
            DrawRestaurantSelection(ref topBar);
            rect.yMin += topBar.height;

            if (showSettings)
            {
                var smallRect = new Rect(rect);

                DrawSettings(ref smallRect);
                rect.yMin += smallRect.height + 10;
            }

            if (showStats)
            {
                var smallRect = new Rect(rect);

                DrawStats(ref smallRect);
                rect.yMin += smallRect.height + 10;
            }
        }

        private void DrawRestaurantSelection(ref Rect rect)
        {
            restaurant ??= Register.GetRestaurant();

            var restaurants = restaurant.GetRestaurantsManager();
            var rectSelection = rect.LeftHalf();

            // Select
            if (Widgets.ButtonText(rectSelection, restaurant.Name.CapitalizeFirst()))
            {
                var list = new List<FloatMenuOption>();
                foreach (var controllerOption in restaurants.restaurants)
                {
                    list.Add(new FloatMenuOption(controllerOption.Name.CapitalizeFirst(), delegate
                    {
                        SetRestaurant(controllerOption);
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(list));
            }
            TooltipHandler.TipRegion(rectSelection, "RestaurantTooltipSelect".Translate());

            var widgetRow = new WidgetRow(rect.xMax, rect.y, UIDirection.LeftThenDown);

            // Remove
            if (restaurants.restaurants.Count > 1)
                if (widgetRow.ButtonIcon(TexButton.Minus, "RestaurantTooltipRemove".Translate()))
                {
                    Find.WindowStack.Add(new Dialog_Confirm("RestaurantDialogConfirmRemove".Translate(restaurant.Name),
                        () =>
                        {
                            restaurants.DeleteRestaurant(restaurant);
                            SetRestaurant(restaurants.restaurants.Last());
                        }));
                }
            // Add
            if (widgetRow.ButtonIcon(TexButton.Plus, "RestaurantTooltipAdd".Translate()))
            {
                SetRestaurant(restaurants.AddRestaurant());
            }
            // Rename
            if (widgetRow.ButtonIcon(TexButton.Rename, "RestaurantTooltipRename".Translate()))
            {
                Find.WindowStack.Add(new Dialog_RenameRestaurant(restaurant));
            }
        }

        private void SetRestaurant(RestaurantController newRestaurant)
        {
            newRestaurant.LinkRegister(Register);
            restaurant = newRestaurant;
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
                if (ModsConfig.IdeologyActive)
                {
                    listing.CheckboxLabeled("TabRegisterSlaves".Translate(), ref restaurant.allowSlaves, "TabRegisterSlavesTooltip".Translate());
                }

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
            var value = Widgets.HorizontalSlider(rect, restaurant.guestPricePercentage, 0, 5, false, label, null, null, 0.1f);
            TooltipHandler.TipRegionByKey(rect, "TabRegisterGuestPriceTooltip");

            if (value != restaurant.guestPricePercentage)
            {
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
                restaurant.guestPricePercentage = value;
            }
        }

        private void DrawStats(ref Rect rect)
        {
            const int listingItems = 11;
            rect.height = listingItems * 24 + 20;

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

            var itemsDrawn = stock.Take(16).ToList(); // Max 16 items, or multiple rows are needed (which push other text out of view)

            var iconCols = Mathf.FloorToInt(rect.width / iconSize);
            var iconRows = Mathf.CeilToInt((float)itemsDrawn.Count / iconCols);
            var height = iconRows * iconSize;

            var rectIcons = listing.GetRect(height);
            rectIcon.x = rectIcons.x;
            rectIcon.y = rectIcons.y;

            // Icons for each type of stock
            int col = 0;
            foreach (var group in itemsDrawn)
            {
                if (group.Value.def == null) continue;
                if (group.Value.items.Count == 0) continue;

                // Icon
                var value = group.Value.def.GetPrice(restaurant).ToStringMoney();
                DrawDefIcon(rectIcon, group.Value.def, $"{group.Value.items.Sum(item => item.stackCount)}x {group.Value.def.LabelCap} ({value})");
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
            return pawn.workSettings.WorkIsActive(WaitingDefOf.Gastronomy_Waiting);
        }
    }
}
