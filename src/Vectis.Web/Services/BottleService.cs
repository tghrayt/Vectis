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

public sealed record PrepareBottleCommand(
    Guid UserId,
    Guid BabyId,
    IReadOnlyList<PreparedBottleSource> Sources,
    string Notes);

public sealed record FeedPreparedBottleCommand(
    Guid UserId,
    Guid BabyId,
    Guid BottleId,
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

    public Task<PreparedBottle> PrepareAsync(PrepareBottleCommand command)
    {
        return _store.MutateAsync(state =>
            _engine.PrepareBottle(state, command.UserId, command.BabyId, command.Sources, command.Notes));
    }

    public Task FeedAsync(FeedPreparedBottleCommand command)
    {
        return _store.MutateAsync(state =>
        {
            var bottle = state.PreparedBottles.FirstOrDefault(item => item.Id == command.BottleId && item.BabyId == command.BabyId)
                ?? throw new InvalidOperationException("Biberon introuvable.");

            _engine.RecordFeeding(state, command.UserId, command.BabyId, bottle.Id, bottle.TotalQuantityMl, command.ConsumedMl, command.Reaction, command.LeftoverOutcome, command.Notes);
        });
    }

    public async Task<IReadOnlyList<PreparedBottle>> GetPendingAsync(Guid userId, Guid babyId)
    {
        var state = await _store.LoadAsync();
        var baby = state.Babies.FirstOrDefault(item => item.Id == babyId && item.IsActive)
            ?? throw new InvalidOperationException("Bebe introuvable.");

        if (state.Members.All(member => member.UserId != userId || member.FamilyId != baby.FamilyId || member.Status != "accepted"))
        {
            throw new InvalidOperationException("Acces refuse a cette famille.");
        }

        return state.PreparedBottles
            .Where(item => item.BabyId == babyId && item.Status == "prepared")
            .OrderBy(item => item.PreparedAt)
            .ToList();
    }
}
