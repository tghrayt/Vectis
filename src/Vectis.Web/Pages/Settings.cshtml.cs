using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vectis.Domain;
using Vectis.Web.Services;

namespace Vectis.Web.Pages;

[Authorize]
public sealed class SettingsModel : PageModel
{
    private readonly SettingsService _settingsService;
    private readonly NotificationService _notificationService;
    private readonly CurrentUser _currentUser;

    public SettingsModel(SettingsService settingsService, NotificationService notificationService, CurrentUser currentUser)
    {
        _settingsService = settingsService;
        _notificationService = notificationService;
        _currentUser = currentUser;
    }

    [BindProperty]
    public BabyInput Baby { get; set; } = new();

    [BindProperty]
    public FamilyInput Family { get; set; } = new();

    [BindProperty]
    public NotificationInput Notifications { get; set; } = new();

    [BindProperty]
    public RulesInput Rules { get; set; } = new();

    public string? SavedSection { get; private set; }
    public bool CanEdit { get; private set; }
    public string AccessLabel => CanEdit ? "Administrateur" : "Lecture seule";
    public string NotificationStatusLabel => Notifications.AutomaticEmailEnabled ? "Automatique active" : "Automatique desactive";
    public int ActiveNotificationRules =>
        (Notifications.StockLowEnabled ? 1 : 0)
        + (Notifications.ExpiringSoonEnabled ? 1 : 0)
        + (Notifications.PreparedBottleAgingEnabled ? 1 : 0);

    public async Task<IActionResult> OnGetAsync(string? saved)
    {
        var context = await _currentUser.GetAsync();
        if (context?.Family is null || context.Baby is null)
        {
            return RedirectToPage("/Account/Login");
        }

        await LoadAsync(context);
        SavedSection = saved;
        return Page();
    }

    public async Task<IActionResult> OnPostBabyAsync()
    {
        var context = await _currentUser.GetAsync();
        if (context?.Family is null || context.Baby is null)
        {
            return RedirectToPage("/Account/Login");
        }

        ValidateBaby();
        if (!context.IsAdmin)
        {
            ModelState.AddModelError("", "Seul un administrateur peut modifier le profil bebe.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(context);
            return Page();
        }

        try
        {
            await _settingsService.UpdateBabyAsync(context.User.Id, context.Baby.Id, Baby.FirstName, Baby.BirthDate, Baby.CurrentWeightKg, Baby.UsualBottleMl, Baby.Notes);
            return RedirectToPage("/Settings", new { saved = "baby" });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadAsync(context);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostFamilyAsync()
    {
        var context = await _currentUser.GetAsync();
        if (context?.Family is null || context.Baby is null)
        {
            return RedirectToPage("/Account/Login");
        }

        ValidateFamily();
        if (!context.IsAdmin)
        {
            ModelState.AddModelError("", "Seul un administrateur peut modifier les reglages famille.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(context);
            return Page();
        }

        try
        {
            await _settingsService.UpdateFamilyAsync(context.User.Id, context.Family.Id, Family.Name, Family.Language, Family.TimeZone);
            return RedirectToPage("/Settings", new { saved = "family" });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadAsync(context);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostRulesAsync()
    {
        var context = await _currentUser.GetAsync();
        if (context?.Family is null || context.Baby is null)
        {
            return RedirectToPage("/Account/Login");
        }

        if (!context.IsAdmin)
        {
            ModelState.AddModelError("", "Seul un administrateur peut modifier les regles de conservation.");
            await LoadAsync(context);
            return Page();
        }

        ValidateRules();
        if (!ModelState.IsValid)
        {
            await LoadAsync(context);
            return Page();
        }

        var now = DateTimeOffset.UtcNow;
        await _settingsService.SaveConservationRulesAsync(Rules.Items
            .Select(rule => new ConservationRule(rule.Location, rule.DurationHours, true, now))
            .ToList());

        return RedirectToPage("/Settings", new { saved = "rules" });
    }

    public async Task<IActionResult> OnPostNotificationsAsync()
    {
        var context = await _currentUser.GetAsync();
        if (context?.Family is null || context.Baby is null)
        {
            return RedirectToPage("/Account/Login");
        }

        ValidateNotifications();
        if (!context.IsAdmin)
        {
            ModelState.AddModelError("", "Seul un administrateur peut modifier les notifications.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(context);
            return Page();
        }

        try
        {
            await _notificationService.SavePreferencesAsync(
                context.User.Id,
                context.Family.Id,
                new NotificationPreferences(
                    context.Family.Id,
                    Notifications.AutomaticEmailEnabled,
                    Notifications.StockLowEnabled,
                    Notifications.ExpiringSoonEnabled,
                    Notifications.PreparedBottleAgingEnabled,
                    Notifications.StockLowBottleThreshold,
                    Notifications.ExpiringSoonHours,
                    Notifications.PreparedBottleAgeMinutes));
            return RedirectToPage("/Settings", new { saved = "notifications" });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadAsync(context);
            return Page();
        }
    }

    private async Task LoadAsync(CurrentContext context)
    {
        CanEdit = context.IsAdmin;
        Baby = new BabyInput
        {
            FirstName = context.Baby!.FirstName,
            BirthDate = context.Baby.BirthDate,
            CurrentWeightKg = context.Baby.CurrentWeightKg,
            UsualBottleMl = context.Baby.UsualBottleMl,
            Notes = context.Baby.Notes
        };
        Family = new FamilyInput
        {
            Name = context.Family!.Name,
            Language = context.User.Language,
            TimeZone = context.User.TimeZone
        };
        var overview = await _notificationService.GetOverviewAsync(context.User.Id, context.Family.Id);
        Notifications = new NotificationInput
        {
            AutomaticEmailEnabled = overview.Preferences.AutomaticEmailEnabled,
            StockLowEnabled = overview.Preferences.StockLowEnabled,
            ExpiringSoonEnabled = overview.Preferences.ExpiringSoonEnabled,
            PreparedBottleAgingEnabled = overview.Preferences.PreparedBottleAgingEnabled,
            StockLowBottleThreshold = overview.Preferences.StockLowBottleThreshold,
            ExpiringSoonHours = overview.Preferences.ExpiringSoonHours,
            PreparedBottleAgeMinutes = overview.Preferences.PreparedBottleAgeMinutes
        };

        var rules = await _settingsService.GetConservationRulesAsync();
        Rules.Items = rules
            .OrderBy(rule => rule.Location.ToString())
            .Select(rule => new RuleInput { Location = rule.Location, DurationHours = rule.DurationHours })
            .ToList();
    }

    private void ValidateBaby()
    {
        ModelState.Clear();
        if (string.IsNullOrWhiteSpace(Baby.FirstName))
        {
            ModelState.AddModelError("Baby.FirstName", "Le prenom du bebe est obligatoire.");
        }

        if (Baby.UsualBottleMl <= 0 || Baby.UsualBottleMl > 2000)
        {
            ModelState.AddModelError("Baby.UsualBottleMl", "La quantite habituelle doit etre comprise entre 1 et 2000 ml.");
        }

        if (Baby.CurrentWeightKg is <= 0 or > 30)
        {
            ModelState.AddModelError("Baby.CurrentWeightKg", "Le poids doit rester dans une valeur realiste.");
        }
    }

    private void ValidateFamily()
    {
        ModelState.Clear();
        if (string.IsNullOrWhiteSpace(Family.Name))
        {
            ModelState.AddModelError("Family.Name", "Le nom de la famille est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(Family.Language))
        {
            ModelState.AddModelError("Family.Language", "La langue est obligatoire.");
        }

        if (string.IsNullOrWhiteSpace(Family.TimeZone))
        {
            ModelState.AddModelError("Family.TimeZone", "Le fuseau horaire est obligatoire.");
        }
    }

    private void ValidateRules()
    {
        ModelState.Clear();
        foreach (var rule in Rules.Items.Where(rule => rule.DurationHours <= 0 || rule.DurationHours > 20000))
        {
            ModelState.AddModelError("Rules.Items", $"La duree pour {DisplayLabels.Location(rule.Location)} doit etre comprise entre 1 et 20000 heures.");
        }
    }

    private void ValidateNotifications()
    {
        ModelState.Clear();
        if (Notifications.StockLowBottleThreshold < 0 || Notifications.StockLowBottleThreshold > 50)
        {
            ModelState.AddModelError("Notifications.StockLowBottleThreshold", "Le seuil stock faible doit etre compris entre 0 et 50 biberons.");
        }

        if (Notifications.ExpiringSoonHours <= 0 || Notifications.ExpiringSoonHours > 240)
        {
            ModelState.AddModelError("Notifications.ExpiringSoonHours", "Le seuil d'expiration doit etre compris entre 1 et 240 heures.");
        }

        if (Notifications.PreparedBottleAgeMinutes < 15 || Notifications.PreparedBottleAgeMinutes > 1440)
        {
            ModelState.AddModelError("Notifications.PreparedBottleAgeMinutes", "Le delai biberon doit etre compris entre 15 et 1440 minutes.");
        }
    }

    public sealed class BabyInput
    {
        [Required] public string FirstName { get; set; } = "";
        [Required] public DateOnly BirthDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        [Range(0.5, 30)] public decimal? CurrentWeightKg { get; set; }
        [Range(1, 2000)] public int UsualBottleMl { get; set; } = 120;
        public string Notes { get; set; } = "";
    }

    public sealed class FamilyInput
    {
        [Required] public string Name { get; set; } = "";
        [Required] public string Language { get; set; } = "fr";
        [Required] public string TimeZone { get; set; } = "Europe/Paris";
    }

    public sealed class NotificationInput
    {
        public bool AutomaticEmailEnabled { get; set; }
        public bool StockLowEnabled { get; set; } = true;
        public bool ExpiringSoonEnabled { get; set; } = true;
        public bool PreparedBottleAgingEnabled { get; set; } = true;
        [Range(0, 50)] public int StockLowBottleThreshold { get; set; } = 2;
        [Range(1, 240)] public int ExpiringSoonHours { get; set; } = 24;
        [Range(15, 1440)] public int PreparedBottleAgeMinutes { get; set; } = 120;
    }

    public sealed class RulesInput
    {
        public List<RuleInput> Items { get; set; } = [];
    }

    public sealed class RuleInput
    {
        public StorageLocation Location { get; set; }
        [Range(1, 20000)] public int DurationHours { get; set; }
    }
}
