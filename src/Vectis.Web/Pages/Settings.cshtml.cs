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
    private readonly CurrentUser _currentUser;

    public SettingsModel(SettingsService settingsService, CurrentUser currentUser)
    {
        _settingsService = settingsService;
        _currentUser = currentUser;
    }

    [BindProperty]
    public SettingsInput Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        var rules = await _settingsService.GetConservationRulesAsync();
        Input.Rules = rules
            .OrderBy(rule => rule.Location.ToString())
            .Select(rule => new RuleInput { Location = rule.Location, DurationHours = rule.DurationHours })
            .ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var context = await _currentUser.GetAsync();
        if (context?.IsAdmin != true)
        {
            ModelState.AddModelError("", "Seul un administrateur peut modifier les regles de conservation.");
            await OnGetAsync();
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var now = DateTimeOffset.UtcNow;
        await _settingsService.SaveConservationRulesAsync(Input.Rules
            .Select(rule => new ConservationRule(rule.Location, rule.DurationHours, true, now))
            .ToList());

        return RedirectToPage("/Settings");
    }

    public sealed class SettingsInput
    {
        public List<RuleInput> Rules { get; set; } = [];
    }

    public sealed class RuleInput
    {
        public StorageLocation Location { get; set; }
        [Range(1, 20000)] public int DurationHours { get; set; }
    }
}
