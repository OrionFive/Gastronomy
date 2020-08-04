using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using JetBrains.Annotations;
using Restaurant.Dining;
using Restaurant.Timetable;
using Verse;

namespace Restaurant
{
	public class RestaurantController : MapComponent
	{
		public RestaurantController(Map map) : base(map) { }

		[NotNull] public readonly List<DiningSpot> diningSpots = new List<DiningSpot>();
		[NotNull] private readonly List<Pawn> spawnedDiningPawnsResult = new List<Pawn>();
		private RestaurantMenu menu;
		private RestaurantOrders orders;
		private RestaurantStock stock;

		public bool IsOpenedRightNow => openForBusiness && timetableOpen.CurrentAssignment(map);
		public bool openForBusiness = true;

		public TimetableBool timetableOpen;

		public int Seats => diningSpots.Sum(s => s.GetMaxSeats());
		[NotNull] public ReadOnlyCollection<Pawn> Patrons => SpawnedDiningPawns.AsReadOnly();
		[NotNull] public RestaurantMenu Menu => menu;
		[NotNull] public RestaurantOrders Orders => orders;
		[NotNull] public RestaurantStock Stock => stock;
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
			Scribe_Deep.Look(ref timetableOpen, "timetableOpen");
			Scribe_Deep.Look(ref menu, "menu");
			Scribe_Deep.Look(ref stock, "stock", this);
			Scribe_Deep.Look(ref orders, "orders", this);
			InitDeepFieldsInitial();
		}

		private void InitDeepFieldsInitial()
		{
			if (timetableOpen == null) timetableOpen = new TimetableBool();
			if (menu == null) menu = new RestaurantMenu();
			if (orders == null) orders = new RestaurantOrders(this);
			if (stock == null) stock = new RestaurantStock(this);
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

		public override void MapComponentTick()
		{
			// Don't tick everything at once
			if ((GenTicks.TicksGame + map.uniqueID) % 500 == 0) stock.RareTick();
			if ((GenTicks.TicksGame + map.uniqueID) % 500 == 250) orders.RareTick();
			//Log.Message($"Stock: {stock.Select(s => s.def.label).ToCommaList(true)}");
		}
	}
}
