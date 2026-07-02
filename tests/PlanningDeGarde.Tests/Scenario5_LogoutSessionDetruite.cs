using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 23 — Sc.5 — Logout : la session est détruite, l'identité retombe non connectée (@back)
//   Tranche BACKEND (frontière Application) : après un logout, il n'y a PLUS de session serveur (la
//   destruction de session est l'absence de session côté serveur — pas d'agrégat durable, borne
//   anti-cliquet règle 30). La résolution de l'acteur par défaut retombe alors sur le comportement NON
//   connecté : AUCUNE identité résiduelle du compte déconnecté — l'acteur par défaut n'est plus « Alice ».
//   Le défaut non connecté EXACT (valeur de repli) est éprouvé au Sc.6 — ici on prouve seulement la
//   non-fuite d'identité. On NE teste PAS ici de rendu Blazor.
public class Scenario5_LogoutSessionDetruite
{
    private const string Email = "alice@foyer.fr";
    private const string ActeurAlice = "acteur-alice";

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Une session est ouverte pour « alice@foyer.fr » (défaut = « Alice »), puis le compte se déconnecte.
    // Après logout (plus de session), résoudre l'acteur par défaut ne doit PLUS rendre « Alice » : aucune
    // identité résiduelle ne subsiste.
    [Fact]
    public void Acceptation_Should_Ne_plus_resoudre_l_acteur_du_compte_deconnecte_When_la_session_est_detruite_par_logout()
    {
        var comptes = new ReferentielComptesEnMemoire();
        comptes.Creer("compte-1", Email, StatutCompte.Actif, ActeurAlice);
        var resolveur = new ResoudreActeurParDefautQuery();
        var session = new SeConnecterHandler(comptes).Handle(new SeConnecterCommand(Email)).Valeur!;
        Assert.Equal(ActeurAlice, resolveur.Resoudre(session)); // pré-condition : connecté → « Alice »

        // Logout = destruction de session : côté serveur, plus aucune session (absence de session).
        var apresLogout = resolveur.Resoudre(session: null);

        Assert.NotEqual(ActeurAlice, apresLogout); // aucune identité résiduelle : plus « Alice »
    }
}
