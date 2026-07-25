using Vectis.Domain;

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
        ExpiredContainerIsNotRecommended();
        FeedingTracksLeftover();
        FamilyAccessIsIsolated();
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
