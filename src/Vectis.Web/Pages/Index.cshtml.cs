using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vectis.Domain;
using Vectis.Web.Services;

namespace Vectis.Web.Pages;

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly CurrentUser _currentUser;
    private readonly JsonAppStore _store;
    private readonly VectisEngine _engine;

    public IndexModel(CurrentUser currentUser, JsonAppStore store, VectisEngine engine)
    {
        _currentUser = currentUser;
        _store = store;
        _engine = engine;
    }

    public StockSummary? Summary { get; private set; }
    public string BabyName { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync()
    {
        var context = await _currentUser.GetAsync();
        if (context?.Baby is null)
        {
            return RedirectToPage("/Account/Login");
        }

        BabyName = context.Baby.FirstName;
        var state = await _store.LoadAsync();
        Summary = _engine.BuildStockSummary(state, context.Baby.Id);
        return Page();
    }
}
