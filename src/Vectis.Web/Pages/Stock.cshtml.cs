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

    public static string LocationLabel(StorageLocation location)
    {
        return location switch
        {
            StorageLocation.RoomTemperature => "Temperature ambiante",
            StorageLocation.Refrigerator => "Refrigerateur",
            StorageLocation.FridgeFreezerCompartment => "Compartiment congelateur",
            StorageLocation.SeparateFreezer => "Congelateur separe",
            StorageLocation.CoolerBag => "Sac isotherme",
            StorageLocation.Other => "Autre",
            _ => location.ToString()
        };
    }

    public static string StatusLabel(MilkStatus status)
    {
        return status switch
        {
            MilkStatus.FreshlyPumped => "Fraichement tire",
            MilkStatus.Refrigerated => "Refrigere",
            MilkStatus.Frozen => "Congele",
            MilkStatus.Thawing => "En decongelation",
            MilkStatus.Thawed => "Decongele",
            MilkStatus.ReadyToFeed => "Pret a donner",
            MilkStatus.PartiallyConsumed => "Partiellement consomme",
            MilkStatus.Consumed => "Consomme",
            MilkStatus.Discarded => "Jete",
            MilkStatus.Expired => "Expire",
            _ => status.ToString()
        };
    }
}
