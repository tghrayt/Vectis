using Vectis.Domain;

namespace Vectis.Web.Services;

public sealed record RegisterFamilyCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string FamilyName,
    string BabyFirstName,
    DateTime BabyBirthDate,
    int UsualBottleMl);

public sealed class AuthService
{
    private readonly IAppStore _store;
    private readonly VectisEngine _engine;
    private readonly PasswordHasher _hasher;

    public AuthService(IAppStore store, VectisEngine engine, PasswordHasher hasher)
    {
        _store = store;
        _engine = engine;
        _hasher = hasher;
    }

    public async Task<AppUser> RegisterFamilyAsync(RegisterFamilyCommand command)
    {
        return await _store.MutateAsync(state =>
        {
            var user = _engine.RegisterUser(state, command.FirstName, command.LastName, command.Email, _hasher.Hash(command.Password));
            var family = _engine.CreateFamily(state, user.Id, command.FamilyName);
            _engine.CreateBaby(state, user.Id, family.Id, command.BabyFirstName, DateOnly.FromDateTime(command.BabyBirthDate), command.UsualBottleMl, "");
            return user;
        });
    }

    public async Task<AppUser?> ValidateCredentialsAsync(string email, string password)
    {
        var state = await _store.LoadAsync();
        var user = state.Users.FirstOrDefault(user => user.Email.Equals(email.Trim(), StringComparison.OrdinalIgnoreCase));
        return user is not null && _hasher.Verify(password, user.PasswordHash) ? user : null;
    }
}
