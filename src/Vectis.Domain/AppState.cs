namespace Vectis.Domain;

public sealed class AppState
{
    public List<AppUser> Users { get; set; } = [];
    public List<Family> Families { get; set; } = [];
    public List<FamilyMember> Members { get; set; } = [];
    public List<FamilyInvitation> Invitations { get; set; } = [];
    public List<NotificationPreferences> NotificationPreferences { get; set; } = [];
    public List<NotificationDelivery> NotificationDeliveries { get; set; } = [];
    public List<Baby> Babies { get; set; } = [];
    public List<PumpingSession> PumpingSessions { get; set; } = [];
    public List<MilkContainer> Containers { get; set; } = [];
    public List<StockMovement> StockMovements { get; set; } = [];
    public List<PreparedBottle> PreparedBottles { get; set; } = [];
    public List<Feeding> Feedings { get; set; } = [];
    public List<ConservationRule> ConservationRules { get; set; } = DefaultConservationRules.Create();
    public List<AuditEntry> AuditEntries { get; set; } = [];
}

public static class DefaultConservationRules
{
    public static List<ConservationRule> Create()
    {
        var now = DateTimeOffset.UtcNow;
        return
        [
            new(StorageLocation.RoomTemperature, 4, true, now),
            new(StorageLocation.Refrigerator, 96, true, now),
            new(StorageLocation.FridgeFreezerCompartment, 336, true, now),
            new(StorageLocation.SeparateFreezer, 4380, true, now),
            new(StorageLocation.CoolerBag, 24, true, now),
            new(StorageLocation.Other, 24, true, now)
        ];
    }
}

public sealed record ContainerDraft(ContainerType Type, int QuantityMl, StorageLocation Location, string Notes);

public sealed record StockSummary(
    int TotalAvailableMl,
    int RefrigeratorMl,
    int FreezerMl,
    int ThawingMl,
    int ExpiringSoonMl,
    int EstimatedBottles,
    int PumpedTodayMl,
    int ConsumedTodayMl,
    int DiscardedTodayMl,
    MilkContainer? NextRecommended,
    PumpingSession? LastPumping,
    Feeding? LastFeeding,
    decimal AverageConsumedPerBottleMl);
