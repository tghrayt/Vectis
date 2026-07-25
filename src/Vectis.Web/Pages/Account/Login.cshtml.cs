using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Vectis.Domain;
using Vectis.Web.Services;

namespace Vectis.Web.Pages.Account;

public sealed class LoginModel : PageModel
{
    private readonly IAppStore _store;
    private readonly PasswordHasher _hasher;

    public LoginModel(IAppStore store, PasswordHasher hasher)
    {
        _store = store;
        _hasher = hasher;
    }

    [BindProperty]
    public LoginInput Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var state = await _store.LoadAsync();
        var user = state.Users.FirstOrDefault(user => user.Email.Equals(Input.Email.Trim(), StringComparison.OrdinalIgnoreCase));
        if (user is null || !_hasher.Verify(Input.Password, user.PasswordHash))
        {
            ModelState.AddModelError("", "Identifiants invalides.");
            return Page();
        }

        await SignInAsync(user);
        return RedirectToPage("/Index");
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

    public sealed class LoginInput
    {
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required] public string Password { get; set; } = "";
    }
}
