using Vectis.Domain;

namespace Vectis.Web.Services;

public sealed class SettingsService
{
    private readonly IAppStore _store;

    public SettingsService(IAppStore store)
    {
        _store = store;
    }

    public async Task<IReadOnlyList<ConservationRule>> GetConservationRulesAsync()
    {
        var state = await _store.LoadAsync();
        return state.ConservationRules.OrderBy(rule => rule.Location.ToString()).ToList();
    }

    public Task SaveConservationRulesAsync(IReadOnlyList<ConservationRule> rules)
    {
        return _store.MutateAsync(state => state.ConservationRules = rules.ToList());
    }

    public Task UpdateBabyAsync(Guid userId, Guid babyId, string firstName, DateOnly birthDate, decimal? currentWeightKg, int usualBottleMl, string notes)
    {
        return _store.MutateAsync(state =>
        {
            var babyIndex = state.Babies.FindIndex(baby => baby.Id == babyId && baby.IsActive);
            if (babyIndex < 0)
            {
                throw new InvalidOperationException("Bebe introuvable.");
            }

            var baby = state.Babies[babyIndex];
            EnsureFamilyAdmin(state, userId, baby.FamilyId);
            if (usualBottleMl <= 0)
            {
                throw new InvalidOperationException("La quantite habituelle doit etre positive.");
            }

            state.Babies[babyIndex] = baby with
            {
                FirstName = firstName.Trim(),
                BirthDate = birthDate,
                CurrentWeightKg = currentWeightKg,
                UsualBottleMl = usualBottleMl,
                Notes = notes.Trim()
            };
        });
    }

    public Task UpdateFamilyAsync(Guid userId, Guid familyId, string name, string language, string timeZone)
    {
        return _store.MutateAsync(state =>
        {
            EnsureFamilyAdmin(state, userId, familyId);
            var familyIndex = state.Families.FindIndex(family => family.Id == familyId);
            if (familyIndex < 0)
            {
                throw new InvalidOperationException("Famille introuvable.");
            }

            var userIndex = state.Users.FindIndex(user => user.Id == userId);
            if (userIndex < 0)
            {
                throw new InvalidOperationException("Utilisateur introuvable.");
            }

            var familyName = name.Trim();
            if (string.IsNullOrWhiteSpace(familyName))
            {
                throw new InvalidOperationException("Le nom de la famille est obligatoire.");
            }

            var user = state.Users[userIndex];
            state.Families[familyIndex] = state.Families[familyIndex] with { Name = familyName };
            state.Users[userIndex] = user with
            {
                Language = string.IsNullOrWhiteSpace(language) ? user.Language : language.Trim(),
                TimeZone = string.IsNullOrWhiteSpace(timeZone) ? user.TimeZone : timeZone.Trim()
            };
        });
    }

    private static void EnsureFamilyAdmin(AppState state, Guid userId, Guid familyId)
    {
        if (state.Members.All(member => member.UserId != userId || member.FamilyId != familyId || member.Status != "accepted" || member.Role != UserRole.Admin))
        {
            throw new InvalidOperationException("Seul un administrateur peut modifier ces reglages.");
        }
    }
}
