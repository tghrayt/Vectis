using System.Security.Claims;
using Vectis.Domain;

namespace Vectis.Web.Services;

public sealed record CurrentContext(AppUser User, Family? Family, Baby? Baby);

public sealed class CurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JsonAppStore _store;

    public CurrentUser(IHttpContextAccessor httpContextAccessor, JsonAppStore store)
    {
        _httpContextAccessor = httpContextAccessor;
        _store = store;
    }

    public async Task<CurrentContext?> GetAsync()
    {
        var idValue = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(idValue, out var userId))
        {
            return null;
        }

        var state = await _store.LoadAsync();
        var user = state.Users.FirstOrDefault(user => user.Id == userId);
        if (user is null)
        {
            return null;
        }

        var familyId = state.Members.FirstOrDefault(member => member.UserId == user.Id && member.Status == "accepted")?.FamilyId;
        var family = familyId is null ? null : state.Families.FirstOrDefault(item => item.Id == familyId);
        var baby = family is null ? null : state.Babies.FirstOrDefault(item => item.FamilyId == family.Id && item.IsActive);
        return new CurrentContext(user, family, baby);
    }
}
