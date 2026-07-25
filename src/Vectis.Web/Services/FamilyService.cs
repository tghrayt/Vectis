using Vectis.Domain;

namespace Vectis.Web.Services;

public sealed record FamilyMemberView(string Name, string Email, UserRole Role, string Status);

public sealed record FamilyInvitationView(Guid Id, string Email, UserRole Role, string Status, DateTimeOffset CreatedAt, DateTimeOffset? AcceptedAt);

public sealed record FamilyUsersSnapshot(IReadOnlyList<FamilyMemberView> Members, IReadOnlyList<FamilyInvitationView> Invitations);

public sealed class FamilyService
{
    private readonly IAppStore _store;

    public FamilyService(IAppStore store)
    {
        _store = store;
    }

    public async Task<FamilyUsersSnapshot> GetUsersAsync(Guid familyId, Guid requestingUserId)
    {
        var state = await _store.LoadAsync();
        EnsureFamilyMember(state, requestingUserId, familyId);

        var members = state.Members
            .Where(member => member.FamilyId == familyId)
            .Join(state.Users, member => member.UserId, user => user.Id, (member, user) => new FamilyMemberView($"{user.FirstName} {user.LastName}".Trim(), user.Email, member.Role, member.Status))
            .OrderBy(member => member.Email)
            .ToList();

        var invitations = state.Invitations
            .Where(invitation => invitation.FamilyId == familyId)
            .OrderByDescending(invitation => invitation.CreatedAt)
            .Select(invitation => new FamilyInvitationView(invitation.Id, invitation.Email, invitation.Role, invitation.Status, invitation.CreatedAt, invitation.AcceptedAt))
            .ToList();

        return new FamilyUsersSnapshot(members, invitations);
    }

    public Task<FamilyInvitation> InviteAsync(Guid familyId, Guid adminUserId, string email, UserRole role)
    {
        return _store.MutateAsync(state =>
        {
            EnsureFamilyAdmin(state, adminUserId, familyId);
            var normalizedEmail = email.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalizedEmail))
            {
                throw new InvalidOperationException("Adresse e-mail obligatoire.");
            }

            var invitedUser = state.Users.FirstOrDefault(user => user.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));
            if (invitedUser is not null && state.Members.Any(member => member.UserId == invitedUser.Id && member.FamilyId == familyId && member.Status == "accepted"))
            {
                throw new InvalidOperationException("Cet utilisateur est deja membre de la famille.");
            }

            var existingPending = state.Invitations.FirstOrDefault(invitation =>
                invitation.FamilyId == familyId &&
                invitation.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase) &&
                invitation.Status == "pending");
            if (existingPending is not null)
            {
                throw new InvalidOperationException("Une invitation est deja en attente pour cette adresse.");
            }

            var invitation = new FamilyInvitation(Guid.NewGuid(), familyId, normalizedEmail, role, "pending", adminUserId, DateTimeOffset.UtcNow, null);
            state.Invitations.Add(invitation);
            state.AuditEntries.Add(new AuditEntry(Guid.NewGuid(), adminUserId, "invite", nameof(FamilyInvitation), invitation.Id, "", normalizedEmail, DateTimeOffset.UtcNow));
            return invitation;
        });
    }

    public async Task<FamilyInvitation?> GetPendingInvitationAsync(Guid invitationId)
    {
        var state = await _store.LoadAsync();
        return state.Invitations.FirstOrDefault(invitation => invitation.Id == invitationId && invitation.Status == "pending");
    }

    public Task CancelInvitationAsync(Guid familyId, Guid adminUserId, Guid invitationId)
    {
        return _store.MutateAsync(state =>
        {
            EnsureFamilyAdmin(state, adminUserId, familyId);

            var invitationIndex = state.Invitations.FindIndex(invitation =>
                invitation.Id == invitationId &&
                invitation.FamilyId == familyId &&
                invitation.Status == "pending");

            if (invitationIndex < 0)
            {
                throw new InvalidOperationException("Invitation introuvable ou deja traitee.");
            }

            var invitation = state.Invitations[invitationIndex];
            state.Invitations[invitationIndex] = invitation with { Status = "cancelled" };
            state.AuditEntries.Add(new AuditEntry(Guid.NewGuid(), adminUserId, "cancel_invitation", nameof(FamilyInvitation), invitation.Id, "pending", "cancelled", DateTimeOffset.UtcNow));
        });
    }

    public Task AcceptInvitationAsync(Guid invitationId, Guid userId)
    {
        return _store.MutateAsync(state =>
        {
            var user = state.Users.FirstOrDefault(user => user.Id == userId)
                ?? throw new InvalidOperationException("Utilisateur introuvable.");
            var invitationIndex = state.Invitations.FindIndex(invitation => invitation.Id == invitationId && invitation.Status == "pending");
            if (invitationIndex < 0)
            {
                throw new InvalidOperationException("Invitation introuvable ou deja traitee.");
            }

            var invitation = state.Invitations[invitationIndex];
            if (!user.Email.Equals(invitation.Email, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Cette invitation est associee a une autre adresse e-mail.");
            }

            if (state.Members.All(member => member.UserId != userId || member.FamilyId != invitation.FamilyId))
            {
                state.Members.Add(new FamilyMember(userId, invitation.FamilyId, invitation.Role, "accepted"));
            }

            state.Invitations[invitationIndex] = invitation with { Status = "accepted", AcceptedAt = DateTimeOffset.UtcNow };
            state.AuditEntries.Add(new AuditEntry(Guid.NewGuid(), userId, "accept_invitation", nameof(FamilyInvitation), invitation.Id, "pending", "accepted", DateTimeOffset.UtcNow));
        });
    }

    private static void EnsureFamilyMember(AppState state, Guid userId, Guid familyId)
    {
        if (state.Members.All(member => member.UserId != userId || member.FamilyId != familyId || member.Status != "accepted"))
        {
            throw new InvalidOperationException("Acces refuse a cette famille.");
        }
    }

    private static void EnsureFamilyAdmin(AppState state, Guid userId, Guid familyId)
    {
        if (state.Members.All(member => member.UserId != userId || member.FamilyId != familyId || member.Role != UserRole.Admin || member.Status != "accepted"))
        {
            throw new InvalidOperationException("Seul un administrateur peut gerer les invitations.");
        }
    }
}
