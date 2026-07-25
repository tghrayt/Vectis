using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vectis.Domain;
using Vectis.Web.Services;

namespace Vectis.Web.Pages;

[Authorize]
public sealed class BottleModel : PageModel
{
    private readonly CurrentUser _currentUser;
    private readonly JsonAppStore _store;
    private readonly VectisEngine _engine;

    public BottleModel(CurrentUser currentUser, JsonAppStore store, VectisEngine engine)
    {
        _currentUser = currentUser;
        _store = store;
        _engine = engine;
    }

    public IReadOnlyList<MilkContainer> Containers { get; private set; } = [];

    [BindProperty]
    public BottleInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadContainersAsync();
        if (Containers.Count > 0)
        {
            Input.Container1Id = Containers[0].Id;
            Input.Container1Ml = Math.Min(60, Containers[0].RemainingQuantityMl);
        }

        if (Containers.Count > 1)
        {
            Input.Container2Id = Containers[1].Id;
            Input.Container2Ml = 60;
        }
        Input.ConsumedMl = 90;
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
            await LoadContainersAsync();
            return Page();
        }

        try
        {
            await _store.MutateAsync(state =>
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

                var bottle = _engine.PrepareBottle(state, context.User.Id, context.Baby.Id, sources, Input.Notes);
                _engine.RecordFeeding(state, context.User.Id, context.Baby.Id, bottle.Id, bottle.TotalQuantityMl, Input.ConsumedMl, Input.Reaction, Input.LeftoverOutcome, Input.Notes);
            });
            return RedirectToPage("/History");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadContainersAsync();
            return Page();
        }
    }

    private async Task LoadContainersAsync()
    {
        var context = await _currentUser.GetAsync();
        if (context?.Baby is null)
        {
            Containers = [];
            return;
        }

        var state = await _store.LoadAsync();
        Containers = _engine.AvailableContainers(state, context.Baby.Id);
    }

    public sealed class BottleInput
    {
        [Required] public Guid Container1Id { get; set; }
        [Range(0, 2000)] public int Container1Ml { get; set; } = 60;
        public Guid? Container2Id { get; set; }
        [Range(0, 2000)] public int Container2Ml { get; set; } = 60;
        [Range(0, 2000)] public int ConsumedMl { get; set; } = 90;
        public FeedingReaction Reaction { get; set; } = FeedingReaction.Normal;
        public string LeftoverOutcome { get; set; } = "jete";
        public string Notes { get; set; } = "";
    }
}
