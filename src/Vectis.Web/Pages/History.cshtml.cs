using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vectis.Domain;
using Vectis.Web.Services;

namespace Vectis.Web.Pages;

public sealed record HistoryFilter(string Key, string Label, int Count);
public sealed record HistoryEvent(
    string Kind,
    string KindLabel,
    string Title,
    string Detail,
    string Meta,
    string VolumeLabel,
    string Severity,
    DateTimeOffset OccurredAt);

[Authorize]
public sealed class HistoryModel : PageModel
{
    private readonly CurrentUser _currentUser;
    private readonly HistoryService _historyService;

    public HistoryModel(CurrentUser currentUser, HistoryService historyService)
    {
        _currentUser = currentUser;
        _historyService = historyService;
    }

    public IReadOnlyList<PumpingSession> PumpingSessions { get; private set; } = [];
    public IReadOnlyList<PreparedBottle> PreparedBottles { get; private set; } = [];
    public IReadOnlyList<Feeding> Feedings { get; private set; } = [];
    public IReadOnlyList<AuditEntry> AuditEntries { get; private set; } = [];
    public IReadOnlyList<HistoryEvent> Events { get; private set; } = [];
    public IReadOnlyList<HistoryFilter> Filters { get; private set; } = [];
    public string ActiveFilter { get; private set; } = "all";
    public int PumpedTodayMl { get; private set; }
    public int ConsumedTodayMl { get; private set; }
    public int PreparedTodayMl { get; private set; }
    public int PumpedSevenDaysMl { get; private set; }
    public int ConsumedSevenDaysMl { get; private set; }
    public bool BottleSaved { get; private set; }
    public bool FeedingSaved { get; private set; }

    public async Task<IActionResult> OnGetAsync(string filter = "all")
    {
        var context = await _currentUser.GetAsync();
        if (context?.Baby is null)
        {
            return RedirectToPage("/Account/Login");
        }

        BottleSaved = Request.Query["saved"] == "bottle";
        FeedingSaved = Request.Query["saved"] == "feeding";
        var history = await _historyService.GetAsync(context.User.Id, context.Baby.Id);
        PumpingSessions = history.PumpingSessions;
        PreparedBottles = history.PreparedBottles;
        Feedings = history.Feedings;
        AuditEntries = history.AuditEntries;
        ActiveFilter = NormalizeFilter(filter);
        Events = BuildEvents(history)
            .Where(item => ActiveFilter == "all" || item.Kind == ActiveFilter)
            .OrderByDescending(item => item.OccurredAt)
            .ToList();
        Filters = BuildFilters(history);
        SetSummary(history);
        return Page();
    }

    private void SetSummary(HistorySnapshot history)
    {
        var today = DateTimeOffset.Now.Date;
        var sevenDaysAgo = DateTimeOffset.Now.AddDays(-6).Date;
        PumpedTodayMl = history.PumpingSessions.Where(item => item.PumpedAt.ToLocalTime().Date == today).Sum(item => item.TotalQuantityMl);
        ConsumedTodayMl = history.Feedings.Where(item => item.StartedAt.ToLocalTime().Date == today).Sum(item => item.ConsumedQuantityMl);
        PreparedTodayMl = history.PreparedBottles.Where(item => item.PreparedAt.ToLocalTime().Date == today).Sum(item => item.TotalQuantityMl);
        PumpedSevenDaysMl = history.PumpingSessions.Where(item => item.PumpedAt.ToLocalTime().Date >= sevenDaysAgo).Sum(item => item.TotalQuantityMl);
        ConsumedSevenDaysMl = history.Feedings.Where(item => item.StartedAt.ToLocalTime().Date >= sevenDaysAgo).Sum(item => item.ConsumedQuantityMl);
    }

    private static string NormalizeFilter(string filter)
    {
        return filter is "pumping" or "bottle" or "feeding" or "audit" ? filter : "all";
    }

    private static IReadOnlyList<HistoryFilter> BuildFilters(HistorySnapshot history)
    {
        return
        [
            new("all", "Tout", history.PumpingSessions.Count + history.PreparedBottles.Count + history.Feedings.Count + history.AuditEntries.Count),
            new("pumping", "Tirages", history.PumpingSessions.Count),
            new("bottle", "Biberons prepares", history.PreparedBottles.Count),
            new("feeding", "Consommations", history.Feedings.Count),
            new("audit", "Audit", history.AuditEntries.Count)
        ];
    }

    private static IReadOnlyList<HistoryEvent> BuildEvents(HistorySnapshot history)
    {
        var events = new List<HistoryEvent>();
        events.AddRange(history.PumpingSessions.Select(item => new HistoryEvent(
            "pumping",
            "Tirage",
            $"{item.TotalQuantityMl} ml tires",
            string.IsNullOrWhiteSpace(item.Notes) ? "Tirage enregistre" : item.Notes,
            item.DurationMinutes is null ? item.PumpedAt.LocalDateTime.ToString("g") : $"{item.DurationMinutes} min - {item.PumpedAt.LocalDateTime:g}",
            $"+{item.TotalQuantityMl} ml",
            "ok",
            item.PumpedAt)));

        events.AddRange(history.PreparedBottles.Select(item => new HistoryEvent(
            "bottle",
            "Biberon",
            $"{item.TotalQuantityMl} ml prepares",
            $"{item.Sources.Count} source(s) - {DisplayLabels.BottleStatus(item.Status)}",
            item.PreparedAt.LocalDateTime.ToString("g"),
            $"-{item.TotalQuantityMl} ml stock",
            item.Status == "prepared" ? "warning" : "ok",
            item.PreparedAt)));

        events.AddRange(history.Feedings.Select(item => new HistoryEvent(
            "feeding",
            "Consommation",
            $"{item.ConsumedQuantityMl} ml bus",
            $"Prepare {item.PreparedQuantityMl} ml - reste {item.LeftoverQuantityMl} ml {item.LeftoverOutcome}",
            $"{DisplayLabels.FeedingReaction(item.Reaction)} - {item.StartedAt.LocalDateTime:g}",
            $"{item.ConsumedQuantityMl}/{item.PreparedQuantityMl} ml",
            item.LeftoverQuantityMl > 0 ? "warning" : "ok",
            item.StartedAt)));

        events.AddRange(history.AuditEntries.Select(item => new HistoryEvent(
            "audit",
            "Audit",
            $"{item.Action} {item.EntityName}",
            item.NewValue,
            item.OccurredAt.LocalDateTime.ToString("g"),
            "",
            "info",
            item.OccurredAt)));

        return events;
    }
}
