using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vectis.Domain;
using Vectis.Web.Services;

namespace Vectis.Web.Pages;

public sealed record StockFilter(string Key, string Label, int Count);
public sealed record StockRow(
    MilkContainer Container,
    string DisplayName,
    string Priority,
    string PriorityClass,
    string ExpirationLabel,
    string AgeLabel,
    int ExpirationProgress);

[Authorize]
public sealed class StockModel : PageModel
{
    private readonly CurrentUser _currentUser;
    private readonly StockService _stockService;

    public StockModel(CurrentUser currentUser, StockService stockService)
    {
        _currentUser = currentUser;
        _stockService = stockService;
    }

    public IReadOnlyList<MilkContainer> Containers { get; private set; } = [];
    public IReadOnlyList<StockRow> Rows { get; private set; } = [];
    public IReadOnlyList<StockFilter> Filters { get; private set; } = [];
    public StockSummary? Summary { get; private set; }
    public string ActiveFilter { get; private set; } = "all";

    public async Task<IActionResult> OnGetAsync(string filter = "all")
    {
        var context = await _currentUser.GetAsync();
        if (context?.Baby is null)
        {
            return RedirectToPage("/Account/Login");
        }

        Containers = await _stockService.GetAvailableContainersAsync(context.User.Id, context.Baby.Id);
        Summary = await _stockService.GetSummaryAsync(context.User.Id, context.Baby.Id);
        ActiveFilter = NormalizeFilter(filter);
        Filters = BuildFilters(Containers);
        Rows = Containers
            .Where(container => MatchesFilter(container, ActiveFilter))
            .Select((container, index) => BuildRow(container, index + 1))
            .ToList();
        return Page();
    }

    private static string NormalizeFilter(string filter)
    {
        return filter is "urgent" or "soon" or "fridge" or "freezer" ? filter : "all";
    }

    private static IReadOnlyList<StockFilter> BuildFilters(IReadOnlyList<MilkContainer> containers)
    {
        var now = DateTimeOffset.UtcNow;
        return
        [
            new("all", "Tout", containers.Count),
            new("urgent", "A utiliser maintenant", containers.Count(container => container.EstimatedExpiresAt <= now.AddHours(24))),
            new("soon", "Bientot", containers.Count(container => container.EstimatedExpiresAt > now.AddHours(24) && container.EstimatedExpiresAt <= now.AddHours(72))),
            new("fridge", "Refrigerateur", containers.Count(container => container.Location == StorageLocation.Refrigerator)),
            new("freezer", "Congelateur", containers.Count(container => container.Location is StorageLocation.SeparateFreezer or StorageLocation.FridgeFreezerCompartment))
        ];
    }

    private static bool MatchesFilter(MilkContainer container, string filter)
    {
        var now = DateTimeOffset.UtcNow;
        return filter switch
        {
            "urgent" => container.EstimatedExpiresAt <= now.AddHours(24),
            "soon" => container.EstimatedExpiresAt > now.AddHours(24) && container.EstimatedExpiresAt <= now.AddHours(72),
            "fridge" => container.Location == StorageLocation.Refrigerator,
            "freezer" => container.Location is StorageLocation.SeparateFreezer or StorageLocation.FridgeFreezerCompartment,
            _ => true
        };
    }

    private static StockRow BuildRow(MilkContainer container, int position)
    {
        var now = DateTimeOffset.UtcNow;
        var remaining = container.EstimatedExpiresAt - now;
        var total = container.EstimatedExpiresAt - container.PumpedAt;
        var elapsed = now - container.PumpedAt;
        var progress = total.TotalMinutes <= 0
            ? 100
            : Math.Clamp((int)Math.Round(elapsed.TotalMinutes * 100 / total.TotalMinutes), 0, 100);

        var priority = "Stable";
        var priorityClass = "ok";
        if (remaining <= TimeSpan.FromHours(24))
        {
            priority = "A utiliser maintenant";
            priorityClass = "danger";
        }
        else if (remaining <= TimeSpan.FromHours(72))
        {
            priority = "Bientot";
            priorityClass = "warning";
        }

        return new StockRow(container, $"Contenant {position}", priority, priorityClass, FormatRemaining(remaining), FormatAge(elapsed), progress);
    }

    private static string FormatRemaining(TimeSpan remaining)
    {
        if (remaining <= TimeSpan.Zero)
        {
            return "Expire";
        }

        if (remaining.TotalHours < 24)
        {
            return $"{Math.Max(1, (int)Math.Ceiling(remaining.TotalHours))} h restantes";
        }

        return $"{Math.Max(1, (int)Math.Ceiling(remaining.TotalDays))} j restants";
    }

    private static string FormatAge(TimeSpan age)
    {
        if (age.TotalHours < 24)
        {
            return $"Tire il y a {Math.Max(1, (int)Math.Floor(age.TotalHours))} h";
        }

        return $"Tire il y a {Math.Max(1, (int)Math.Floor(age.TotalDays))} j";
    }
}
