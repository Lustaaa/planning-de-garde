using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 23 — Sc.3 — Rejet : compte Inactif, aucune session (@back)
//   Tranche BACKEND (frontière Application) : la commande SeConnecter, sur l'email d'un compte de statut
//   INACTIF (défaut de création s22), ÉCHOUE avec un motif clair (compte non activé) et n'ouvre AUCUNE
//   session. L'activation Inactif→Actif reste HORS SCOPE (palier 13) : aucun chemin d'activation
//   déclenché — le compte reste Inactif après la tentative. On NE teste PAS ici de rendu Blazor.
public class Scenario3_SeConnecterCompteInactif
{
    private const string EmailBob = "bob@foyer.fr";
    private const string ActeurBob = "acteur-bob";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Un compte EXISTANT mais INACTIF : tenter de s'y connecter doit ÉCHOUER avec un motif clair et sans
    // session ouverte. Le compte reste Inactif (aucune activation déclenchée — hors scope).
    [Fact]
    public void Acceptation_Should_Echouer_avec_motif_clair_et_sans_session_When_le_compte_est_inactif()
    {
        var comptes = new ReferentielComptesEnMemoire();
        comptes.Creer("compte-bob", EmailBob, StatutCompte.Inactif, ActeurBob);
        var handler = new SeConnecterHandler(comptes, new HacheurMotDePassePbkdf2());

        var resultat = handler.Handle(new SeConnecterCommand(EmailBob));

        Assert.False(resultat.EstSucces);                        // refus
        Assert.Null(resultat.Valeur);                            // aucune session ouverte
        Assert.False(string.IsNullOrWhiteSpace(resultat.Motif)); // motif clair présent
    }

    // ---------- Driver — le motif désigne « compte non activé » (distinct de « email inconnu ») ----------
    // Contradiction : la garde « email inconnu » (Sc.2) laisserait passer un compte EXISTANT inactif vers
    // le succès. Force une garde distincte au motif clair « non activé », séparé de l'email inconnu (Sc.2).
    [Fact]
    public void Should_Nommer_compte_non_active_dans_le_motif_When_le_compte_est_inactif()
    {
        var comptes = new ReferentielComptesEnMemoire();
        comptes.Creer("compte-bob", EmailBob, StatutCompte.Inactif, ActeurBob);
        var handler = new SeConnecterHandler(comptes, new HacheurMotDePassePbkdf2());

        var resultat = handler.Handle(new SeConnecterCommand(EmailBob));

        Assert.Contains("activ", resultat.Motif); // « compte non activé » : la cause désignée, ≠ email inconnu
    }

    // ---------- Driver — l'activation reste hors scope : le compte demeure Inactif ----------
    // Contradiction : une tentative de connexion ne doit PAS activer le compte (pas de chemin
    // d'activation, palier 13). Force l'absence d'effet de bord : après le refus, le compte est toujours
    // Inactif dans le référentiel.
    [Fact]
    public void Should_Laisser_le_compte_inactif_When_une_connexion_est_tentee()
    {
        var comptes = new ReferentielComptesEnMemoire();
        comptes.Creer("compte-bob", EmailBob, StatutCompte.Inactif, ActeurBob);
        var handler = new SeConnecterHandler(comptes, new HacheurMotDePassePbkdf2());

        handler.Handle(new SeConnecterCommand(EmailBob));

        var compte = System.Linq.Enumerable.Single(comptes.EnumererComptes(), c => c.Email == EmailBob);
        Assert.Equal(StatutCompte.Inactif, compte.Statut); // toujours Inactif : aucune activation déclenchée
    }
}
