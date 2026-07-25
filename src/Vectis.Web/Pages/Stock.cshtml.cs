using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vectis.Domain;
using Vectis.Web.Services;

namespace Vectis.Web.Pages;

[Authorize]
public sealed class StockModel : PageModel
{
    private readonly CurrentUser _currentUser;
    private readonly IAppStore _store;
    private readonly VectisEngine _engine;

    public StockModel(CurrentUser currentUser, IAppStore store, VectisEngine engine)
    {
        _currentUser = currentUser;
        _store = store;
        _engine = engine;
    }

    public IReadOnlyList<MilkContainer> Containers { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var context = await _currentUser.GetAsync();
        if (context?.Baby is null)
        {
            return RedirectToPage("/Account/Login");
        }

        var state = await _store.LoadAsync();
        Containers = _engine.AvailableContainers(state, context.Baby.Id);
        return Page();
    }
}
