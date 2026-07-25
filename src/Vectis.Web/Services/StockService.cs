using Vectis.Domain;

namespace Vectis.Web.Services;

public sealed class StockService
{
    private readonly IAppStore _store;
    private readonly VectisEngine _engine;

    public StockService(IAppStore store, VectisEngine engine)
    {
        _store = store;
        _engine = engine;
    }

    public async Task<StockSummary> GetSummaryAsync(Guid userId, Guid babyId)
    {
        var state = await _store.LoadAsync();
        EnsureBabyAccess(state, userId, babyId);
        return _engine.BuildStockSummary(state, babyId);
    }

    public async Task<IReadOnlyList<MilkContainer>> GetAvailableContainersAsync(Guid userId, Guid babyId)
    {
        var state = await _store.LoadAsync();
        EnsureBabyAccess(state, userId, babyId);
        return _engine.AvailableContainers(state, babyId);
    }

    private static void EnsureBabyAccess(AppState state, Guid userId, Guid babyId)
    {
        var baby = state.Babies.FirstOrDefault(item => item.Id == babyId && item.IsActive)
            ?? throw new InvalidOperationException("Bebe introuvable.");

        if (state.Members.All(member => member.UserId != userId || member.FamilyId != baby.FamilyId || member.Status != "accepted"))
        {
            throw new InvalidOperationException("Acces refuse a cette famille.");
        }
    }
}
