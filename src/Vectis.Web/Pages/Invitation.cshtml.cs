using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vectis.Domain;
using Vectis.Web.Services;

namespace Vectis.Web.Pages;

public sealed class InvitationModel : PageModel
{
    private readonly CurrentUser _currentUser;
    private readonly FamilyService _familyService;

    public InvitationModel(CurrentUser currentUser, FamilyService familyService)
    {
        _currentUser = currentUser;
        _familyService = familyService;
    }

    public FamilyInvitation? Invitation { get; private set; }

    public async Task OnGetAsync(Guid id)
    {
        Invitation = await _familyService.GetPendingInvitationAsync(id);
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var context = await _currentUser.GetAsync();
        if (context is null)
        {
            return RedirectToPage("/Account/Login");
        }

        try
        {
            await _familyService.AcceptInvitationAsync(id, context.User.Id);
            return RedirectToPage("/Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            Invitation = await _familyService.GetPendingInvitationAsync(id);
            return Page();
        }
    }
}
