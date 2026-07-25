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
    private readonly StockService _stockService;

    public IndexModel(CurrentUser currentUser, StockService stockService)
    {
        _currentUser = currentUser;
        _stockService = stockService;
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
        Summary = await _stockService.GetSummaryAsync(context.Baby.Id);
        return Page();
    }
}
