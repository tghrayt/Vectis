using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vectis.Domain;
using Vectis.Web.Services;

namespace Vectis.Web.Pages;

[Authorize]
public sealed class UsersModel : PageModel
{
    private readonly CurrentUser _currentUser;
    private readonly FamilyService _familyService;

    public UsersModel(CurrentUser currentUser, FamilyService familyService)
    {
        _currentUser = currentUser;
        _familyService = familyService;
    }

    [BindProperty]
    public InviteInput Input { get; set; } = new();

    public IReadOnlyList<FamilyMemberView> Members { get; private set; } = [];
    public IReadOnlyList<FamilyInvitationView> Invitations { get; private set; } = [];
    public bool IsAdmin { get; private set; }
    public string? CreatedInvitationLink { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var context = await _currentUser.GetAsync();
        if (context?.Family is null)
        {
            return RedirectToPage("/Account/Login");
        }

        await LoadAsync(context);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var context = await _currentUser.GetAsync();
        if (context?.Family is null)
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
            var invitation = await _familyService.InviteAsync(context.Family.Id, context.User.Id, Input.Email, Input.Role);
            CreatedInvitationLink = Url.Page("/Invitation", null, new { id = invitation.Id }, Request.Scheme);
            await LoadAsync(context);
            return Page();
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
        IsAdmin = context.IsAdmin;
        var snapshot = await _familyService.GetUsersAsync(context.Family!.Id, context.User.Id);
        Members = snapshot.Members;
        Invitations = snapshot.Invitations;
    }

    public sealed class InviteInput
    {
        [Required, EmailAddress] public string Email { get; set; } = "";
        public UserRole Role { get; set; } = UserRole.Caregiver;
    }
}
