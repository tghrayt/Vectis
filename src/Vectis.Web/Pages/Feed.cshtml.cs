using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Vectis.Domain;
using Vectis.Web.Services;

namespace Vectis.Web.Pages;

public sealed record PendingBottleOption(
    Guid Id,
    string ShortId,
    int TotalQuantityMl,
    int SourceCount,
    string PreparedLabel,
    string AgeLabel,
    string PriorityLabel,
    string PriorityClass);

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
    public IReadOnlyList<PendingBottleOption> PendingOptions { get; private set; } = [];
    public PendingBottleOption? SelectedBottle { get; private set; }
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
            SelectedBottle = PendingOptions.FirstOrDefault(option => option.Id == selected.Id);
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
            SelectBottle(Input.BottleId);
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
            SelectBottle(Input.BottleId);
            return Page();
        }
    }

    private async Task LoadAsync(CurrentContext context)
    {
        PendingBottles = await _bottleService.GetPendingAsync(context.User.Id, context.Baby!.Id);
        PendingOptions = PendingBottles.Select(BuildPendingOption).ToList();
        SelectedBottle = PendingOptions.FirstOrDefault();
    }

    private void SelectBottle(Guid bottleId)
    {
        SelectedBottle = PendingOptions.FirstOrDefault(option => option.Id == bottleId) ?? PendingOptions.FirstOrDefault();
    }

    private static PendingBottleOption BuildPendingOption(PreparedBottle bottle)
    {
        var age = DateTimeOffset.UtcNow - bottle.PreparedAt;
        var priorityLabel = "Pret";
        var priorityClass = "ok";

        if (age >= TimeSpan.FromHours(2))
        {
            priorityLabel = "A traiter maintenant";
            priorityClass = "danger";
        }
        else if (age >= TimeSpan.FromMinutes(90))
        {
            priorityLabel = "Bientot urgent";
            priorityClass = "warning";
        }

        return new PendingBottleOption(
            bottle.Id,
            bottle.Id.ToString()[..8],
            bottle.TotalQuantityMl,
            bottle.Sources.Count,
            bottle.PreparedAt.LocalDateTime.ToString("g"),
            FormatAge(age),
            priorityLabel,
            priorityClass);
    }

    private static string FormatAge(TimeSpan age)
    {
        if (age.TotalMinutes < 1)
        {
            return "Prepare a l'instant";
        }

        if (age.TotalHours < 1)
        {
            return $"Prepare il y a {Math.Max(1, (int)Math.Floor(age.TotalMinutes))} min";
        }

        return $"Prepare il y a {Math.Max(1, (int)Math.Floor(age.TotalHours))} h {age.Minutes:00}";
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
