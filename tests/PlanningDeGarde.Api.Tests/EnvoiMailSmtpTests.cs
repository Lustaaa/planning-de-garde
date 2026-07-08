using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 28 — S1 (cycle interne) — l'adaptateur SMTP concret <see cref="EnvoiMailSmtp"/> remet un
/// <b>vrai mail</b> au serveur SMTP de développement (Smtp4dev, Docker) : sur
/// <c>EnvoyerRecuperationMotDePasse</c>, un message adressé au destinataire et porteur du jeton fourni
/// est capté par le serveur, relu par son API HTTP. Preuve runtime réelle du câblage SMTP (aucune
/// doublure du canal mail). <b>Skip propre</b> si Smtp4dev est injoignable.
/// </summary>
[Collection("Smtp4dev")]
public sealed class EnvoiMailSmtpTests
{
    [SmtpDevRequisFact]
    public async Task Should_Remettre_un_mail_capte_par_le_serveur_SMTP_de_dev_adresse_au_destinataire_et_porteur_du_jeton_When_EnvoyerRecuperationMotDePasse_est_appele()
    {
        await SmtpDev.ViderMessages();
        var destinataire = "maman@foyer.fr";
        var jeton = $"reset-{Guid.NewGuid():N}";

        var adaptateur = new EnvoiMailSmtp(SmtpDev.SmtpHote, SmtpDev.SmtpPort, "no-reply@planning-de-garde.fr");
        adaptateur.EnvoyerRecuperationMotDePasse(destinataire, jeton);

        var source = await SmtpDev.TrouverSourceMailPour(destinataire);
        Assert.NotNull(source);
        Assert.Contains(jeton, source);
    }
}
