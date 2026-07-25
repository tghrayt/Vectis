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

    public async Task<StockSummary> GetSummaryAsync(Guid babyId)
    {
        var state = await _store.LoadAsync();
        return _engine.BuildStockSummary(state, babyId);
    }

    public async Task<IReadOnlyList<MilkContainer>> GetAvailableContainersAsync(Guid babyId)
    {
        var state = await _store.LoadAsync();
        return _engine.AvailableContainers(state, babyId);
    }
}
