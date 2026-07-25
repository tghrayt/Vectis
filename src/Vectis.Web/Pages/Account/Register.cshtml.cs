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
    private readonly IAppStore _store;
    private readonly VectisEngine _engine;
    private readonly PasswordHasher _hasher;

    public RegisterModel(IAppStore store, VectisEngine engine, PasswordHasher hasher)
    {
        _store = store;
        _engine = engine;
        _hasher = hasher;
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
            var user = await _store.MutateAsync(state =>
            {
                var created = _engine.RegisterUser(state, Input.FirstName, Input.LastName, Input.Email, _hasher.Hash(Input.Password));
                var family = _engine.CreateFamily(state, created.Id, Input.FamilyName);
                _engine.CreateBaby(state, created.Id, family.Id, Input.BabyFirstName, DateOnly.FromDateTime(Input.BabyBirthDate), Input.UsualBottleMl, "");
                return created;
            });

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
