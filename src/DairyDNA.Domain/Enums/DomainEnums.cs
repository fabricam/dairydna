namespace DairyDNA.Domain.Enums;

public enum FacilityType
{
    Receiving = 0,
    Separation = 1,
    Storage = 2
}

public enum InventoryLotStatus
{
    Available = 0,
    Allocated = 1,
    Expired = 2
}

public enum OrderType
{
    Spot = 0
}

public enum OrderStatus
{
    Open = 0,
    Filled = 1,
    Partial = 2,
    Cancelled = 3
}

public enum TruckStatus
{
    Available = 0,
    Assigned = 1,
    Unavailable = 2
}

public enum MarketPriceType
{
    StaticSpot = 0
}

public enum GenerationRunStatus
{
    Queued = 0,
    Running = 1,
    Completed = 2,
    Failed = 3
}

public enum OptimizationRunStatus
{
    Queued = 0,
    Running = 1,
    Feasible = 2,
    Infeasible = 3,
    Failed = 4
}
