namespace Vectis.Domain;

public sealed class VectisEngine
{
    private readonly TimeProvider _clock;

    public VectisEngine(TimeProvider? clock = null)
    {
        _clock = clock ?? TimeProvider.System;
    }

    public AppUser RegisterUser(AppState state, string firstName, string lastName, string email, string passwordHash)
    {
        if (state.Users.Any(user => user.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Cette adresse e-mail est deja utilisee.");
        }

        var user = new AppUser(Guid.NewGuid(), firstName.Trim(), lastName.Trim(), email.Trim().ToLowerInvariant(), passwordHash, "fr", "Europe/Paris", Now());
        state.Users.Add(user);
        Audit(state, user.Id, "register", nameof(AppUser), user.Id, "", email);
        return user;
    }

    public Family CreateFamily(AppState state, Guid creatorUserId, string name)
    {
        RequireUser(state, creatorUserId);
        var family = new Family(Guid.NewGuid(), name.Trim(), creatorUserId, Now());
        state.Families.Add(family);
        state.Members.Add(new FamilyMember(creatorUserId, family.Id, UserRole.Admin, "accepted"));
        Audit(state, creatorUserId, "create", nameof(Family), family.Id, "", family.Name);
        return family;
    }

    public Baby CreateBaby(AppState state, Guid userId, Guid familyId, string firstName, DateOnly birthDate, int usualBottleMl, string notes)
    {
        EnsureFamilyAccess(state, userId, familyId);
        if (usualBottleMl <= 0)
        {
            throw new InvalidOperationException("La quantite habituelle doit etre positive.");
        }

        var baby = new Baby(Guid.NewGuid(), familyId, firstName.Trim(), birthDate, null, usualBottleMl, notes.Trim(), true);
        state.Babies.Add(baby);
        Audit(state, userId, "create", nameof(Baby), baby.Id, "", baby.FirstName);
        return baby;
    }

    public PumpingSession AddPumpingSession(
        AppState state,
        Guid userId,
        Guid babyId,
        DateTimeOffset pumpedAt,
        int totalQuantityMl,
        int? durationMinutes,
        string? side,
        string notes,
        IReadOnlyList<ContainerDraft> containers)
    {
        var baby = RequireBaby(state, babyId);
        EnsureFamilyAccess(state, userId, baby.FamilyId);

        if (totalQuantityMl <= 0)
        {
            throw new InvalidOperationException("La quantite tiree doit etre positive.");
        }

        if (containers.Count == 0)
        {
            throw new InvalidOperationException("Ajoute au moins un contenant.");
        }

        var containersTotal = containers.Sum(container => container.QuantityMl);
        if (containers.Any(container => container.QuantityMl <= 0) || containersTotal > totalQuantityMl)
        {
            throw new InvalidOperationException("La somme des contenants ne doit pas depasser le tirage total.");
        }

        var session = new PumpingSession(Guid.NewGuid(), babyId, pumpedAt, totalQuantityMl, durationMinutes, side, userId, notes.Trim(), Now());
        state.PumpingSessions.Add(session);

        foreach (var draft in containers)
        {
            var status = draft.Location is StorageLocation.SeparateFreezer or StorageLocation.FridgeFreezerCompartment
                ? MilkStatus.Frozen
                : draft.Location == StorageLocation.Refrigerator
                    ? MilkStatus.Refrigerated
                    : MilkStatus.FreshlyPumped;

            var container = new MilkContainer(
                Guid.NewGuid(),
                session.Id,
                babyId,
                draft.Type,
                draft.QuantityMl,
                draft.QuantityMl,
                draft.Location,
                status,
                pumpedAt,
                Now(),
                EstimateExpiration(state, pumpedAt, draft.Location),
                draft.Notes.Trim());

            state.Containers.Add(container);
            state.StockMovements.Add(new StockMovement(Guid.NewGuid(), container.Id, null, draft.Location, null, status, Now(), userId, "Creation du contenant"));
        }

        Audit(state, userId, "create", nameof(PumpingSession), session.Id, "", $"{totalQuantityMl} ml");
        return session;
    }

    public PreparedBottle PrepareBottle(AppState state, Guid userId, Guid babyId, IReadOnlyList<PreparedBottleSource> sources, string notes)
    {
        var baby = RequireBaby(state, babyId);
        EnsureFamilyAccess(state, userId, baby.FamilyId);
        if (sources.Count == 0)
        {
            throw new InvalidOperationException("Selectionne au moins un contenant.");
        }

        var requestedByContainer = sources
            .GroupBy(source => source.ContainerId)
            .Select(group => new PreparedBottleSource(group.Key, group.Sum(source => source.QuantityMl)))
            .ToList();

        foreach (var source in requestedByContainer)
        {
            var container = RequireContainer(state, source.ContainerId);
            if (container.BabyId != babyId || !IsAvailable(container) || container.EstimatedExpiresAt <= Now())
            {
                throw new InvalidOperationException("Un contenant selectionne n'est pas disponible.");
            }

            if (source.QuantityMl <= 0 || source.QuantityMl > container.RemainingQuantityMl)
            {
                throw new InvalidOperationException("Un prelevement depasse la quantite restante.");
            }
        }

        foreach (var source in sources)
        {
            var index = state.Containers.FindIndex(container => container.Id == source.ContainerId);
            var container = state.Containers[index];
            var remaining = container.RemainingQuantityMl - source.QuantityMl;
            var status = remaining == 0 ? MilkStatus.Consumed : MilkStatus.PartiallyConsumed;
            state.Containers[index] = container with { RemainingQuantityMl = remaining, Status = status };
            state.StockMovements.Add(new StockMovement(Guid.NewGuid(), container.Id, container.Location, container.Location, container.Status, status, Now(), userId, $"Prelevement de {source.QuantityMl} ml"));
        }

        var bottle = new PreparedBottle(Guid.NewGuid(), babyId, sources.Sum(source => source.QuantityMl), Now(), userId, [.. sources], "prepared", notes.Trim());
        state.PreparedBottles.Add(bottle);
        Audit(state, userId, "prepare", nameof(PreparedBottle), bottle.Id, "", $"{bottle.TotalQuantityMl} ml");
        return bottle;
    }

    public Feeding RecordFeeding(AppState state, Guid userId, Guid babyId, Guid? bottleId, int preparedMl, int consumedMl, FeedingReaction reaction, string leftoverOutcome, string notes)
    {
        var baby = RequireBaby(state, babyId);
        EnsureFamilyAccess(state, userId, baby.FamilyId);
        if (preparedMl <= 0 || consumedMl < 0 || consumedMl > preparedMl)
        {
            throw new InvalidOperationException("La consommation doit rester entre 0 et la quantite preparee.");
        }

        if (bottleId is not null && state.PreparedBottles.All(bottle => bottle.Id != bottleId.Value))
        {
            throw new InvalidOperationException("Le biberon prepare est introuvable.");
        }

        var feeding = new Feeding(
            Guid.NewGuid(),
            babyId,
            bottleId,
            Now(),
            null,
            preparedMl,
            consumedMl,
            preparedMl - consumedMl,
            "lait maternel",
            reaction,
            userId,
            leftoverOutcome.Trim(),
            notes.Trim());

        state.Feedings.Add(feeding);
        Audit(state, userId, "feed", nameof(Feeding), feeding.Id, "", $"{consumedMl}/{preparedMl} ml");
        return feeding;
    }

    public StockSummary BuildStockSummary(AppState state, Guid babyId)
    {
        var baby = RequireBaby(state, babyId);
        var today = Now().Date;
        var available = state.Containers
            .Where(container => container.BabyId == baby.Id && IsAvailable(container) && container.EstimatedExpiresAt > Now())
            .OrderBy(container => container.EstimatedExpiresAt)
            .ToList();

        var consumedFeedings = state.Feedings.Where(feeding => feeding.BabyId == baby.Id).ToList();
        var average = consumedFeedings.Count == 0 ? 0 : consumedFeedings.Average(feeding => feeding.ConsumedQuantityMl);

        return new StockSummary(
            available.Sum(container => container.RemainingQuantityMl),
            available.Where(container => container.Location == StorageLocation.Refrigerator).Sum(container => container.RemainingQuantityMl),
            available.Where(container => container.Location is StorageLocation.SeparateFreezer or StorageLocation.FridgeFreezerCompartment).Sum(container => container.RemainingQuantityMl),
            available.Where(container => container.Status == MilkStatus.Thawing).Sum(container => container.RemainingQuantityMl),
            available.Where(container => container.EstimatedExpiresAt <= Now().AddHours(24)).Sum(container => container.RemainingQuantityMl),
            baby.UsualBottleMl <= 0 ? 0 : available.Sum(container => container.RemainingQuantityMl) / baby.UsualBottleMl,
            state.PumpingSessions.Where(session => session.BabyId == baby.Id && session.PumpedAt.Date == today).Sum(session => session.TotalQuantityMl),
            consumedFeedings.Where(feeding => feeding.StartedAt.Date == today).Sum(feeding => feeding.ConsumedQuantityMl),
            state.StockMovements.Where(move => move.NewStatus == MilkStatus.Discarded && move.OccurredAt.Date == today).Sum(_ => 0),
            available.FirstOrDefault(),
            state.PumpingSessions.Where(session => session.BabyId == baby.Id).OrderByDescending(session => session.PumpedAt).FirstOrDefault(),
            consumedFeedings.OrderByDescending(feeding => feeding.StartedAt).FirstOrDefault(),
            Math.Round((decimal)average, 1));
    }

    public IReadOnlyList<MilkContainer> AvailableContainers(AppState state, Guid babyId)
    {
        return state.Containers
            .Where(container => container.BabyId == babyId && IsAvailable(container) && container.EstimatedExpiresAt > Now())
            .OrderBy(container => container.EstimatedExpiresAt)
            .ToList();
    }

    public void SeedDemo(AppState state, string passwordHash)
    {
        if (state.Users.Count > 0)
        {
            return;
        }

        var user = RegisterUser(state, "Youssef", "Demo", "demo@vectis.local", passwordHash);
        var family = CreateFamily(state, user.Id, "Famille Demo");
        var baby = CreateBaby(state, user.Id, family.Id, "Adam", DateOnly.FromDateTime(Now().AddMonths(-4).Date), 120, "Donnees fictives pour tester le MVP.");
        AddPumpingSession(state, user.Id, baby.Id, Now().AddHours(-8), 150, 20, "both", "Tirage de demonstration",
        [
            new(ContainerType.StorageBag, 90, StorageLocation.Refrigerator, "A utiliser rapidement"),
            new(ContainerType.Bottle, 60, StorageLocation.Refrigerator, "")
        ]);
        AddPumpingSession(state, user.Id, baby.Id, Now().AddDays(-10), 200, 25, "both", "Reserve congelee",
        [
            new(ContainerType.StorageBag, 100, StorageLocation.SeparateFreezer, ""),
            new(ContainerType.StorageBag, 100, StorageLocation.SeparateFreezer, "")
        ]);
    }

    private DateTimeOffset EstimateExpiration(AppState state, DateTimeOffset from, StorageLocation location)
    {
        var rule = state.ConservationRules.FirstOrDefault(rule => rule.Location == location && rule.IsActive)
            ?? throw new InvalidOperationException("Regle de conservation manquante.");
        return from.AddHours(rule.DurationHours);
    }

    private static bool IsAvailable(MilkContainer container)
    {
        return container.RemainingQuantityMl > 0 && container.Status is not (MilkStatus.Consumed or MilkStatus.Discarded or MilkStatus.Expired);
    }

    private static AppUser RequireUser(AppState state, Guid userId)
    {
        return state.Users.FirstOrDefault(user => user.Id == userId)
            ?? throw new InvalidOperationException("Utilisateur introuvable.");
    }

    private static Baby RequireBaby(AppState state, Guid babyId)
    {
        return state.Babies.FirstOrDefault(baby => baby.Id == babyId && baby.IsActive)
            ?? throw new InvalidOperationException("Bebe introuvable.");
    }

    private static MilkContainer RequireContainer(AppState state, Guid containerId)
    {
        return state.Containers.FirstOrDefault(container => container.Id == containerId)
            ?? throw new InvalidOperationException("Contenant introuvable.");
    }

    private static void EnsureFamilyAccess(AppState state, Guid userId, Guid familyId)
    {
        if (state.Members.All(member => member.UserId != userId || member.FamilyId != familyId || member.Status != "accepted"))
        {
            throw new InvalidOperationException("Acces refuse a cette famille.");
        }
    }

    private void Audit(AppState state, Guid userId, string action, string entityName, Guid entityId, string oldValue, string newValue)
    {
        state.AuditEntries.Add(new AuditEntry(Guid.NewGuid(), userId, action, entityName, entityId, oldValue, newValue, Now()));
    }

    private DateTimeOffset Now() => _clock.GetUtcNow();
}
