using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vectis.Domain;
using Vectis.Web.Services;

namespace Vectis.Web.Pages;

public sealed record DashboardDay(string Label, int PumpedMl, int ConsumedMl, int Feedings);

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly CurrentUser _currentUser;
    private readonly StockService _stockService;
    private readonly HistoryService _historyService;
    private readonly FamilyService _familyService;

    public IndexModel(CurrentUser currentUser, StockService stockService, HistoryService historyService, FamilyService familyService)
    {
        _currentUser = currentUser;
        _stockService = stockService;
        _historyService = historyService;
        _familyService = familyService;
    }

    public StockSummary? Summary { get; private set; }
    public string BabyName { get; private set; } = "";
    public int MemberCount { get; private set; }
    public int PendingInvitationCount { get; private set; }
    public int FeedingCountToday { get; private set; }
    public IReadOnlyList<Feeding> RecentFeedings { get; private set; } = [];
    public IReadOnlyList<PumpingSession> RecentPumpingSessions { get; private set; } = [];
    public IReadOnlyList<DashboardDay> DailyTrend { get; private set; } = [];
    public int MaxDailyVolumeMl { get; private set; } = 1;
    public int RefrigeratorPercent { get; private set; }
    public int FreezerPercent { get; private set; }
    public int OtherStockPercent { get; private set; }
    public int AutonomyPercent { get; private set; }
    public string StockHealthLabel { get; private set; } = "Stable";
    public string StockHealthClass { get; private set; } = "ok";
    public string StockDistributionStyle =>
        $"--fridge:{RefrigeratorPercent}; --freezer:{FreezerPercent}; --other:{OtherStockPercent};";
    public string AutonomyStyle => $"--progress:{AutonomyPercent};";

    public async Task<IActionResult> OnGetAsync()
    {
        var context = await _currentUser.GetAsync();
        if (context?.Baby is null || context.Family is null)
        {
            return RedirectToPage("/Account/Login");
        }

        BabyName = context.Baby.FirstName;
        Summary = await _stockService.GetSummaryAsync(context.User.Id, context.Baby.Id);
        var history = await _historyService.GetAsync(context.User.Id, context.Baby.Id);
        var users = await _familyService.GetUsersAsync(context.Family.Id, context.User.Id);
        var today = DateTimeOffset.Now.Date;

        MemberCount = users.Members.Count;
        PendingInvitationCount = users.Invitations.Count(invitation => invitation.Status == "pending");
        FeedingCountToday = history.Feedings.Count(feeding => feeding.StartedAt.ToLocalTime().Date == today);
        RecentFeedings = history.Feedings.Take(5).ToList();
        RecentPumpingSessions = history.PumpingSessions.Take(5).ToList();
        DailyTrend = BuildDailyTrend(history, today);
        MaxDailyVolumeMl = Math.Max(1, DailyTrend.Max(day => Math.Max(day.PumpedMl, day.ConsumedMl)));
        SetStockDistribution(Summary);
        SetStockHealth(Summary);
        return Page();
    }

    public int BarHeight(int value)
    {
        return Math.Clamp((int)Math.Round(value * 100m / MaxDailyVolumeMl), 4, 100);
    }

    private static IReadOnlyList<DashboardDay> BuildDailyTrend(HistorySnapshot history, DateTime today)
    {
        return Enumerable.Range(0, 7)
            .Select(offset => today.AddDays(offset - 6))
            .Select(day =>
            {
                var pumped = history.PumpingSessions
                    .Where(session => session.PumpedAt.ToLocalTime().Date == day)
                    .Sum(session => session.TotalQuantityMl);
                var consumed = history.Feedings
                    .Where(feeding => feeding.StartedAt.ToLocalTime().Date == day)
                    .Sum(feeding => feeding.ConsumedQuantityMl);
                var feedings = history.Feedings.Count(feeding => feeding.StartedAt.ToLocalTime().Date == day);

                return new DashboardDay(day.ToString("dd/MM"), pumped, consumed, feedings);
            })
            .ToList();
    }

    private void SetStockDistribution(StockSummary summary)
    {
        if (summary.TotalAvailableMl <= 0)
        {
            RefrigeratorPercent = 0;
            FreezerPercent = 0;
            OtherStockPercent = 100;
            AutonomyPercent = 0;
            return;
        }

        RefrigeratorPercent = Percent(summary.RefrigeratorMl, summary.TotalAvailableMl);
        FreezerPercent = Percent(summary.FreezerMl, summary.TotalAvailableMl);
        OtherStockPercent = Math.Max(0, 100 - RefrigeratorPercent - FreezerPercent);
        AutonomyPercent = Math.Clamp(summary.EstimatedBottles * 12, 0, 100);
    }

    private void SetStockHealth(StockSummary summary)
    {
        if (summary.TotalAvailableMl <= 0)
        {
            StockHealthLabel = "Stock vide";
            StockHealthClass = "danger";
            return;
        }

        if (summary.ExpiringSoonMl > 0)
        {
            StockHealthLabel = "A utiliser vite";
            StockHealthClass = "warning";
            return;
        }

        StockHealthLabel = "Stable";
        StockHealthClass = "ok";
    }

    private static int Percent(int value, int total)
    {
        return total <= 0 ? 0 : Math.Clamp((int)Math.Round(value * 100m / total), 0, 100);
    }
}
