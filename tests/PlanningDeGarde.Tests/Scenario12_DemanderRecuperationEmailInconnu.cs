using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 25 — Sc.12 — Récupération : email INCONNU → réponse neutre, AUCUN mail (@back @preuve-doublure)
//   PREUVE PAR DOUBLURE D'ADAPTATEUR (SpyEnvoiMail) ; câblage SMTP réel vérifié MANUELLEMENT au G3.
//   Tranche BACKEND (frontière Application) : une demande de récupération pour un email qu'AUCUN compte
//   ne porte NE génère AUCUN jeton et NE remet AUCUN envoi au port IEnvoiMail. Surtout, la RÉPONSE au
//   client est la MÊME réponse neutre qu'au Sc.11 (email connu) : anti-énumération strict — un attaquant
//   ne peut pas distinguer un email existant d'un email inconnu à partir de la réponse.
public class Scenario12_DemanderRecuperationEmailInconnu
{
    private const string EmailConnu = "carole@foyer.fr";
    private const string EmailInconnu = "personne@foyer.fr";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Aucun compte pour l'email : la demande NE remet AUCUN mail au port (aucun jeton généré), et répond
    // tout de même par un succès neutre (aucune fuite sur l'inexistence du compte).
    [Fact]
    public void Acceptation_Should_N_emettre_aucun_mail_et_repondre_neutre_When_l_email_est_inconnu()
    {
        var comptes = new ReferentielComptesEnMemoire(); // référentiel sans le compte visé
        var mail = new SpyEnvoiMail();
        var handler = new DemanderRecuperationMotDePasseHandler(comptes, mail);

        var resultat = handler.Handle(new DemanderRecuperationMotDePasseCommand(EmailInconnu));

        Assert.True(resultat.EstSucces);          // réponse neutre = succès (jamais un refus qui trahirait)
        Assert.Equal(0, mail.NombreDeMailsEmis);  // AUCUN mail : aucun jeton généré
    }

    // ---------- Driver — réponse IDENTIQUE à l'email connu (anti-énumération) ----------
    // Contradiction : une réponse d'email inconnu qui différerait de celle d'un email connu (motif,
    // succès/échec, contenu) permettrait d'énumérer les comptes. Force une réponse STRICTEMENT identique
    // dans les deux cas — la seule différence observable (le mail) est HORS du canal de réponse client.
    [Fact]
    public void Should_Retourner_la_meme_reponse_neutre_que_pour_un_email_connu_When_l_email_est_inconnu()
    {
        var comptes = new ReferentielComptesEnMemoire();
        comptes.Creer("compte-carole", EmailConnu, StatutCompte.Actif, "acteur-carole");
        var mail = new SpyEnvoiMail();
        var handler = new DemanderRecuperationMotDePasseHandler(comptes, mail);

        var reponseConnu = handler.Handle(new DemanderRecuperationMotDePasseCommand(EmailConnu));
        var reponseInconnu = handler.Handle(new DemanderRecuperationMotDePasseCommand(EmailInconnu));

        Assert.Equal(reponseConnu.EstSucces, reponseInconnu.EstSucces); // même issue
        Assert.Equal(reponseConnu.Motif, reponseInconnu.Motif);         // même motif (aucun ici)
        Assert.Equal(reponseConnu.Valeur, reponseInconnu.Valeur);       // même valeur neutre (record => égalité structurelle)
    }
}
