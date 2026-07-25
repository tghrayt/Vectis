using Vectis.Domain;

namespace Vectis.Web.Services;

public sealed record AddPumpingCommand(
    Guid UserId,
    Guid BabyId,
    DateTime PumpedAt,
    int TotalMl,
    int? DurationMinutes,
    string? Side,
    string Notes,
    IReadOnlyList<ContainerDraft> Containers);

public sealed class PumpingService
{
    private readonly IAppStore _store;
    private readonly VectisEngine _engine;

    public PumpingService(IAppStore store, VectisEngine engine)
    {
        _store = store;
        _engine = engine;
    }

    public Task AddAsync(AddPumpingCommand command)
    {
        return _store.MutateAsync(state =>
        {
            _engine.AddPumpingSession(
                state,
                command.UserId,
                command.BabyId,
                new DateTimeOffset(command.PumpedAt).ToUniversalTime(),
                command.TotalMl,
                command.DurationMinutes,
                command.Side,
                command.Notes,
                command.Containers);
        });
    }
}
