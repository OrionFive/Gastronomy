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

namespace Gastronomy.Restaurant;

public class RestaurantController : IExposable, IRenameable
{
    [NotNull] public readonly HashSet<DiningSpot> diningSpots = new();
    [NotNull] private readonly List<Pawn> spawnedActiveStaffResult = new();
    [NotNull] private readonly List<Pawn> spawnedDiningPawnsResult = new();
    public bool allowColonists = true;

    public bool allowGuests = true;
    public bool allowPrisoners;
    public bool allowSlaves;

    private int day;
    private RestaurantDebt debts;

    public float guestPricePercentage = 1;
    private RestaurantMenu menu;
    private string name;

    public bool openForBusiness;
    private RestaurantOrders orders;

    [NotNull] private List<Building_CashRegister> registers = new();
    private RestaurantStock stock;

    public RestaurantController(Map map)
    {
        Map = map;
    }

    [NotNull] public IReadOnlyList<Building_CashRegister> Registers => registers;

    public Map Map { get; }

    public bool IsOpenedRightNow => openForBusiness && AnyRegisterOpen;

    private bool AnyRegisterOpen => Registers.Any(r => r?.IsActive == true);

    public int Seats => diningSpots.Sum(s => s.GetMaxSeats());
    [NotNull] public ReadOnlyCollection<Pawn> Patrons => SpawnedDiningPawns.AsReadOnly();
    [NotNull] public RestaurantMenu Menu => menu;
    [NotNull] public RestaurantOrders Orders => orders;
    [NotNull] public RestaurantDebt Debts => debts;
    [NotNull] public RestaurantStock Stock => stock;

    [NotNull]
    public List<Pawn> SpawnedDiningPawns
    {
        get
        {
            spawnedDiningPawnsResult.Clear();
            spawnedDiningPawnsResult.AddRange(Map.mapPawns.AllPawnsSpawned.Where(pawn => pawn.jobs?.curDriver is JobDriver_Dine && pawn.GetRestaurantsManager().GetRestaurantDining(pawn) == this));
            return spawnedDiningPawnsResult;
        }
    }

    [NotNull]
    public List<Pawn> ActiveStaff
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

    public void ExposeData()
    {
        Scribe_Values.Look(ref openForBusiness, "openForBusiness");
        Scribe_Values.Look(ref allowGuests, "allowGuests", true);
        Scribe_Values.Look(ref allowColonists, "allowColonists", true);
        Scribe_Values.Look(ref allowPrisoners, "allowPrisoners");
        Scribe_Values.Look(ref allowSlaves, "allowSlaves");
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

    public string RenamableLabel
    {
        get => name ?? BaseLabel;
        set => name = value;
    }

    public string BaseLabel => "RestaurantDefaultName";

    public string InspectLabel => RenamableLabel;

    public event Action onNextDay;

    public bool GetIsInRange(IntVec3 position)
    {
        return Registers.Any(r => r.GetIsInRange(position));
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
        Stock.RefreshStock();
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
        if (!allowColonists && pawn.IsColonist) return false;
        if (!allowGuests && pawn.IsGuest()) return false;
        if (!allowPrisoners && pawn.IsPrisoner) return false;
        if (!allowSlaves && pawn.IsSlave) return false;

        return true;
    }

    public bool HasToWork(Pawn pawn)
    {
        if (!openForBusiness) return false;
        return Registers.Any(r => r?.HasToWork(pawn) == true);
    }

    public void RescanDiningSpots()
    {
        var count = diningSpots.Count;
        diningSpots.Clear();
        foreach (var register in Registers)
        {
            foreach (var diningSpot in DiningUtility.GetAllDiningSpots(Map).Where(spot => register.GetIsInRange(spot.Position)))
            {
                if (!diningSpot.Spawned || diningSpot.Destroyed) Log.Warning("Scan returned destroyed dining spot.");
                else diningSpots.Add(diningSpot);
            }
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

    public void OnDiningSpotsChanged()
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

    public bool CanDineHere(Pawn pawn)
    {
        return IsOpenedRightNow && MayDineHere(pawn);
    }
}