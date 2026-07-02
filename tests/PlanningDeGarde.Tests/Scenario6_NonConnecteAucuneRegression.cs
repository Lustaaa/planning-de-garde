using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 23 — Sc.6 — Non connecté = aucune régression (impersonation bornée s14) (@back)
//   Scénario de NON-RÉGRESSION (caractérisation à la frontière Application) : sans session serveur,
//   l'acteur par défaut retombe sur le DÉFAUT ACTUEL — aucun couplage au compte connecté, même si le
//   référentiel contient un compte Actif ; le défaut non connecté ≠ l'acteur d'un compte (pas de fuite
//   d'auth dans le chemin non connecté). Cette scène VERROUILLE le comportement d'avant l'auth : elle est
//   verte parce qu'AUCUNE régression n'a été introduite (pas de pilotage de code neuf ici).
//
//   L'impersonation bornée lecture (s14) — incarnation gatée, retour auto sur suppression concurrente —
//   vit dans SessionPlanning (front, PlanningDeGarde.Web) et reste INDÉPENDANTE de toute session serveur.
//   Sa non-régression est prouvée par les tests s14 existants (PlanningDeGarde.Web.Tests :
//   SessionPlanningIncarnationTests + FrontWasm…TempsReel), verts dans la suite complète — on ne les
//   duplique pas ici, et on n'introduit AUCUNE référence de Web dans les tests domaine/application
//   (respect de l'archi hexagonale : les tests d'App ne dépendent pas du front).
public class Scenario6_NonConnecteAucuneRegression
{
    private const string Email = "alice@foyer.fr";
    private const string ActeurAlice = "acteur-alice";

    // ---------- Non connecté → l'acteur par défaut n'est PAS couplé au compte ----------
    // Même avec un compte Actif au référentiel, sans session serveur, l'acteur par défaut retombe sur le
    // défaut actuel (aucun pré-positionnement couplé) — jamais l'acteur d'un compte (pas de fuite d'auth
    // dans le chemin non connecté). Contraste avec le chemin connecté (Sc.4) : là le défaut = l'acteur.
    [Fact]
    public void Acceptation_Should_Retomber_sur_le_defaut_actuel_non_couple_au_compte_When_aucune_session_n_est_ouverte()
    {
        var comptes = new ReferentielComptesEnMemoire();
        comptes.Creer("compte-1", Email, StatutCompte.Actif, ActeurAlice); // un compte Actif existe
        var resolveur = new ResoudreActeurParDefautQuery();

        var defautNonConnecte = resolveur.Resoudre(session: null);

        Assert.NotEqual(ActeurAlice, defautNonConnecte); // défaut non connecté ≠ acteur d'un compte
        Assert.Null(defautNonConnecte);                  // = défaut actuel : aucun pré-positionnement couplé
    }
}
