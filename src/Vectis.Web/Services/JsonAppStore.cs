using System.Text.Json;
using Vectis.Domain;

namespace Vectis.Web.Services;

public sealed class JsonAppStore : IAppStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _path;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonAppStore(IWebHostEnvironment environment)
    {
        var directory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "vectis-data.json");
    }

    public async Task<AppState> LoadAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return await LoadUnsafeAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task MutateAsync(Action<AppState> action)
    {
        await _lock.WaitAsync();
        try
        {
            var state = await LoadUnsafeAsync();
            action(state);
            await SaveUnsafeAsync(state);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<T> MutateAsync<T>(Func<AppState, T> action)
    {
        await _lock.WaitAsync();
        try
        {
            var state = await LoadUnsafeAsync();
            var result = action(state);
            await SaveUnsafeAsync(state);
            return result;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<AppState> LoadUnsafeAsync()
    {
        if (!File.Exists(_path))
        {
            return new AppState();
        }

        await using var stream = File.OpenRead(_path);
        return await JsonSerializer.DeserializeAsync<AppState>(stream, Options) ?? new AppState();
    }

    private async Task SaveUnsafeAsync(AppState state)
    {
        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, state, Options);
    }
}
