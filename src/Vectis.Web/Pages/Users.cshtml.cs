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
    private readonly InvitationEmailService _invitationEmailService;

    public UsersModel(CurrentUser currentUser, FamilyService familyService, InvitationEmailService invitationEmailService)
    {
        _currentUser = currentUser;
        _familyService = familyService;
        _invitationEmailService = invitationEmailService;
    }

    [BindProperty]
    public InviteInput Input { get; set; } = new();

    public IReadOnlyList<FamilyMemberView> Members { get; private set; } = [];
    public IReadOnlyList<FamilyInvitationView> Invitations { get; private set; } = [];
    public bool IsAdmin { get; private set; }
    public string? CreatedInvitationLink { get; private set; }
    public string? EmailStatusMessage { get; private set; }
    public string? ActionStatusMessage { get; private set; }

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
            var emailResult = await _invitationEmailService.SendInvitationAsync(invitation, context.Family.Name, CreatedInvitationLink!);
            EmailStatusMessage = emailResult.Message;
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

    public async Task<IActionResult> OnPostResendAsync(Guid invitationId)
    {
        var context = await _currentUser.GetAsync();
        if (context?.Family is null)
        {
            return RedirectToPage("/Account/Login");
        }

        if (!context.IsAdmin)
        {
            ModelState.AddModelError("", "Seul un administrateur peut renvoyer une invitation.");
            await LoadAsync(context);
            return Page();
        }

        var invitation = await _familyService.GetPendingInvitationAsync(invitationId);
        if (invitation is null || invitation.FamilyId != context.Family.Id)
        {
            ModelState.AddModelError("", "Invitation introuvable ou deja acceptee.");
            await LoadAsync(context);
            return Page();
        }

        CreatedInvitationLink = Url.Page("/Invitation", null, new { id = invitation.Id }, Request.Scheme);
        var emailResult = await _invitationEmailService.SendInvitationAsync(invitation, context.Family.Name, CreatedInvitationLink!);
        EmailStatusMessage = emailResult.Message;
        await LoadAsync(context);
        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid invitationId)
    {
        var context = await _currentUser.GetAsync();
        if (context?.Family is null)
        {
            return RedirectToPage("/Account/Login");
        }

        try
        {
            await _familyService.CancelInvitationAsync(context.Family.Id, context.User.Id, invitationId);
            ActionStatusMessage = "Invitation annulee.";
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
        }

        await LoadAsync(context);
        return Page();
    }

    private async Task LoadAsync(CurrentContext context)
    {
        IsAdmin = context.IsAdmin;
        var snapshot = await _familyService.GetUsersAsync(context.Family!.Id, context.User.Id);
        Members = snapshot.Members;
        Invitations = snapshot.Invitations;
    }

    public static string RoleLabel(UserRole role)
    {
        return role == UserRole.Admin ? "Administrateur" : "Accompagnant";
    }

    public static string StatusLabel(string status)
    {
        return status switch
        {
            "accepted" => "Acceptee",
            "pending" => "En attente",
            "cancelled" => "Annulee",
            _ => status
        };
    }

    public sealed class InviteInput
    {
        [Required, EmailAddress] public string Email { get; set; } = "";
        public UserRole Role { get; set; } = UserRole.Caregiver;
    }
}
