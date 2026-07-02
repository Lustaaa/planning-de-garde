using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 25 — Sc.10 — Libre-service : email DÉJÀ PORTEUR → rejet SANS écriture (@back, Volet 3)
//   Tranche BACKEND (frontière Application) : l'inscription libre-service (Sc.9) doit respecter
//   l'invariant EMAIL UNIQUE du référentiel de comptes (s22). Si un compte existe déjà pour l'email
//   fourni, la création est REJETÉE avec un motif clair, SANS aucune écriture — le référentiel reste
//   inchangé (pas de doublon, pas d'écrasement du compte existant). C'est CE scénario qui force la garde
//   d'unicité dans CreerCompteLibreServiceHandler (absente à Sc.9, qui ne teste que l'email neuf).
public class Scenario10_CreerCompteLibreServiceEmailDejaPorteur
{
    private const string Email = "existant@foyer.fr";
    private const string MotDePasse = "s3cr3t-nouveau";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Un compte existe déjà pour l'email : tenter une inscription libre-service avec le même email doit
    // ÉCHOUER avec un motif clair et NE RIEN écrire — le référentiel garde exactement le compte initial,
    // inchangé (même id, même statut : pas d'écrasement).
    [Fact]
    public void Acceptation_Should_Rejeter_sans_ecriture_ni_ecrasement_When_l_email_est_deja_porteur()
    {
        var comptes = new ReferentielComptesEnMemoire();
        comptes.Creer("compte-existant", Email, StatutCompte.Actif, "acteur-x"); // déjà porteur (via admin s22)
        var handler = new CreerCompteLibreServiceHandler(comptes, comptes, new HacheurMotDePassePbkdf2());

        var resultat = handler.Handle(new CreerCompteLibreServiceCommand(Email, MotDePasse));

        Assert.False(resultat.EstSucces);                        // refus
        Assert.Null(resultat.Valeur);                            // aucune création
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif)); // motif clair présent

        var comptesPourEmail = comptes.EnumererComptes().Where(c => c.Email == Email).ToList();
        Assert.Single(comptesPourEmail);                         // aucun doublon écrit
        var compte = comptesPourEmail.Single();
        Assert.Equal("compte-existant", compte.Id);              // compte initial intact (pas d'écrasement)
        Assert.Equal(StatutCompte.Actif, compte.Statut);        // ... statut initial préservé
    }
}
