using Vectis.Domain;

namespace Vectis.Web.Services;

public sealed class InvitationEmailService
{
    private readonly EmailService _emailService;

    public InvitationEmailService(EmailService emailService)
    {
        _emailService = emailService;
    }

    public Task<EmailSendResult> SendInvitationAsync(FamilyInvitation invitation, string familyName, string invitationLink)
    {
        var roleLabel = invitation.Role == UserRole.Admin ? "administrateur" : "accompagnant";
        var body = $"""
Bonjour,

Vous avez ete invite a rejoindre la famille "{familyName}" sur Vectis avec le role {roleLabel}.

Pour accepter l'invitation, ouvrez ce lien :
{invitationLink}

Si vous n'attendiez pas cette invitation, vous pouvez ignorer cet e-mail.

Vectis
""";

        return _emailService.SendAsync(invitation.Email, "Invitation a rejoindre une famille Vectis", body);
    }
}
