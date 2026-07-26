using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Vectis.Domain;
using Vectis.Web.Services;

namespace Vectis.Web.Pages;

[Authorize]
public sealed class PumpingModel : PageModel
{
    private readonly CurrentUser _currentUser;
    private readonly PumpingService _pumpingService;

    public PumpingModel(CurrentUser currentUser, PumpingService pumpingService)
    {
        _currentUser = currentUser;
        _pumpingService = pumpingService;
    }

    [BindProperty]
    public PumpingInput Input { get; set; } = new();

    public IReadOnlyList<SelectListItem> LocationOptions { get; } = Enum.GetValues<StorageLocation>()
        .Select(location => new SelectListItem(DisplayLabels.Location(location), location.ToString()))
        .ToList();

    public void OnGet()
    {
        Input.PumpedAt = DateTime.Now;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var context = await _currentUser.GetAsync();
        if (context?.Baby is null)
        {
            return RedirectToPage("/Account/Login");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var drafts = new List<ContainerDraft> { new(ContainerType.StorageBag, Input.Container1Ml, Input.Location1, "") };
            if (Input.Container2Ml > 0)
            {
                drafts.Add(new ContainerDraft(ContainerType.StorageBag, Input.Container2Ml, Input.Location2, ""));
            }

            await _pumpingService.AddAsync(new AddPumpingCommand(context.User.Id, context.Baby.Id, Input.PumpedAt, Input.TotalMl, Input.DurationMinutes, Input.Side, Input.Notes, drafts));
            return RedirectToPage("/Stock");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return Page();
        }
    }

    public sealed class PumpingInput
    {
        [Required] public DateTime PumpedAt { get; set; } = DateTime.Now;
        [Range(1, 2000)] public int TotalMl { get; set; } = 180;
        [Range(0, 240)] public int? DurationMinutes { get; set; }
        public string? Side { get; set; } = "both";
        [Range(1, 2000)] public int Container1Ml { get; set; } = 100;
        [Range(0, 2000)] public int Container2Ml { get; set; } = 80;
        public StorageLocation Location1 { get; set; } = StorageLocation.Refrigerator;
        public StorageLocation Location2 { get; set; } = StorageLocation.Refrigerator;
        public string Notes { get; set; } = "";
    }
}
