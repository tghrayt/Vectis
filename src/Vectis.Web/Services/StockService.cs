using Vectis.Domain;

namespace Vectis.Web.Services;

public sealed record MoveStockContainerCommand(Guid UserId, Guid BabyId, Guid ContainerId, StorageLocation Location, string Comment);
public sealed record AdjustStockContainerCommand(Guid UserId, Guid BabyId, Guid ContainerId, int RemainingQuantityMl, string Comment);
public sealed record MarkStockContainerCommand(Guid UserId, Guid BabyId, Guid ContainerId, MilkStatus Status, string Comment);

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

    public Task<MilkContainer> MoveContainerAsync(MoveStockContainerCommand command)
    {
        return _store.MutateAsync(state => _engine.MoveContainer(state, command.UserId, command.BabyId, command.ContainerId, command.Location, command.Comment));
    }

    public Task<MilkContainer> AdjustContainerAsync(AdjustStockContainerCommand command)
    {
        return _store.MutateAsync(state => _engine.AdjustContainerQuantity(state, command.UserId, command.BabyId, command.ContainerId, command.RemainingQuantityMl, command.Comment));
    }

    public Task<MilkContainer> MarkContainerAsync(MarkStockContainerCommand command)
    {
        return _store.MutateAsync(state => _engine.MarkContainerStatus(state, command.UserId, command.BabyId, command.ContainerId, command.Status, command.Comment));
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
