namespace Vectis.Domain;

public enum UserRole
{
    Admin,
    Caregiver
}

public enum ContainerType
{
    StorageBag,
    Bottle,
    Jar,
    Other
}

public enum StorageLocation
{
    RoomTemperature,
    Refrigerator,
    FridgeFreezerCompartment,
    SeparateFreezer,
    CoolerBag,
    Other
}

public enum MilkStatus
{
    FreshlyPumped,
    Refrigerated,
    Frozen,
    Thawing,
    Thawed,
    ReadyToFeed,
    PartiallyConsumed,
    Consumed,
    Discarded,
    Expired
}

public enum FeedingReaction
{
    Finished,
    Normal,
    Slow,
    Refused,
    Reflux,
    Vomiting,
    Discomfort,
    Other
}

public enum NotificationKind
{
    StockLow,
    ExpiringSoon,
    PreparedBottleAging
}

public sealed record AppUser(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string PasswordHash,
    string Language,
    string TimeZone,
    DateTimeOffset CreatedAt);

public sealed record FamilyMember(Guid UserId, Guid FamilyId, UserRole Role, string Status);

public sealed record FamilyInvitation(
    Guid Id,
    Guid FamilyId,
    string Email,
    UserRole Role,
    string Status,
    Guid InvitedByUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? AcceptedAt);

public sealed record NotificationPreferences(
    Guid FamilyId,
    bool StockLowEnabled,
    bool ExpiringSoonEnabled,
    bool PreparedBottleAgingEnabled,
    int StockLowBottleThreshold,
    int ExpiringSoonHours,
    int PreparedBottleAgeMinutes);

public sealed record NotificationDelivery(
    Guid Id,
    Guid FamilyId,
    NotificationKind Kind,
    string RecipientEmail,
    string Subject,
    string Status,
    string Message,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt);

public sealed record Family(Guid Id, string Name, Guid CreatorUserId, DateTimeOffset CreatedAt);

public sealed record Baby(
    Guid Id,
    Guid FamilyId,
    string FirstName,
    DateOnly BirthDate,
    decimal? CurrentWeightKg,
    int UsualBottleMl,
    string Notes,
    bool IsActive);

public sealed record PumpingSession(
    Guid Id,
    Guid BabyId,
    DateTimeOffset PumpedAt,
    int TotalQuantityMl,
    int? DurationMinutes,
    string? Side,
    Guid CreatedByUserId,
    string Notes,
    DateTimeOffset CreatedAt);

public sealed record MilkContainer(
    Guid Id,
    Guid PumpingSessionId,
    Guid BabyId,
    ContainerType Type,
    int InitialQuantityMl,
    int RemainingQuantityMl,
    StorageLocation Location,
    MilkStatus Status,
    DateTimeOffset PumpedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset EstimatedExpiresAt,
    string Notes);

public sealed record StockMovement(
    Guid Id,
    Guid ContainerId,
    StorageLocation? PreviousLocation,
    StorageLocation NewLocation,
    MilkStatus? PreviousStatus,
    MilkStatus NewStatus,
    DateTimeOffset OccurredAt,
    Guid UserId,
    string Comment);

public sealed record PreparedBottleSource(Guid ContainerId, int QuantityMl);

public sealed record PreparedBottle(
    Guid Id,
    Guid BabyId,
    int TotalQuantityMl,
    DateTimeOffset PreparedAt,
    Guid PreparedByUserId,
    IReadOnlyList<PreparedBottleSource> Sources,
    string Status,
    string Notes);

public sealed record Feeding(
    Guid Id,
    Guid BabyId,
    Guid? PreparedBottleId,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    int PreparedQuantityMl,
    int ConsumedQuantityMl,
    int LeftoverQuantityMl,
    string MilkType,
    FeedingReaction Reaction,
    Guid FedByUserId,
    string LeftoverOutcome,
    string Notes);

public sealed record ConservationRule(
    StorageLocation Location,
    int DurationHours,
    bool IsActive,
    DateTimeOffset UpdatedAt);

public sealed record AuditEntry(
    Guid Id,
    Guid UserId,
    string Action,
    string EntityName,
    Guid EntityId,
    string OldValue,
    string NewValue,
    DateTimeOffset OccurredAt);
