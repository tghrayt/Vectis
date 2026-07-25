using Vectis.Domain;

namespace Vectis.Web.Services;

public sealed record PrepareAndFeedCommand(
    Guid UserId,
    Guid BabyId,
    IReadOnlyList<PreparedBottleSource> Sources,
    int ConsumedMl,
    FeedingReaction Reaction,
    string LeftoverOutcome,
    string Notes);

public sealed class BottleService
{
    private readonly IAppStore _store;
    private readonly VectisEngine _engine;

    public BottleService(IAppStore store, VectisEngine engine)
    {
        _store = store;
        _engine = engine;
    }

    public Task PrepareAndFeedAsync(PrepareAndFeedCommand command)
    {
        return _store.MutateAsync(state =>
        {
            var bottle = _engine.PrepareBottle(state, command.UserId, command.BabyId, command.Sources, command.Notes);
            _engine.RecordFeeding(state, command.UserId, command.BabyId, bottle.Id, bottle.TotalQuantityMl, command.ConsumedMl, command.Reaction, command.LeftoverOutcome, command.Notes);
        });
    }
}
