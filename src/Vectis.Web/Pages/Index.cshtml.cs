using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vectis.Domain;
using Vectis.Web.Services;

namespace Vectis.Web.Pages;

public sealed record DashboardDay(string Label, int PumpedMl, int ConsumedMl, int Feedings);
public sealed record DashboardAlert(string Severity, string Title, string Message, string ActionLabel, string Href);
public sealed record DashboardAction(string Severity, string Eyebrow, string Title, string Message, string ActionLabel, string Href, string IconId);

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly CurrentUser _currentUser;
    private readonly StockService _stockService;
    private readonly HistoryService _historyService;
    private readonly FamilyService _familyService;
    private readonly BottleService _bottleService;

    public IndexModel(CurrentUser currentUser, StockService stockService, HistoryService historyService, FamilyService familyService, BottleService bottleService)
    {
        _currentUser = currentUser;
        _stockService = stockService;
        _historyService = historyService;
        _familyService = familyService;
        _bottleService = bottleService;
    }

    public StockSummary? Summary { get; private set; }
    public string BabyName { get; private set; } = "";
    public int MemberCount { get; private set; }
    public int PendingInvitationCount { get; private set; }
    public int PendingBottleCount { get; private set; }
    public int FeedingCountToday { get; private set; }
    public IReadOnlyList<PreparedBottle> PendingBottles { get; private set; } = [];
    public IReadOnlyList<DashboardAlert> Alerts { get; private set; } = [];
    public IReadOnlyList<Feeding> RecentFeedings { get; private set; } = [];
    public IReadOnlyList<PumpingSession> RecentPumpingSessions { get; private set; } = [];
    public IReadOnlyList<DashboardDay> DailyTrend { get; private set; } = [];
    public DashboardAction? RecommendedAction { get; private set; }
    public string NextContainerName { get; private set; } = "Aucun contenant";
    public string NextContainerLabel { get; private set; } = "Aucun";
    public string NextContainerExpiresLabel { get; private set; } = "-";
    public int IntakeBalanceTodayMl { get; private set; }
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
        var availableContainers = await _stockService.GetAvailableContainersAsync(context.User.Id, context.Baby.Id);
        var history = await _historyService.GetAsync(context.User.Id, context.Baby.Id);
        var users = await _familyService.GetUsersAsync(context.Family.Id, context.User.Id);
        PendingBottles = await _bottleService.GetPendingAsync(context.User.Id, context.Baby.Id);
        var today = DateTimeOffset.Now.Date;

        MemberCount = users.Members.Count;
        PendingInvitationCount = users.Invitations.Count(invitation => invitation.Status == "pending");
        PendingBottleCount = PendingBottles.Count;
        FeedingCountToday = history.Feedings.Count(feeding => feeding.StartedAt.ToLocalTime().Date == today);
        RecentFeedings = history.Feedings.Take(5).ToList();
        RecentPumpingSessions = history.PumpingSessions.Take(5).ToList();
        DailyTrend = BuildDailyTrend(history, today);
        MaxDailyVolumeMl = Math.Max(1, DailyTrend.Max(day => Math.Max(day.PumpedMl, day.ConsumedMl)));
        IntakeBalanceTodayMl = Summary.PumpedTodayMl - Summary.ConsumedTodayMl;
        SetStockDistribution(Summary);
        SetStockHealth(Summary);
        SetNextContainer(Summary);
        Alerts = BuildAlerts(Summary, availableContainers, PendingBottles, PendingInvitationCount, context.Baby);
        RecommendedAction = BuildRecommendedAction(Summary, PendingBottles, PendingInvitationCount, context.Baby);
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

    private void SetNextContainer(StockSummary summary)
    {
        if (summary.NextRecommended is null)
        {
            return;
        }

        NextContainerName = "Contenant prioritaire";
        NextContainerLabel = $"{DisplayLabels.Location(summary.NextRecommended.Location)} - {summary.NextRecommended.RemainingQuantityMl} ml";
        NextContainerExpiresLabel = summary.NextRecommended.EstimatedExpiresAt.LocalDateTime.ToString("g");
    }

    private static DashboardAction BuildRecommendedAction(
        StockSummary summary,
        IReadOnlyList<PreparedBottle> pendingBottles,
        int pendingInvitationCount,
        Baby baby)
    {
        var now = DateTimeOffset.UtcNow;
        var oldBottle = pendingBottles.FirstOrDefault(bottle => now - bottle.PreparedAt >= TimeSpan.FromHours(2));
        if (oldBottle is not null)
        {
            return new DashboardAction(
                "danger",
                "Action urgente",
                "Traiter le biberon en attente",
                $"{oldBottle.TotalQuantityMl} ml ont ete prepares a {oldBottle.PreparedAt.LocalDateTime:g}. Note s'il a ete donne ou jette.",
                "Ouvrir le biberon",
                $"/Feed?prepared={oldBottle.Id}",
                "icon-alert");
        }

        if (pendingBottles.Count > 0)
        {
            var nextBottle = pendingBottles[0];
            return new DashboardAction(
                "info",
                "Pret maintenant",
                "Donner le prochain biberon",
                $"{nextBottle.TotalQuantityMl} ml sont deja prets. Enregistre le repas pour garder le stock juste.",
                "Donner",
                $"/Feed?prepared={nextBottle.Id}",
                "icon-feed");
        }

        if (summary.TotalAvailableMl <= 0)
        {
            return new DashboardAction(
                "danger",
                "Stock vide",
                "Ajouter un tirage",
                $"Aucun lait disponible pour {baby.FirstName}. Le prochain geste utile est d'enregistrer un nouveau tirage.",
                "Ajouter un tirage",
                "/Pumping",
                "icon-pumping");
        }

        if (summary.NextRecommended is not null && summary.NextRecommended.EstimatedExpiresAt <= now.AddHours(24))
        {
            return new DashboardAction(
                "warning",
                "Priorite anti-perte",
                "Preparer avec le lait qui expire",
                $"{summary.NextRecommended.RemainingQuantityMl} ml expirent le {summary.NextRecommended.EstimatedExpiresAt.LocalDateTime:g}.",
                "Preparer",
                "/Bottle",
                "icon-bottle");
        }

        if (summary.EstimatedBottles <= 2 || summary.TotalAvailableMl < baby.UsualBottleMl * 2)
        {
            return new DashboardAction(
                "warning",
                "Anticipation",
                "Renforcer le stock",
                $"Il reste environ {summary.EstimatedBottles} biberon(s). Un tirage maintenant evitera une rupture.",
                "Ajouter un tirage",
                "/Pumping",
                "icon-pumping");
        }

        if (pendingInvitationCount > 0)
        {
            return new DashboardAction(
                "info",
                "Coordination famille",
                "Suivre les invitations",
                $"{pendingInvitationCount} invitation(s) sont encore en attente.",
                "Voir les utilisateurs",
                "/Users",
                "icon-users");
        }

        return new DashboardAction(
            "ok",
            "Situation stable",
            "Continuer le suivi",
            $"Le stock couvre environ {summary.EstimatedBottles} biberon(s). La prochaine preparation peut suivre la priorite d'expiration.",
            "Voir le stock",
            "/Stock",
            "icon-check");
    }

    private static IReadOnlyList<DashboardAlert> BuildAlerts(
        StockSummary summary,
        IReadOnlyList<MilkContainer> availableContainers,
        IReadOnlyList<PreparedBottle> pendingBottles,
        int pendingInvitationCount,
        Baby baby)
    {
        var now = DateTimeOffset.UtcNow;
        var alerts = new List<DashboardAlert>();
        var firstExpiring = availableContainers.FirstOrDefault(container => container.EstimatedExpiresAt <= now.AddHours(24));
        if (firstExpiring is not null)
        {
            alerts.Add(new DashboardAlert(
                "warning",
                "Lait bientot expire",
                $"{summary.ExpiringSoonMl} ml a utiliser avant {firstExpiring.EstimatedExpiresAt.LocalDateTime:g}.",
                "Preparer avec ce stock",
                "/Bottle"));
        }

        if (summary.TotalAvailableMl <= 0)
        {
            alerts.Add(new DashboardAlert("danger", "Stock vide", "Aucun lait disponible pour preparer un biberon.", "Ajouter un tirage", "/Pumping"));
        }
        else if (summary.EstimatedBottles <= 2 || summary.TotalAvailableMl < baby.UsualBottleMl * 2)
        {
            alerts.Add(new DashboardAlert(
                "warning",
                "Stock faible",
                $"Il reste environ {summary.EstimatedBottles} biberon(s) pour {baby.FirstName}.",
                "Ajouter un tirage",
                "/Pumping"));
        }

        var oldBottle = pendingBottles.FirstOrDefault(bottle => now - bottle.PreparedAt >= TimeSpan.FromHours(2));
        if (oldBottle is not null)
        {
            alerts.Add(new DashboardAlert(
                "danger",
                "Biberon prepare depuis trop longtemps",
                $"{oldBottle.TotalQuantityMl} ml prepare a {oldBottle.PreparedAt.LocalDateTime:g}.",
                "Traiter ce biberon",
                $"/Feed?prepared={oldBottle.Id}"));
        }
        else if (pendingBottles.Count > 0)
        {
            var nextBottle = pendingBottles[0];
            alerts.Add(new DashboardAlert(
                "info",
                "Biberon en attente",
                $"{nextBottle.TotalQuantityMl} ml pret pour le prochain repas.",
                "Donner maintenant",
                $"/Feed?prepared={nextBottle.Id}"));
        }

        if (pendingInvitationCount > 0)
        {
            alerts.Add(new DashboardAlert(
                "info",
                "Invitation en attente",
                $"{pendingInvitationCount} personne(s) n'ont pas encore rejoint la famille.",
                "Voir les utilisateurs",
                "/Users"));
        }

        return alerts;
    }

    private static int Percent(int value, int total)
    {
        return total <= 0 ? 0 : Math.Clamp((int)Math.Round(value * 100m / total), 0, 100);
    }
}
