using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Vectis.Domain;
using Vectis.Web.Services;

namespace Vectis.Web.Pages;

[Authorize]
public sealed class FeedModel : PageModel
{
    private readonly CurrentUser _currentUser;
    private readonly BottleService _bottleService;

    public FeedModel(CurrentUser currentUser, BottleService bottleService)
    {
        _currentUser = currentUser;
        _bottleService = bottleService;
    }

    public IReadOnlyList<PreparedBottle> PendingBottles { get; private set; } = [];
    public IReadOnlyList<SelectListItem> ReactionOptions { get; } = Enum.GetValues<FeedingReaction>()
        .Select(reaction => new SelectListItem(DisplayLabels.FeedingReaction(reaction), reaction.ToString()))
        .ToList();
    public bool BottlePrepared { get; private set; }

    [BindProperty]
    public FeedInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? prepared)
    {
        var context = await _currentUser.GetAsync();
        if (context?.Baby is null)
        {
            return RedirectToPage("/Account/Login");
        }

        await LoadAsync(context);
        BottlePrepared = prepared.HasValue;
        var selected = prepared.HasValue
            ? PendingBottles.FirstOrDefault(bottle => bottle.Id == prepared.Value)
            : PendingBottles.FirstOrDefault();

        if (selected is not null)
        {
            Input.BottleId = selected.Id;
            Input.ConsumedMl = selected.TotalQuantityMl;
        }

        return Page();
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
            await LoadAsync(context);
            return Page();
        }

        try
        {
            await _bottleService.FeedAsync(new FeedPreparedBottleCommand(context.User.Id, context.Baby.Id, Input.BottleId, Input.ConsumedMl, Input.Reaction, Input.LeftoverOutcome, Input.Notes));
            return RedirectToPage("/History", new { saved = "feeding" });
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
        PendingBottles = await _bottleService.GetPendingAsync(context.User.Id, context.Baby!.Id);
    }

    public sealed class FeedInput
    {
        [Required] public Guid BottleId { get; set; }
        [Range(0, 2000)] public int ConsumedMl { get; set; }
        public FeedingReaction Reaction { get; set; } = FeedingReaction.Normal;
        public string LeftoverOutcome { get; set; } = "jete";
        public string Notes { get; set; } = "";
    }
}
