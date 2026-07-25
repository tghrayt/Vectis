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
    private readonly JsonAppStore _store;

    public SettingsModel(JsonAppStore store)
    {
        _store = store;
    }

    [BindProperty]
    public SettingsInput Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        var state = await _store.LoadAsync();
        Input.Rules = state.ConservationRules
            .OrderBy(rule => rule.Location.ToString())
            .Select(rule => new RuleInput { Location = rule.Location, DurationHours = rule.DurationHours })
            .ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _store.MutateAsync(state =>
        {
            var now = DateTimeOffset.UtcNow;
            state.ConservationRules = Input.Rules
                .Select(rule => new ConservationRule(rule.Location, rule.DurationHours, true, now))
                .ToList();
        });

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
