using Vectis.Domain;

namespace Vectis.Web.Services;

public sealed class SettingsService
{
    private readonly IAppStore _store;

    public SettingsService(IAppStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyList<ConservationRule>> GetConservationRulesAsync()
    {
        var state = await _store.LoadAsync();
        return state.ConservationRules.OrderBy(rule => rule.Location.ToString()).ToList();
    }

    public Task SaveConservationRulesAsync(IReadOnlyList<ConservationRule> rules)
    {
        return _store.MutateAsync(state => state.ConservationRules = rules.ToList());
    }
}
