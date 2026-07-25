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

    public async Task<HistorySnapshot> GetAsync(Guid userId, Guid babyId)
    {
        var state = await _store.LoadAsync();
        var baby = state.Babies.FirstOrDefault(item => item.Id == babyId && item.IsActive)
            ?? throw new InvalidOperationException("Bebe introuvable.");

        if (state.Members.All(member => member.UserId != userId || member.FamilyId != baby.FamilyId || member.Status != "accepted"))
        {
            throw new InvalidOperationException("Acces refuse a cette famille.");
        }

        var familyUserIds = state.Members
            .Where(member => member.FamilyId == baby.FamilyId && member.Status == "accepted")
            .Select(member => member.UserId)
            .ToHashSet();

        return new HistorySnapshot(
            state.PumpingSessions.Where(item => item.BabyId == babyId).OrderByDescending(item => item.PumpedAt).ToList(),
            state.Feedings.Where(item => item.BabyId == babyId).OrderByDescending(item => item.StartedAt).ToList(),
            state.AuditEntries.Where(item => familyUserIds.Contains(item.UserId)).OrderByDescending(item => item.OccurredAt).Take(30).ToList());
    }
}
