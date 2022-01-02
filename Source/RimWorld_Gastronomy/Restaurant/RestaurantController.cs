using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CashRegister;
using CashRegister.TableTops;
using Gastronomy.Dining;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace Gastronomy.Restaurant
{
	public class RestaurantController : MapComponent
	{
		// TODO: Make all these settings not part of a map component, so we can have multiple restaurants
		public RestaurantController(Map map) : base(map) { }

		[NotNull] public readonly List<DiningSpot> diningSpots = new List<DiningSpot>();
		[NotNull] public IList<Building_CashRegister> Registers { get; private set; } = Array.Empty<Building_CashRegister>();

		[NotNull] private readonly List<Pawn> spawnedDiningPawnsResult = new List<Pawn>();
		[NotNull] private readonly List<Pawn> spawnedActiveStaffResult = new List<Pawn>();
		private RestaurantMenu menu;
		private RestaurantOrders orders;
		private RestaurantDebt debts;
		private RestaurantStock stock;

		private int day;

		public bool IsOpenedRightNow => openForBusiness && AnyRegisterOpen;

		private bool AnyRegisterOpen => Registers.Any(r => r?.IsActive == true);

		public bool openForBusiness = true;

		public bool allowGuests = true;
		public bool allowColonists = true;
		public bool allowPrisoners = false;
		public bool allowSlaves = false;

		public float guestPricePercentage = 1;

		public event Action onNextDay;

		public int Seats => diningSpots.Sum(s => s.GetMaxSeats());
		[NotNull] public ReadOnlyCollection<Pawn> Patrons => SpawnedDiningPawns.AsReadOnly();
		[NotNull] public RestaurantMenu Menu => menu;
		[NotNull] public RestaurantOrders Orders => orders;
		[NotNull] public RestaurantDebt Debts => debts;
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
		[NotNull] public List<Pawn> ActiveStaff
		{
			get
			{
				spawnedActiveStaffResult.Clear();
				var activeShifts = Registers.SelectMany(r => r.shifts.Where(s => s.IsActive));
				spawnedActiveStaffResult.AddRange(activeShifts.SelectMany(s => s.assigned).Where(p => p.MapHeld == map));
				return spawnedActiveStaffResult;
			}
		}


		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref openForBusiness, "openForBusiness", true);
			Scribe_Values.Look(ref allowGuests, "allowGuests", true);
			Scribe_Values.Look(ref allowColonists, "allowColonists", true);
			Scribe_Values.Look(ref allowPrisoners, "allowPrisoners", false);
			Scribe_Values.Look(ref allowSlaves, "allowSlaves", false);
			Scribe_Values.Look(ref guestPricePercentage, "guestPricePercentage", 1);
			Scribe_Values.Look(ref day, "day");
			Scribe_Deep.Look(ref menu, "menu");
			Scribe_Deep.Look(ref stock, "stock", this);
			Scribe_Deep.Look(ref orders, "orders", this);
			Scribe_Deep.Look(ref debts, "debts", this);
			InitDeepFieldsInitial();
		}

		private void InitDeepFieldsInitial()
		{
			menu ??= new RestaurantMenu();
			orders ??= new RestaurantOrders(this);
			debts ??= new RestaurantDebt(this);
			stock ??= new RestaurantStock(this);
		}

		public override void MapGenerated()
		{
			InitDeepFieldsInitial();
		}

		public override void FinalizeInit()
		{
			base.FinalizeInit();

			InitDeepFieldsInitial();
			diningSpots.Clear();
			diningSpots.AddRange(DiningUtility.GetAllDiningSpots(map));
			stock.RareTick();
			orders.RareTick();
			debts.RareTick();

			TableTop_Events.onAnyBuildingSpawned.AddListener(RefreshRegisters);
			TableTop_Events.onAnyBuildingDespawned.AddListener(RefreshRegisters);
			RefreshRegisters(null, map);
		}

		private void RefreshRegisters(Building building, Map map)
		{
			// Only check for local map
			if (map == this.map)
			{
				// Remove old listeners
                foreach (var register in Registers)
                {
                    register.onRadiusChanged.RemoveListener(OnRegisterRadiusChanged);
                }

				Registers = RegisterUtility.GetRegisters(map);

				// Add new listeners
                foreach (var register in Registers)
                {
                    register.onRadiusChanged.AddListener(OnRegisterRadiusChanged);
                }
			}
		}

        private void OnRegisterRadiusChanged(Building_CashRegister register)
        {
            RefreshRegisters(null, register.Map);
        }

        public override void MapComponentTick()
		{
			RestaurantUtility.OnTick();
			// Don't tick everything at once
			if ((GenTicks.TicksGame + map.uniqueID) % 500 == 0) stock.RareTick();
			if ((GenTicks.TicksGame + map.uniqueID) % 500 == 250) orders.RareTick();
			//Log.Message($"Stock: {stock.Select(s => s.def.label).ToCommaList(true)}");
			if ((GenTicks.TicksGame + map.uniqueID) % 500 == 300) RareTick();
		}

		private void RareTick()
		{
			if (GenDate.DaysPassed > day && GenLocalDate.HourInteger(map) == 0) OnNextDay(GenDate.DaysPassed);
		}

		private void OnNextDay(int today)
		{
			day = today;
			onNextDay?.Invoke();
		}

		public bool MayDineHere(Pawn pawn)
		{
			//var isPrisoner = pawn.IsPrisoner;
			var isGuest = pawn.IsGuest();
			var isColonist = pawn.IsColonist;
			var isPrisoner = pawn.IsPrisoner;
			var isSlave = pawn.IsSlave;

			if (!allowColonists && isColonist) return false;
			if (!allowGuests && isGuest) return false;
			if (!allowPrisoners && isPrisoner) return false;
			if (!allowSlaves && isSlave) return false;
			
			return true;
		}

		public bool HasToWork(Pawn pawn)
		{
			if (!openForBusiness) return false;
			return Registers.Any(r => r?.HasToWork(pawn) == true);
		}

		public void RescanDiningSpots()
		{
			diningSpots.Clear();
			diningSpots.AddRange(DiningUtility.GetAllDiningSpots(map));
		}
	}
}
