using Vectis.Domain;

namespace Vectis.Web.Services;

public sealed record HistorySnapshot(
    IReadOnlyList<PumpingSession> PumpingSessions,
    IReadOnlyList<Feeding> Feedings,
    IReadOnlyList<AuditEntry> AuditEntries);

public sealed class HistoryService
{
    private readonly IAppStore _store;

    public HistoryService(IAppStore store)
    {
        _store = store;
    }

    public async Task<HistorySnapshot> GetAsync(Guid babyId)
    {
        var state = await _store.LoadAsync();
        return new HistorySnapshot(
            state.PumpingSessions.Where(item => item.BabyId == babyId).OrderByDescending(item => item.PumpedAt).ToList(),
            state.Feedings.Where(item => item.BabyId == babyId).OrderByDescending(item => item.StartedAt).ToList(),
            state.AuditEntries.OrderByDescending(item => item.OccurredAt).Take(30).ToList());
    }
}
