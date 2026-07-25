using Vectis.Domain;

namespace Vectis.Web.Services;

public interface IAppStore
{
    Task<AppState> LoadAsync();
    Task MutateAsync(Action<AppState> action);
    Task<T> MutateAsync<T>(Func<AppState, T> action);
}
