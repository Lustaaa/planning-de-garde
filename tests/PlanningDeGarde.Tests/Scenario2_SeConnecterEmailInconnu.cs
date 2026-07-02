using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 23 — Sc.2 — Rejet : email inconnu, aucune session (@back)
//   Tranche BACKEND (frontière Application) : la commande SeConnecter, sur un email qu'AUCUN compte du
//   référentiel ne porte, ÉCHOUE avec un motif clair (email inconnu) et n'ouvre AUCUNE session (le
//   visiteur reste non connecté). On NE teste PAS ici de rendu Blazor.
public class Scenario2_SeConnecterEmailInconnu
{
    private const string EmailInconnu = "inconnu@foyer.fr";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Un référentiel sans aucun compte d'email « inconnu@foyer.fr » : tenter de s'y connecter doit
    // ÉCHOUER avec un motif clair et sans valeur de session (aucune session ouverte).
    [Fact]
    public void Acceptation_Should_Echouer_avec_motif_clair_et_sans_session_When_l_email_est_inconnu()
    {
        var comptes = new ReferentielComptesEnMemoire();
        comptes.Creer("compte-1", "alice@foyer.fr", StatutCompte.Actif, "acteur-alice"); // un autre compte
        var handler = new SeConnecterHandler(comptes, new HacheurMotDePassePbkdf2());

        var resultat = handler.Handle(new SeConnecterCommand(EmailInconnu));

        Assert.False(resultat.EstSucces);                 // refus
        Assert.Null(resultat.Valeur);                     // aucune session ouverte
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif)); // motif clair présent
    }

    // ---------- Driver — le motif nomme la cause « email inconnu » ----------
    // Contradiction : un refus générique ne distingue pas l'email inconnu du compte inactif (Sc.3). Force
    // un motif qui désigne la cause « email inconnu » (message clair, actionnable côté IHM au Sc.7).
    [Fact]
    public void Should_Nommer_email_inconnu_dans_le_motif_When_l_email_est_inconnu()
    {
        var comptes = new ReferentielComptesEnMemoire();
        var handler = new SeConnecterHandler(comptes, new HacheurMotDePassePbkdf2()); // référentiel vide

        var resultat = handler.Handle(new SeConnecterCommand(EmailInconnu));

        Assert.Contains("inconnu", resultat.Motif);
    }
}
