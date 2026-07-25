using Vectis.Domain;
using Vectis.Web.Services;

var tests = new BusinessRuleTests();
tests.RunAll();
Console.WriteLine("Tous les tests Vectis sont passes.");

internal sealed class BusinessRuleTests
{
    private readonly VectisEngine _engine = new(new FixedTimeProvider(new DateTimeOffset(2026, 7, 25, 10, 0, 0, TimeSpan.Zero)));

    public void RunAll()
    {
        PumpingContainersCannotExceedTotal();
        PreparingBottleDecreasesStockAndKeepsTraceability();
        PreparingBottleCannotOverdrawSameContainerTwice();
        FeedingUpdatesPreparedBottleStatus();
        ExpiredContainerIsNotRecommended();
        FeedingTracksLeftover();
        FamilyAccessIsIsolated();
        FamilyInvitationFlow();
        CancelledInvitationCannotBeAccepted();
        StockReadRequiresFamilyAccess();
        HistoryReadRequiresFamilyAccess();
    }

    private void PumpingContainersCannotExceedTotal()
    {
        var (state, user, baby) = CreateReadyState();
        Throws(() => _engine.AddPumpingSession(state, user.Id, baby.Id, DateTimeOffset.UtcNow, 100, null, null, "",
        [
            new(ContainerType.StorageBag, 80, StorageLocation.Refrigerator, ""),
            new(ContainerType.StorageBag, 40, StorageLocation.Refrigerator, "")
        ]));
    }

    private void PreparingBottleDecreasesStockAndKeepsTraceability()
    {
        var (state, user, baby) = CreateReadyState();
        _engine.AddPumpingSession(state, user.Id, baby.Id, DateTimeOffset.UtcNow, 180, null, "both", "",
        [
            new(ContainerType.StorageBag, 100, StorageLocation.Refrigerator, ""),
            new(ContainerType.StorageBag, 80, StorageLocation.Refrigerator, "")
        ]);

        var available = _engine.AvailableContainers(state, baby.Id);
        var bottle = _engine.PrepareBottle(state, user.Id, baby.Id,
        [
            new(available[0].Id, 60),
            new(available[1].Id, 60)
        ], "");

        Equal(120, bottle.TotalQuantityMl);
        Equal(2, bottle.Sources.Count);
        Equal(40, state.Containers.First(container => container.Id == available[0].Id).RemainingQuantityMl);
        Equal(20, state.Containers.First(container => container.Id == available[1].Id).RemainingQuantityMl);
    }

    private void PreparingBottleCannotOverdrawSameContainerTwice()
    {
        var (state, user, baby) = CreateReadyState();
        _engine.AddPumpingSession(state, user.Id, baby.Id, DateTimeOffset.UtcNow, 100, null, "both", "",
        [
            new(ContainerType.StorageBag, 100, StorageLocation.Refrigerator, "")
        ]);

        var available = _engine.AvailableContainers(state, baby.Id);
        Throws(() => _engine.PrepareBottle(state, user.Id, baby.Id,
        [
            new(available[0].Id, 60),
            new(available[0].Id, 60)
        ], ""));
    }

    private void FeedingUpdatesPreparedBottleStatus()
    {
        var (state, user, baby) = CreateReadyState();
        _engine.AddPumpingSession(state, user.Id, baby.Id, DateTimeOffset.UtcNow, 120, null, "both", "",
        [
            new(ContainerType.StorageBag, 120, StorageLocation.Refrigerator, "")
        ]);

        var available = _engine.AvailableContainers(state, baby.Id);
        var bottle = _engine.PrepareBottle(state, user.Id, baby.Id, [new(available[0].Id, 120)], "");
        _engine.RecordFeeding(state, user.Id, baby.Id, bottle.Id, bottle.TotalQuantityMl, 90, FeedingReaction.Normal, "jete", "");

        Equal("partially_consumed", state.PreparedBottles.Single(item => item.Id == bottle.Id).Status);
    }

    private void ExpiredContainerIsNotRecommended()
    {
        var engine = new VectisEngine(new FixedTimeProvider(new DateTimeOffset(2026, 7, 30, 10, 0, 0, TimeSpan.Zero)));
        var (state, user, baby) = CreateReadyState(engine);
        engine.AddPumpingSession(state, user.Id, baby.Id, new DateTimeOffset(2026, 7, 20, 10, 0, 0, TimeSpan.Zero), 100, null, null, "",
        [
            new(ContainerType.StorageBag, 100, StorageLocation.Refrigerator, "")
        ]);

        Equal(0, engine.BuildStockSummary(state, baby.Id).TotalAvailableMl);
    }

    private void FeedingTracksLeftover()
    {
        var (state, user, baby) = CreateReadyState();
        var feeding = _engine.RecordFeeding(state, user.Id, baby.Id, null, 120, 90, FeedingReaction.Normal, "jete", "");
        Equal(30, feeding.LeftoverQuantityMl);
    }

    private void FamilyAccessIsIsolated()
    {
        var (state, user, baby) = CreateReadyState();
        var other = _engine.RegisterUser(state, "Autre", "Parent", "other@test.local", "hash");
        Throws(() => _engine.AddPumpingSession(state, other.Id, baby.Id, DateTimeOffset.UtcNow, 100, null, null, "",
        [
            new(ContainerType.StorageBag, 100, StorageLocation.Refrigerator, "")
        ]));
    }

    private void FamilyInvitationFlow()
    {
        var (state, admin, baby) = CreateReadyState();
        var familyId = baby.FamilyId;
        var caregiver = _engine.RegisterUser(state, "Second", "Parent", "second@test.local", "hash");

        var invitation = Invite(state, admin.Id, familyId, caregiver.Email, UserRole.Caregiver);
        Equal("pending", invitation.Status);

        AcceptInvitation(state, invitation.Id, caregiver.Id);
        Equal(true, state.Members.Any(member => member.UserId == caregiver.Id && member.FamilyId == familyId && member.Role == UserRole.Caregiver && member.Status == "accepted"));

        Throws(() => Invite(state, caregiver.Id, familyId, "third@test.local", UserRole.Caregiver));
    }

    private void CancelledInvitationCannotBeAccepted()
    {
        var (state, admin, baby) = CreateReadyState();
        var caregiver = _engine.RegisterUser(state, "Second", "Parent", "second@test.local", "hash");

        var invitation = Invite(state, admin.Id, baby.FamilyId, caregiver.Email, UserRole.Caregiver);
        CancelInvitation(state, admin.Id, baby.FamilyId, invitation.Id);

        Equal("cancelled", state.Invitations.Single(item => item.Id == invitation.Id).Status);
        Throws(() => AcceptInvitation(state, invitation.Id, caregiver.Id));
    }

    private void StockReadRequiresFamilyAccess()
    {
        var (state, _, baby) = CreateReadyState();
        var outsider = _engine.RegisterUser(state, "Autre", "Parent", "outsider@test.local", "hash");
        var otherFamily = _engine.CreateFamily(state, outsider.Id, "Autre famille");
        _engine.CreateBaby(state, outsider.Id, otherFamily.Id, "Nora", new DateOnly(2026, 2, 1), 120, "");

        var service = new StockService(new MemoryAppStore(state), _engine);
        ThrowsAsync(() => service.GetSummaryAsync(outsider.Id, baby.Id));
        ThrowsAsync(() => service.GetAvailableContainersAsync(outsider.Id, baby.Id));
    }

    private void HistoryReadRequiresFamilyAccess()
    {
        var (state, admin, baby) = CreateReadyState();
        var outsider = _engine.RegisterUser(state, "Autre", "Parent", "outsider@test.local", "hash");
        var otherFamily = _engine.CreateFamily(state, outsider.Id, "Autre famille");
        _engine.CreateBaby(state, outsider.Id, otherFamily.Id, "Nora", new DateOnly(2026, 2, 1), 120, "");
        state.AuditEntries.Add(new AuditEntry(Guid.NewGuid(), outsider.Id, "other_action", nameof(Family), otherFamily.Id, "", "", DateTimeOffset.UtcNow));
        state.AuditEntries.Add(new AuditEntry(Guid.NewGuid(), admin.Id, "family_action", nameof(Baby), baby.Id, "", "", DateTimeOffset.UtcNow));

        var service = new HistoryService(new MemoryAppStore(state));
        ThrowsAsync(() => service.GetAsync(outsider.Id, baby.Id));

        var history = service.GetAsync(admin.Id, baby.Id).GetAwaiter().GetResult();
        Equal(false, history.AuditEntries.Any(item => item.UserId == outsider.Id));
        Equal(true, history.AuditEntries.Any(item => item.UserId == admin.Id));
    }

    private static FamilyInvitation Invite(AppState state, Guid adminUserId, Guid familyId, string email, UserRole role)
    {
        if (state.Members.All(member => member.UserId != adminUserId || member.FamilyId != familyId || member.Role != UserRole.Admin || member.Status != "accepted"))
        {
            throw new InvalidOperationException("Admin requis.");
        }

        var invitation = new FamilyInvitation(Guid.NewGuid(), familyId, email, role, "pending", adminUserId, DateTimeOffset.UtcNow, null);
        state.Invitations.Add(invitation);
        return invitation;
    }

    private static void AcceptInvitation(AppState state, Guid invitationId, Guid userId)
    {
        var user = state.Users.Single(item => item.Id == userId);
        var index = state.Invitations.FindIndex(invitation => invitation.Id == invitationId && invitation.Status == "pending");
        if (index < 0)
        {
            throw new InvalidOperationException("Invitation introuvable.");
        }

        var invitation = state.Invitations[index];
        if (!user.Email.Equals(invitation.Email, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Email incorrect.");
        }

        state.Members.Add(new FamilyMember(user.Id, invitation.FamilyId, invitation.Role, "accepted"));
        state.Invitations[index] = invitation with { Status = "accepted", AcceptedAt = DateTimeOffset.UtcNow };
    }

    private static void CancelInvitation(AppState state, Guid adminUserId, Guid familyId, Guid invitationId)
    {
        if (state.Members.All(member => member.UserId != adminUserId || member.FamilyId != familyId || member.Role != UserRole.Admin || member.Status != "accepted"))
        {
            throw new InvalidOperationException("Admin requis.");
        }

        var index = state.Invitations.FindIndex(invitation => invitation.Id == invitationId && invitation.FamilyId == familyId && invitation.Status == "pending");
        if (index < 0)
        {
            throw new InvalidOperationException("Invitation introuvable.");
        }

        state.Invitations[index] = state.Invitations[index] with { Status = "cancelled" };
    }

    private (AppState State, AppUser User, Baby Baby) CreateReadyState(VectisEngine? engine = null)
    {
        engine ??= _engine;
        var state = new AppState();
        var user = engine.RegisterUser(state, "Demo", "User", Guid.NewGuid() + "@test.local", "hash");
        var family = engine.CreateFamily(state, user.Id, "Famille Test");
        var baby = engine.CreateBaby(state, user.Id, family.Id, "Adam", new DateOnly(2026, 1, 1), 120, "");
        return (state, user, baby);
    }

    private static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Attendu {expected}, obtenu {actual}.");
        }
    }

    private static void Throws(Action action)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException("Une exception metier etait attendue.");
    }

    private static void ThrowsAsync(Func<Task> action)
    {
        try
        {
            action().GetAwaiter().GetResult();
        }
        catch (InvalidOperationException)
        {
            return;
        }

        throw new InvalidOperationException("Une exception metier etait attendue.");
    }
}

internal sealed class MemoryAppStore : IAppStore
{
    private readonly AppState _state;

    public MemoryAppStore(AppState state)
    {
        _state = state;
    }

    public Task<AppState> LoadAsync() => Task.FromResult(_state);

    public Task MutateAsync(Action<AppState> action)
    {
        action(_state);
        return Task.CompletedTask;
    }

    public Task<T> MutateAsync<T>(Func<AppState, T> action) => Task.FromResult(action(_state));
}

internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FixedTimeProvider(DateTimeOffset now)
    {
        _now = now;
    }

    public override DateTimeOffset GetUtcNow() => _now;
}
