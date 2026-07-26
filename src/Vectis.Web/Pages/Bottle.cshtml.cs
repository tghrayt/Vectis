using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vectis.Domain;
using Vectis.Web.Services;

namespace Vectis.Web.Pages;

public sealed record BottleSourceOption(
    Guid Id,
    string ShortId,
    int RemainingQuantityMl,
    string LocationLabel,
    string StatusLabel,
    string ExpirationLabel,
    string PriorityLabel,
    string PriorityClass);

[Authorize]
public sealed class BottleModel : PageModel
{
    private readonly CurrentUser _currentUser;
    private readonly StockService _stockService;
    private readonly BottleService _bottleService;

    public BottleModel(CurrentUser currentUser, StockService stockService, BottleService bottleService)
    {
        _currentUser = currentUser;
        _stockService = stockService;
        _bottleService = bottleService;
    }

    public IReadOnlyList<MilkContainer> Containers { get; private set; } = [];
    public IReadOnlyList<BottleSourceOption> SourceOptions { get; private set; } = [];
    public string BabyName { get; private set; } = "";
    public int UsualBottleMl { get; private set; } = 120;
    public int TotalAvailableMl { get; private set; }
    public int AvailableBottleCount { get; private set; }
    public string RecommendedSourceLabel { get; private set; } = "Aucun contenant prioritaire";

    [BindProperty]
    public BottleInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadContextAsync();
        PrefillSources();
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
            await LoadContextAsync();
            return Page();
        }

        try
        {
            var sources = new List<PreparedBottleSource>();
            if (Input.Container1Id != Guid.Empty && Input.Container1Ml > 0)
            {
                sources.Add(new PreparedBottleSource(Input.Container1Id, Input.Container1Ml));
            }
            if (Input.Container2Id.HasValue && Input.Container2Id.Value != Guid.Empty && Input.Container2Ml > 0)
            {
                sources.Add(new PreparedBottleSource(Input.Container2Id.Value, Input.Container2Ml));
            }

            var bottle = await _bottleService.PrepareAsync(new PrepareBottleCommand(context.User.Id, context.Baby.Id, sources, Input.Notes));
            return RedirectToPage("/Feed", new { prepared = bottle.Id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadContextAsync();
            return Page();
        }
    }

    private async Task LoadContextAsync()
    {
        var context = await _currentUser.GetAsync();
        if (context?.Baby is null)
        {
            Containers = [];
            SourceOptions = [];
            return;
        }

        BabyName = context.Baby.FirstName;
        UsualBottleMl = context.Baby.UsualBottleMl;
        Containers = await _stockService.GetAvailableContainersAsync(context.User.Id, context.Baby.Id);
        TotalAvailableMl = Containers.Sum(container => container.RemainingQuantityMl);
        AvailableBottleCount = UsualBottleMl <= 0 ? 0 : TotalAvailableMl / UsualBottleMl;
        RecommendedSourceLabel = Containers.Count == 0
            ? "Aucun contenant prioritaire"
            : $"{Containers[0].Id.ToString()[..8]} - {Containers[0].RemainingQuantityMl} ml - exp {Containers[0].EstimatedExpiresAt.LocalDateTime:g}";
        SourceOptions = Containers.Select(BuildSourceOption).ToList();
    }

    private void PrefillSources()
    {
        Input.Container1Id = Guid.Empty;
        Input.Container1Ml = 0;
        Input.Container2Id = null;
        Input.Container2Ml = 0;

        if (Containers.Count == 0)
        {
            return;
        }

        var remainingTarget = UsualBottleMl;
        Input.Container1Id = Containers[0].Id;
        Input.Container1Ml = Math.Min(remainingTarget, Containers[0].RemainingQuantityMl);
        remainingTarget -= Input.Container1Ml;

        if (remainingTarget > 0 && Containers.Count > 1)
        {
            Input.Container2Id = Containers[1].Id;
            Input.Container2Ml = Math.Min(remainingTarget, Containers[1].RemainingQuantityMl);
        }
    }

    private static BottleSourceOption BuildSourceOption(MilkContainer container)
    {
        var remaining = container.EstimatedExpiresAt - DateTimeOffset.UtcNow;
        var priorityLabel = "Stable";
        var priorityClass = "ok";

        if (remaining <= TimeSpan.FromHours(24))
        {
            priorityLabel = "A utiliser maintenant";
            priorityClass = "danger";
        }
        else if (remaining <= TimeSpan.FromHours(72))
        {
            priorityLabel = "Bientot";
            priorityClass = "warning";
        }

        return new BottleSourceOption(
            container.Id,
            container.Id.ToString()[..8],
            container.RemainingQuantityMl,
            DisplayLabels.Location(container.Location),
            DisplayLabels.MilkStatus(container.Status),
            container.EstimatedExpiresAt.LocalDateTime.ToString("g"),
            priorityLabel,
            priorityClass);
    }

    public sealed class BottleInput
    {
        [Required] public Guid Container1Id { get; set; }
        [Range(0, 2000)] public int Container1Ml { get; set; } = 60;
        public Guid? Container2Id { get; set; }
        [Range(0, 2000)] public int Container2Ml { get; set; } = 60;
        public string Notes { get; set; } = "";
    }
}
