using Vectis.Domain;

namespace Vectis.Web.Services;

public sealed record NotificationAlert(
    NotificationKind Kind,
    string Title,
    string Message,
    string Subject,
    string Body,
    string Severity,
    bool Enabled);

public sealed record NotificationOverview(
    NotificationPreferences Preferences,
    IReadOnlyList<NotificationAlert> Alerts,
    IReadOnlyList<NotificationDelivery> RecentDeliveries);

public sealed record NotificationSendSummary(int Sent, int Skipped, int Failed);

public sealed class NotificationService
{
    private static readonly TimeSpan SendCooldown = TimeSpan.FromHours(6);

    private readonly IAppStore _store;
    private readonly VectisEngine _engine;
    private readonly EmailService _emailService;
    private readonly TimeProvider _clock;

    public NotificationService(IAppStore store, VectisEngine engine, EmailService emailService, TimeProvider clock)
    {
        _store = store;
        _engine = engine;
        _emailService = emailService;
        _clock = clock;
    }

    public async Task<NotificationOverview> GetOverviewAsync(Guid userId, Guid familyId)
    {
        var state = await _store.LoadAsync();
        EnsureFamilyAccess(state, userId, familyId);
        var preferences = EnsurePreferences(state, familyId);
        var alerts = BuildAlerts(state, familyId, preferences);
        var recent = state.NotificationDeliveries
            .Where(delivery => delivery.FamilyId == familyId)
            .OrderByDescending(delivery => delivery.CreatedAt)
            .Take(12)
            .ToList();

        return new NotificationOverview(preferences, alerts, recent);
    }

    public Task SavePreferencesAsync(Guid userId, Guid familyId, NotificationPreferences preferences)
    {
        return _store.MutateAsync(state =>
        {
            EnsureFamilyAdmin(state, userId, familyId);
            var index = state.NotificationPreferences.FindIndex(item => item.FamilyId == familyId);
            var clean = preferences with
            {
                FamilyId = familyId,
                StockLowBottleThreshold = Math.Clamp(preferences.StockLowBottleThreshold, 0, 50),
                ExpiringSoonHours = Math.Clamp(preferences.ExpiringSoonHours, 1, 240),
                PreparedBottleAgeMinutes = Math.Clamp(preferences.PreparedBottleAgeMinutes, 15, 1440)
            };

            if (index < 0)
            {
                state.NotificationPreferences.Add(clean);
                return;
            }

            state.NotificationPreferences[index] = clean;
        });
    }

    public async Task<NotificationSendSummary> SendNowAsync(Guid userId, Guid familyId)
    {
        var pendingSends = new List<(string Email, NotificationAlert Alert)>();
        var skippedDeliveries = new List<NotificationDelivery>();
        var now = _clock.GetUtcNow();

        var state = await _store.LoadAsync();
        EnsureFamilyAdmin(state, userId, familyId);
        var preferences = EnsurePreferences(state, familyId);
        var alerts = BuildAlerts(state, familyId, preferences).Where(alert => alert.Enabled).ToList();
        var recipients = state.Members
            .Where(member => member.FamilyId == familyId && member.Status == "accepted")
            .Join(state.Users, member => member.UserId, user => user.Id, (_, user) => user.Email)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var alert in alerts)
        {
            var recentlySent = state.NotificationDeliveries.Any(delivery =>
                delivery.FamilyId == familyId &&
                delivery.Kind == alert.Kind &&
                delivery.Status == "sent" &&
                delivery.SentAt is not null &&
                now - delivery.SentAt.Value < SendCooldown);

            if (recentlySent)
            {
                skippedDeliveries.Add(NewDelivery(familyId, alert, "", "ignored", "Deja envoye recemment.", now, null));
                continue;
            }

            pendingSends.AddRange(recipients.Select(email => (email, alert)));
        }

        var sentDeliveries = new List<NotificationDelivery>();
        foreach (var (email, alert) in pendingSends)
        {
            var result = await _emailService.SendAsync(email, alert.Subject, alert.Body);
            sentDeliveries.Add(NewDelivery(
                familyId,
                alert,
                email,
                result.Sent ? "sent" : "failed",
                result.Message,
                now,
                result.Sent ? now : null));
        }

        await _store.MutateAsync(nextState =>
        {
            EnsureFamilyAdmin(nextState, userId, familyId);
            nextState.NotificationDeliveries.AddRange(skippedDeliveries);
            nextState.NotificationDeliveries.AddRange(sentDeliveries);
        });

        return new NotificationSendSummary(
            sentDeliveries.Count(delivery => delivery.Status == "sent"),
            skippedDeliveries.Count,
            sentDeliveries.Count(delivery => delivery.Status == "failed"));
    }

    private IReadOnlyList<NotificationAlert> BuildAlerts(AppState state, Guid familyId, NotificationPreferences preferences)
    {
        var baby = state.Babies.FirstOrDefault(item => item.FamilyId == familyId && item.IsActive)
            ?? throw new InvalidOperationException("Bebe introuvable.");
        var family = state.Families.FirstOrDefault(item => item.Id == familyId)
            ?? throw new InvalidOperationException("Famille introuvable.");
        var summary = _engine.BuildStockSummary(state, baby.Id);
        var now = _clock.GetUtcNow();
        var alerts = new List<NotificationAlert>();
        var expiringSoonMl = state.Containers
            .Where(container =>
                container.BabyId == baby.Id &&
                container.RemainingQuantityMl > 0 &&
                container.Status is not (MilkStatus.Consumed or MilkStatus.Discarded or MilkStatus.Expired) &&
                container.EstimatedExpiresAt > now &&
                container.EstimatedExpiresAt <= now.AddHours(preferences.ExpiringSoonHours))
            .Sum(container => container.RemainingQuantityMl);

        if (summary.EstimatedBottles <= preferences.StockLowBottleThreshold)
        {
            alerts.Add(new NotificationAlert(
                NotificationKind.StockLow,
                "Stock faible",
                $"Il reste environ {summary.EstimatedBottles} biberon(s) pour {baby.FirstName}.",
                "Vectis - Stock faible",
                $"Bonjour,\n\nLe stock de {family.Name} est bas : environ {summary.EstimatedBottles} biberon(s) restant(s) pour {baby.FirstName}.\n\nStock disponible : {summary.TotalAvailableMl} ml.\n\nVectis",
                "warning",
                preferences.StockLowEnabled));
        }

        if (expiringSoonMl > 0)
        {
            alerts.Add(new NotificationAlert(
                NotificationKind.ExpiringSoon,
                "Lait bientot expire",
                $"{expiringSoonMl} ml arrive a expiration dans les {preferences.ExpiringSoonHours} prochaines heures.",
                "Vectis - Lait bientot expire",
                $"Bonjour,\n\n{expiringSoonMl} ml de lait pour {baby.FirstName} arrive bientot a expiration.\n\nPense a verifier le stock ou preparer un biberon.\n\nVectis",
                "warning",
                preferences.ExpiringSoonEnabled));
        }

        var oldBottle = state.PreparedBottles
            .Where(bottle => bottle.BabyId == baby.Id && bottle.Status == "prepared")
            .OrderBy(bottle => bottle.PreparedAt)
            .FirstOrDefault(bottle => now - bottle.PreparedAt >= TimeSpan.FromMinutes(preferences.PreparedBottleAgeMinutes));
        if (oldBottle is not null)
        {
            alerts.Add(new NotificationAlert(
                NotificationKind.PreparedBottleAging,
                "Biberon prepare depuis trop longtemps",
                $"{oldBottle.TotalQuantityMl} ml prepare a {oldBottle.PreparedAt.LocalDateTime:g}.",
                "Vectis - Biberon en attente",
                $"Bonjour,\n\nUn biberon de {oldBottle.TotalQuantityMl} ml pour {baby.FirstName} est en attente depuis {oldBottle.PreparedAt.LocalDateTime:g}.\n\nPense a l'enregistrer comme donne ou non consomme.\n\nVectis",
                "danger",
                preferences.PreparedBottleAgingEnabled));
        }

        return alerts;
    }

    private static NotificationPreferences EnsurePreferences(AppState state, Guid familyId)
    {
        var preferences = state.NotificationPreferences.FirstOrDefault(item => item.FamilyId == familyId);
        if (preferences is not null)
        {
            return preferences;
        }

        return new NotificationPreferences(familyId, true, true, true, 2, 24, 120);
    }

    private static NotificationDelivery NewDelivery(Guid familyId, NotificationAlert alert, string email, string status, string message, DateTimeOffset createdAt, DateTimeOffset? sentAt)
    {
        return new NotificationDelivery(Guid.NewGuid(), familyId, alert.Kind, email, alert.Subject, status, message, createdAt, sentAt);
    }

    private static void EnsureFamilyAccess(AppState state, Guid userId, Guid familyId)
    {
        if (state.Members.All(member => member.UserId != userId || member.FamilyId != familyId || member.Status != "accepted"))
        {
            throw new InvalidOperationException("Acces refuse a cette famille.");
        }
    }

    private static void EnsureFamilyAdmin(AppState state, Guid userId, Guid familyId)
    {
        if (state.Members.All(member => member.UserId != userId || member.FamilyId != familyId || member.Status != "accepted" || member.Role != UserRole.Admin))
        {
            throw new InvalidOperationException("Seul un administrateur peut envoyer ou modifier les notifications.");
        }
    }
}
