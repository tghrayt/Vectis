using Vectis.Domain;

namespace Vectis.Web.Data.Entities;

public sealed class UserEntity
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Language { get; set; } = "fr";
    public string TimeZone { get; set; } = "Europe/Paris";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class FamilyEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public Guid CreatorUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class FamilyMemberEntity
{
    public Guid UserId { get; set; }
    public Guid FamilyId { get; set; }
    public UserRole Role { get; set; }
    public string Status { get; set; } = "";
}

public sealed class FamilyInvitationEntity
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public string Email { get; set; } = "";
    public UserRole Role { get; set; }
    public string Status { get; set; } = "";
    public Guid InvitedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
}

public sealed class NotificationPreferencesEntity
{
    public Guid FamilyId { get; set; }
    public bool AutomaticEmailEnabled { get; set; }
    public bool StockLowEnabled { get; set; } = true;
    public bool ExpiringSoonEnabled { get; set; } = true;
    public bool PreparedBottleAgingEnabled { get; set; } = true;
    public int StockLowBottleThreshold { get; set; } = 2;
    public int ExpiringSoonHours { get; set; } = 24;
    public int PreparedBottleAgeMinutes { get; set; } = 120;
}

public sealed class NotificationDeliveryEntity
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public NotificationKind Kind { get; set; }
    public string RecipientEmail { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Status { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}

public sealed class BabyEntity
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public string FirstName { get; set; } = "";
    public DateOnly BirthDate { get; set; }
    public decimal? CurrentWeightKg { get; set; }
    public int UsualBottleMl { get; set; }
    public string Notes { get; set; } = "";
    public bool IsActive { get; set; }
}

public sealed class PumpingSessionEntity
{
    public Guid Id { get; set; }
    public Guid BabyId { get; set; }
    public DateTimeOffset PumpedAt { get; set; }
    public int TotalQuantityMl { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Side { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Notes { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class MilkContainerEntity
{
    public Guid Id { get; set; }
    public Guid PumpingSessionId { get; set; }
    public Guid BabyId { get; set; }
    public ContainerType Type { get; set; }
    public int InitialQuantityMl { get; set; }
    public int RemainingQuantityMl { get; set; }
    public StorageLocation Location { get; set; }
    public MilkStatus Status { get; set; }
    public DateTimeOffset PumpedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset EstimatedExpiresAt { get; set; }
    public string Notes { get; set; } = "";
}

public sealed class StockMovementEntity
{
    public Guid Id { get; set; }
    public Guid ContainerId { get; set; }
    public StorageLocation? PreviousLocation { get; set; }
    public StorageLocation NewLocation { get; set; }
    public MilkStatus? PreviousStatus { get; set; }
    public MilkStatus NewStatus { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public Guid UserId { get; set; }
    public string Comment { get; set; } = "";
}

public sealed class PreparedBottleEntity
{
    public Guid Id { get; set; }
    public Guid BabyId { get; set; }
    public int TotalQuantityMl { get; set; }
    public DateTimeOffset PreparedAt { get; set; }
    public Guid PreparedByUserId { get; set; }
    public string Status { get; set; } = "";
    public string Notes { get; set; } = "";
}

public sealed class PreparedBottleSourceEntity
{
    public Guid PreparedBottleId { get; set; }
    public Guid ContainerId { get; set; }
    public int QuantityMl { get; set; }
}

public sealed class FeedingEntity
{
    public Guid Id { get; set; }
    public Guid BabyId { get; set; }
    public Guid? PreparedBottleId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public int PreparedQuantityMl { get; set; }
    public int ConsumedQuantityMl { get; set; }
    public int LeftoverQuantityMl { get; set; }
    public string MilkType { get; set; } = "";
    public FeedingReaction Reaction { get; set; }
    public Guid FedByUserId { get; set; }
    public string LeftoverOutcome { get; set; } = "";
    public string Notes { get; set; } = "";
}

public sealed class ConservationRuleEntity
{
    public StorageLocation Location { get; set; }
    public int DurationHours { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class AuditEntryEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Action { get; set; } = "";
    public string EntityName { get; set; } = "";
    public Guid EntityId { get; set; }
    public string OldValue { get; set; } = "";
    public string NewValue { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
}
