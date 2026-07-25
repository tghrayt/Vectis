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
    private readonly StockService _stockService;

    public StockModel(CurrentUser currentUser, StockService stockService)
    {
        _currentUser = currentUser;
        _stockService = stockService;
    }

    public IReadOnlyList<MilkContainer> Containers { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var context = await _currentUser.GetAsync();
        if (context?.Baby is null)
        {
            return RedirectToPage("/Account/Login");
        }

        Containers = await _stockService.GetAvailableContainersAsync(context.User.Id, context.Baby.Id);
        return Page();
    }
}
