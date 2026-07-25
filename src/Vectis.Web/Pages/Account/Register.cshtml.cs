using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vectis.Domain;
using Vectis.Web.Services;

namespace Vectis.Web.Pages.Account;

public sealed class RegisterModel : PageModel
{
    private readonly AuthService _authService;

    public RegisterModel(AuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    public RegisterInput Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var user = await _authService.RegisterFamilyAsync(new RegisterFamilyCommand(Input.FirstName, Input.LastName, Input.Email, Input.Password, Input.FamilyName, Input.BabyFirstName, Input.BabyBirthDate, Input.UsualBottleMl));

            await SignInAsync(user);
            return RedirectToPage("/Index");
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return Page();
        }
    }

    private Task SignInAsync(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FirstName),
            new(ClaimTypes.Email, user.Email)
        };
        return HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
    }

    public sealed class RegisterInput
    {
        [Required] public string FirstName { get; set; } = "";
        [Required] public string LastName { get; set; } = "";
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required, MinLength(8)] public string Password { get; set; } = "";
        [Required] public string FamilyName { get; set; } = "Ma famille";
        [Required] public string BabyFirstName { get; set; } = "";
        [Required] public DateTime BabyBirthDate { get; set; } = DateTime.Today;
        [Range(1, 400)] public int UsualBottleMl { get; set; } = 120;
    }
}
