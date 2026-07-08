using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 25 — Sc.11 — Récupération de mot de passe : email CONNU → jeton + mail émis (@back @preuve-doublure)
//   PREUVE PAR DOUBLURE D'ADAPTATEUR (SpyEnvoiMail) ; câblage SMTP réel vérifié MANUELLEMENT au G3.
//   Tranche BACKEND (frontière Application) : une demande de récupération pour un email PORTÉ par un
//   compte génère côté serveur un jeton de réinitialisation (à usage unique + expiration — la
//   consommation/expiration est prouvée à Sc.13) et remet au port IEnvoiMail (doublure) un mail
//   contenant ce jeton. La RÉPONSE au client est NEUTRE : elle ne confirme jamais l'existence du compte
//   (anti-énumération — Sc.12 prouve le pendant email inconnu, même réponse).
public class Scenario11_DemanderRecuperationMotDePasse
{
    private const string Email = "carole@foyer.fr";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Un compte existe pour l'email : la demande de récupération doit RÉUSSIR (réponse neutre), et le port
    // IEnvoiMail (doublure) doit avoir reçu EXACTEMENT un mail, adressé à cet email, porteur d'un jeton non
    // vide (généré côté serveur).
    [Fact]
    public void Acceptation_Should_Generer_un_jeton_et_remettre_un_mail_au_port_When_l_email_est_connu()
    {
        var comptes = new ReferentielComptesEnMemoire();
        comptes.Creer("compte-carole", Email, StatutCompte.Actif, "acteur-carole");
        var mail = new SpyEnvoiMail();
        var handler = new DemanderRecuperationMotDePasseHandler(comptes, mail, new FakeReferentielJetonsReset(), new HorlogeFigee(new System.DateTime(2026, 7, 2, 12, 0, 0, System.DateTimeKind.Utc)));

        var resultat = handler.Handle(new DemanderRecuperationMotDePasseCommand(Email));

        Assert.True(resultat.EstSucces);                       // réponse neutre = toujours un succès (Sc.12 idem)
        Assert.Equal(1, mail.NombreDeMailsEmis);               // un mail remis au port
        Assert.Equal(Email, mail.DernierMail!.Destinataire);   // ... adressé à l'email demandé
        Assert.False(string.IsNullOrWhiteSpace(mail.DernierMail!.Jeton)); // ... porteur d'un jeton généré
    }

    // ---------- Driver — la réponse est NEUTRE : ne divulgue pas l'existence du compte ----------
    // Contradiction : une réponse qui exposerait le jeton (ou « compte trouvé ») fuirait l'existence de
    // l'email. Force une réponse SANS le jeton et SANS distinction d'existence — le jeton ne transite que
    // par le canal mail, jamais par la réponse au client.
    [Fact]
    public void Should_Ne_pas_exposer_le_jeton_dans_la_reponse_client_When_l_email_est_connu()
    {
        var comptes = new ReferentielComptesEnMemoire();
        comptes.Creer("compte-carole", Email, StatutCompte.Actif, "acteur-carole");
        var mail = new SpyEnvoiMail();
        var handler = new DemanderRecuperationMotDePasseHandler(comptes, mail, new FakeReferentielJetonsReset(), new HorlogeFigee(new System.DateTime(2026, 7, 2, 12, 0, 0, System.DateTimeKind.Utc)));

        var resultat = handler.Handle(new DemanderRecuperationMotDePasseCommand(Email));

        var jetonEmis = mail.DernierMail!.Jeton;
        Assert.DoesNotContain(jetonEmis, resultat.ToString()); // le jeton ne fuit jamais par la réponse
    }
}
