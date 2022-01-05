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
    public class RestaurantController : IExposable
    {
        [NotNull] public readonly HashSet<DiningSpot> diningSpots = new HashSet<DiningSpot>();
        [NotNull] private readonly List<Pawn> spawnedDiningPawnsResult = new List<Pawn>();
        [NotNull] private readonly List<Pawn> spawnedActiveStaffResult = new List<Pawn>();
        [NotNull] public IReadOnlyList<Building_CashRegister> Registers => registers;

		public Map Map { get; }
        private RestaurantMenu menu;
		private RestaurantOrders orders;
		private RestaurantDebt debts;
		private RestaurantStock stock;

        [NotNull] private List<Building_CashRegister> registers = new List<Building_CashRegister>();

		private int day;

		public bool IsOpenedRightNow => openForBusiness && AnyRegisterOpen;

		private bool AnyRegisterOpen => Registers.Any(r => r?.IsActive == true);

		public bool openForBusiness = true;

		public bool allowGuests = true;
		public bool allowColonists = true;
		public bool allowPrisoners = false;
		public bool allowSlaves = false;

		public float guestPricePercentage = 1;
        private string name;

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
				spawnedDiningPawnsResult.AddRange(Map.mapPawns.AllPawnsSpawned.Where(pawn => pawn.jobs?.curDriver is JobDriver_Dine));
				return spawnedDiningPawnsResult;
			}
		}
		[NotNull] public List<Pawn> ActiveStaff
		{
			get
			{
				spawnedActiveStaffResult.Clear();
				var activeShifts = Registers.SelectMany(r => r.shifts.Where(s => s.IsActive));
                spawnedActiveStaffResult.AddRange(activeShifts.SelectMany(s => s.assigned).Where(p => p.MapHeld == Map));

                return spawnedActiveStaffResult;
			}
		}

        public string Name
        {
            get => name;
            set => name = value;
        }

        public RestaurantController(Map map)
        {
            Map = map;
        }

		public void ExposeData()
		{
			Scribe_Values.Look(ref openForBusiness, "openForBusiness", true);
			Scribe_Values.Look(ref allowGuests, "allowGuests", true);
			Scribe_Values.Look(ref allowColonists, "allowColonists", true);
			Scribe_Values.Look(ref allowPrisoners, "allowPrisoners", false);
			Scribe_Values.Look(ref allowSlaves, "allowSlaves", false);
			Scribe_Values.Look(ref guestPricePercentage, "guestPricePercentage", 1);
			Scribe_Values.Look(ref day, "day");
			Scribe_Values.Look(ref name, "name");
			Scribe_Deep.Look(ref menu, "menu");
			Scribe_Deep.Look(ref stock, "stock", this);
			Scribe_Deep.Look(ref orders, "orders", this);
			Scribe_Deep.Look(ref debts, "debts", this);
			Scribe_Collections.Look(ref registers, "registers", LookMode.Reference);
			InitDeepFieldsInitial();
		}

		private void InitDeepFieldsInitial()
		{
			menu ??= new RestaurantMenu();
			orders ??= new RestaurantOrders(this);
			debts ??= new RestaurantDebt(this);
			stock ??= new RestaurantStock(this);
		}

		public void MapGenerated()
		{
			InitDeepFieldsInitial();
		}

		public void FinalizeInit()
		{
			InitDeepFieldsInitial();
			RescanDiningSpots();
			stock.RareTick();
			orders.RareTick();
			debts.RareTick();

			TableTop_Events.onAnyBuildingSpawned.AddListener(UpdateRegisterWithBuilding);
			TableTop_Events.onAnyBuildingDespawned.AddListener(UpdateRegisterWithBuilding);

            foreach (var register in Registers)
            {
                register.onRadiusChanged.AddListener(OnRegisterRadiusChanged);
            }
        }

        private void UpdateRegisterWithBuilding(Building building, Map map)
        {
            if (map != Map) return;
            if (building is Building_CashRegister register)
            {
                if (register.Spawned) AddRegister(register);
                else RemoveRegister(register);
            }

            if (building is DiningSpot spot)
            {
                if (Registers.Any(r => r.GetIsInRange(building.Position)))
                {
                    if (spot.Spawned) diningSpots.Add(spot);
                    else diningSpots.Remove(spot);
                }
            }
        }

        private void OnRegisterRadiusChanged(Building_CashRegister register)
        {
            //RefreshRegisters(null, register.Map);
			RescanDiningSpots();
        }

        public void OnTick()
		{
			// Don't tick everything at once
			if ((GenTicks.TicksGame + Map.uniqueID) % 500 == 0) stock.RareTick();
			if ((GenTicks.TicksGame + Map.uniqueID) % 500 == 250) orders.RareTick();
			//Log.Message($"Stock: {stock.Select(s => s.def.label).ToCommaList(true)}");
			if ((GenTicks.TicksGame + Map.uniqueID) % 500 == 300) RareTick();
		}

		private void RareTick()
		{
			if (GenDate.DaysPassed > day && GenLocalDate.HourInteger(Map) == 0) OnNextDay(GenDate.DaysPassed);
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
            foreach (var register in Registers)
            {
                foreach (var diningSpot in DiningUtility.GetAllDiningSpots(Map).Where(spot => register.GetIsInRange(spot.Position))) diningSpots.Add(diningSpot);
            }
		}

        public void CleanUpForRemoval()
        {
            openForBusiness = false;
        }

        private void OnRegistersChanged()
        {
            RescanDiningSpots();
        }

        public void LinkRegister(Building_CashRegister register)
        {
            foreach (var restaurant in register.GetAllRestaurants())
            {
                restaurant.RemoveRegister(register);
            }

            AddRegister(register);
        }

        private void AddRegister(Building_CashRegister register)
        {
            if (!registers.Contains(register))
            {
                registers.Add(register);
                OnRegistersChanged();
                register.onRadiusChanged.AddListener(OnRegisterRadiusChanged);
            }
        }

        public void RemoveRegister(Building_CashRegister register)
        {
            if (registers.Remove(register))
            {
                OnRegistersChanged();
				register.onRadiusChanged.RemoveListener(OnRegisterRadiusChanged);
            }
		}
    }
}
