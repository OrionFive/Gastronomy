using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;
using Restaurant.Dining;
using Restaurant.Timetable;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Restaurant
{
	public class RestaurantController : MapComponent
	{
		public RestaurantController(Map map) : base(map) { }

		[NotNull] public readonly List<DiningSpot> diningSpots = new List<DiningSpot>();
		[NotNull] private readonly List<Pawn> spawnedDiningPawnsResult = new List<Pawn>();
		[NotNull] private readonly List<Thing> stock = new List<Thing>();
		private RestaurantMenu menu;
		private RestaurantOrders orders;

		private int lastStockUpdateTick;

		public bool IsOpenedRightNow => openForBusiness && timetableOpen.CurrentAssignment(map);
		public bool openForBusiness = true;

		public TimetableBool timetableOpen;

		public int Seats => diningSpots.Sum(s => s.GetMaxSeats());
		[NotNull] public ReadOnlyCollection<Pawn> Patrons => SpawnedDiningPawns.AsReadOnly();
		[NotNull] public IEnumerable<Thing> Stock => stock.AsReadOnly();
		[NotNull] public RestaurantMenu Menu => menu;
		[NotNull] public RestaurantOrders Orders => orders;


		[NotNull] public List<Pawn> SpawnedDiningPawns
		{
			get
			{
				spawnedDiningPawnsResult.Clear();
				spawnedDiningPawnsResult.AddRange(map.mapPawns.AllPawnsSpawned.Where(pawn => pawn.jobs?.curDriver is JobDriver_Dine));
				return spawnedDiningPawnsResult;
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref openForBusiness, "openForBusiness", true);
			Scribe_Deep.Look(ref menu, "menu");
			Scribe_Deep.Look(ref timetableOpen, "timetableOpen");
			Scribe_Deep.Look(ref orders, "orders", this);
			InitDeepFieldsInitial();
		}

		private void InitDeepFieldsInitial()
		{
			if (timetableOpen == null) timetableOpen = new TimetableBool();
			if (orders == null) orders = new RestaurantOrders(this);
			if (menu == null) menu = new RestaurantMenu();
		}

		public override void MapGenerated()
		{
			InitDeepFieldsInitial();
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
			var best = stock.Select(item => item.def).Distinct().Where(def => WillConsume(pawn, allowDrug, def)).MaxBy(def => FoodOptimality(pawn, def));
			//Log.Message($"{pawn.NameShortColored}: GetBestFoodFor: {best?.label}");
			return best;
		}

		public ThingDef GetRandomFoodTypeFor([NotNull] Pawn pawn, bool allowDrug)
		{
			var random = stock.Select(item => item.def).Distinct().Where(def => WillConsume(pawn, allowDrug, def)).RandomElementByWeight(def => FoodOptimality(pawn, def));
			//Log.Message($"{pawn.NameShortColored}: GetBestFoodFor: {best?.label}");
			return random;
		}

		private float FoodOptimality(Pawn pawn, ThingDef def)
		{
			// Optimality can be negative
			Log.Message($"{pawn.NameShortColored} - {def.LabelCap}");
			var dummyFoodSource = stock[0]; // Can be null again once erdelf fixes the patch
			return Mathf.Max(0, FoodUtility.FoodOptimality(pawn, dummyFoodSource, def, 0));
		}

		private static bool WillConsume(Pawn pawn, bool allowDrug, ThingDef s)
		{
			return (allowDrug || !s.IsDrug) && pawn.WillEat(s);
		}

		public override void MapComponentTick()
		{
			if (GenTicks.TicksGame < lastStockUpdateTick + 500) return;
			lastStockUpdateTick = GenTicks.TicksGame;
			stock.Clear();
			stock.AddRange(map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSource).Where(t => t.def.IsIngestible && menu.IsOnMenu(t)));
			orders.RareTick();	
			//Log.Message($"Stock: {stock.Select(s => s.def.label).ToCommaList(true)}");
		}

		public Thing GetServableThing(Order order, Pawn pawn)
		{
			return Stock.Where(o => o.Spawned && o.def == order.consumableDef).OrderBy(o => pawn.Position.DistanceToSquared(o.Position)).FirstOrDefault(o => pawn.CanReserveAndReach(o, PathEndMode.Touch, Danger.None, o.stackCount, 1));
		}
	}
}
