using Microsoft.EntityFrameworkCore;
using Vectis.Domain;
using Vectis.Web.Data;
using Vectis.Web.Data.Entities;

namespace Vectis.Web.Services;

public sealed class EfAppStore : IAppStore
{
    private readonly IDbContextFactory<VectisDbContext> _dbFactory;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public EfAppStore(IDbContextFactory<VectisDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<AppState> LoadAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await LoadUnsafeAsync(db);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task MutateAsync(Action<AppState> action)
    {
        await _lock.WaitAsync();
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var state = await LoadUnsafeAsync(db);
            action(state);
            await SaveUnsafeAsync(db, state);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<T> MutateAsync<T>(Func<AppState, T> action)
    {
        await _lock.WaitAsync();
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var state = await LoadUnsafeAsync(db);
            var result = action(state);
            await SaveUnsafeAsync(db, state);
            return result;
        }
        finally
        {
            _lock.Release();
        }
    }

    private static async Task<AppState> LoadUnsafeAsync(VectisDbContext db)
    {
        return new AppState
        {
            Users = await db.Users.AsNoTracking().Select(item => new AppUser(item.Id, item.FirstName, item.LastName, item.Email, item.PasswordHash, item.Language, item.TimeZone, item.CreatedAt)).ToListAsync(),
            Families = await db.Families.AsNoTracking().Select(item => new Family(item.Id, item.Name, item.CreatorUserId, item.CreatedAt)).ToListAsync(),
            Members = await db.FamilyMembers.AsNoTracking().Select(item => new FamilyMember(item.UserId, item.FamilyId, item.Role, item.Status)).ToListAsync(),
            Invitations = await db.FamilyInvitations.AsNoTracking().Select(item => new FamilyInvitation(item.Id, item.FamilyId, item.Email, item.Role, item.Status, item.InvitedByUserId, item.CreatedAt, item.AcceptedAt)).ToListAsync(),
            NotificationPreferences = await db.NotificationPreferences.AsNoTracking().Select(item => new NotificationPreferences(item.FamilyId, item.StockLowEnabled, item.ExpiringSoonEnabled, item.PreparedBottleAgingEnabled, item.StockLowBottleThreshold, item.ExpiringSoonHours, item.PreparedBottleAgeMinutes)).ToListAsync(),
            NotificationDeliveries = await db.NotificationDeliveries.AsNoTracking().Select(item => new NotificationDelivery(item.Id, item.FamilyId, item.Kind, item.RecipientEmail, item.Subject, item.Status, item.Message, item.CreatedAt, item.SentAt)).ToListAsync(),
            Babies = await db.Babies.AsNoTracking().Select(item => new Baby(item.Id, item.FamilyId, item.FirstName, item.BirthDate, item.CurrentWeightKg, item.UsualBottleMl, item.Notes, item.IsActive)).ToListAsync(),
            PumpingSessions = await db.PumpingSessions.AsNoTracking().Select(item => new PumpingSession(item.Id, item.BabyId, item.PumpedAt, item.TotalQuantityMl, item.DurationMinutes, item.Side, item.CreatedByUserId, item.Notes, item.CreatedAt)).ToListAsync(),
            Containers = await db.MilkContainers.AsNoTracking().Select(item => new MilkContainer(item.Id, item.PumpingSessionId, item.BabyId, item.Type, item.InitialQuantityMl, item.RemainingQuantityMl, item.Location, item.Status, item.PumpedAt, item.CreatedAt, item.EstimatedExpiresAt, item.Notes)).ToListAsync(),
            StockMovements = await db.StockMovements.AsNoTracking().Select(item => new StockMovement(item.Id, item.ContainerId, item.PreviousLocation, item.NewLocation, item.PreviousStatus, item.NewStatus, item.OccurredAt, item.UserId, item.Comment)).ToListAsync(),
            PreparedBottles = await LoadPreparedBottlesAsync(db),
            Feedings = await db.Feedings.AsNoTracking().Select(item => new Feeding(item.Id, item.BabyId, item.PreparedBottleId, item.StartedAt, item.EndedAt, item.PreparedQuantityMl, item.ConsumedQuantityMl, item.LeftoverQuantityMl, item.MilkType, item.Reaction, item.FedByUserId, item.LeftoverOutcome, item.Notes)).ToListAsync(),
            ConservationRules = await LoadConservationRulesAsync(db),
            AuditEntries = await db.AuditEntries.AsNoTracking().Select(item => new AuditEntry(item.Id, item.UserId, item.Action, item.EntityName, item.EntityId, item.OldValue, item.NewValue, item.OccurredAt)).ToListAsync()
        };
    }

    private static async Task<List<ConservationRule>> LoadConservationRulesAsync(VectisDbContext db)
    {
        var rules = await db.ConservationRules
            .AsNoTracking()
            .Select(item => new ConservationRule(item.Location, item.DurationHours, item.IsActive, item.UpdatedAt))
            .ToListAsync();

        return rules.Count == 0 ? DefaultConservationRules.Create() : rules;
    }

    private static async Task<List<PreparedBottle>> LoadPreparedBottlesAsync(VectisDbContext db)
    {
        var bottles = await db.PreparedBottles.AsNoTracking().ToListAsync();
        var sources = await db.PreparedBottleSources.AsNoTracking().ToListAsync();
        return bottles
            .Select(item => new PreparedBottle(
                item.Id,
                item.BabyId,
                item.TotalQuantityMl,
                item.PreparedAt,
                item.PreparedByUserId,
                sources.Where(source => source.PreparedBottleId == item.Id).Select(source => new PreparedBottleSource(source.ContainerId, source.QuantityMl)).ToList(),
                item.Status,
                item.Notes))
            .ToList();
    }

    private static async Task SaveUnsafeAsync(VectisDbContext db, AppState state)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();

        db.PreparedBottleSources.RemoveRange(db.PreparedBottleSources);
        db.Feedings.RemoveRange(db.Feedings);
        db.PreparedBottles.RemoveRange(db.PreparedBottles);
        db.StockMovements.RemoveRange(db.StockMovements);
        db.MilkContainers.RemoveRange(db.MilkContainers);
        db.PumpingSessions.RemoveRange(db.PumpingSessions);
        db.Babies.RemoveRange(db.Babies);
        db.NotificationDeliveries.RemoveRange(db.NotificationDeliveries);
        db.NotificationPreferences.RemoveRange(db.NotificationPreferences);
        db.FamilyInvitations.RemoveRange(db.FamilyInvitations);
        db.FamilyMembers.RemoveRange(db.FamilyMembers);
        db.Families.RemoveRange(db.Families);
        db.Users.RemoveRange(db.Users);
        db.ConservationRules.RemoveRange(db.ConservationRules);
        db.AuditEntries.RemoveRange(db.AuditEntries);
        await db.SaveChangesAsync();

        db.Users.AddRange(state.Users.Select(item => new UserEntity { Id = item.Id, FirstName = item.FirstName, LastName = item.LastName, Email = item.Email, PasswordHash = item.PasswordHash, Language = item.Language, TimeZone = item.TimeZone, CreatedAt = item.CreatedAt }));
        db.Families.AddRange(state.Families.Select(item => new FamilyEntity { Id = item.Id, Name = item.Name, CreatorUserId = item.CreatorUserId, CreatedAt = item.CreatedAt }));
        db.FamilyMembers.AddRange(state.Members.Select(item => new FamilyMemberEntity { UserId = item.UserId, FamilyId = item.FamilyId, Role = item.Role, Status = item.Status }));
        db.FamilyInvitations.AddRange(state.Invitations.Select(item => new FamilyInvitationEntity { Id = item.Id, FamilyId = item.FamilyId, Email = item.Email, Role = item.Role, Status = item.Status, InvitedByUserId = item.InvitedByUserId, CreatedAt = item.CreatedAt, AcceptedAt = item.AcceptedAt }));
        db.NotificationPreferences.AddRange(state.NotificationPreferences.Select(item => new NotificationPreferencesEntity { FamilyId = item.FamilyId, StockLowEnabled = item.StockLowEnabled, ExpiringSoonEnabled = item.ExpiringSoonEnabled, PreparedBottleAgingEnabled = item.PreparedBottleAgingEnabled, StockLowBottleThreshold = item.StockLowBottleThreshold, ExpiringSoonHours = item.ExpiringSoonHours, PreparedBottleAgeMinutes = item.PreparedBottleAgeMinutes }));
        db.NotificationDeliveries.AddRange(state.NotificationDeliveries.Select(item => new NotificationDeliveryEntity { Id = item.Id, FamilyId = item.FamilyId, Kind = item.Kind, RecipientEmail = item.RecipientEmail, Subject = item.Subject, Status = item.Status, Message = item.Message, CreatedAt = item.CreatedAt, SentAt = item.SentAt }));
        db.Babies.AddRange(state.Babies.Select(item => new BabyEntity { Id = item.Id, FamilyId = item.FamilyId, FirstName = item.FirstName, BirthDate = item.BirthDate, CurrentWeightKg = item.CurrentWeightKg, UsualBottleMl = item.UsualBottleMl, Notes = item.Notes, IsActive = item.IsActive }));
        db.PumpingSessions.AddRange(state.PumpingSessions.Select(item => new PumpingSessionEntity { Id = item.Id, BabyId = item.BabyId, PumpedAt = item.PumpedAt, TotalQuantityMl = item.TotalQuantityMl, DurationMinutes = item.DurationMinutes, Side = item.Side, CreatedByUserId = item.CreatedByUserId, Notes = item.Notes, CreatedAt = item.CreatedAt }));
        db.MilkContainers.AddRange(state.Containers.Select(item => new MilkContainerEntity { Id = item.Id, PumpingSessionId = item.PumpingSessionId, BabyId = item.BabyId, Type = item.Type, InitialQuantityMl = item.InitialQuantityMl, RemainingQuantityMl = item.RemainingQuantityMl, Location = item.Location, Status = item.Status, PumpedAt = item.PumpedAt, CreatedAt = item.CreatedAt, EstimatedExpiresAt = item.EstimatedExpiresAt, Notes = item.Notes }));
        db.StockMovements.AddRange(state.StockMovements.Select(item => new StockMovementEntity { Id = item.Id, ContainerId = item.ContainerId, PreviousLocation = item.PreviousLocation, NewLocation = item.NewLocation, PreviousStatus = item.PreviousStatus, NewStatus = item.NewStatus, OccurredAt = item.OccurredAt, UserId = item.UserId, Comment = item.Comment }));
        db.PreparedBottles.AddRange(state.PreparedBottles.Select(item => new PreparedBottleEntity { Id = item.Id, BabyId = item.BabyId, TotalQuantityMl = item.TotalQuantityMl, PreparedAt = item.PreparedAt, PreparedByUserId = item.PreparedByUserId, Status = item.Status, Notes = item.Notes }));
        db.PreparedBottleSources.AddRange(state.PreparedBottles.SelectMany(item => item.Sources.Select(source => new PreparedBottleSourceEntity { PreparedBottleId = item.Id, ContainerId = source.ContainerId, QuantityMl = source.QuantityMl })));
        db.Feedings.AddRange(state.Feedings.Select(item => new FeedingEntity { Id = item.Id, BabyId = item.BabyId, PreparedBottleId = item.PreparedBottleId, StartedAt = item.StartedAt, EndedAt = item.EndedAt, PreparedQuantityMl = item.PreparedQuantityMl, ConsumedQuantityMl = item.ConsumedQuantityMl, LeftoverQuantityMl = item.LeftoverQuantityMl, MilkType = item.MilkType, Reaction = item.Reaction, FedByUserId = item.FedByUserId, LeftoverOutcome = item.LeftoverOutcome, Notes = item.Notes }));
        db.ConservationRules.AddRange(state.ConservationRules.Select(item => new ConservationRuleEntity { Location = item.Location, DurationHours = item.DurationHours, IsActive = item.IsActive, UpdatedAt = item.UpdatedAt }));
        db.AuditEntries.AddRange(state.AuditEntries.Select(item => new AuditEntryEntity { Id = item.Id, UserId = item.UserId, Action = item.Action, EntityName = item.EntityName, EntityId = item.EntityId, OldValue = item.OldValue, NewValue = item.NewValue, OccurredAt = item.OccurredAt }));

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
